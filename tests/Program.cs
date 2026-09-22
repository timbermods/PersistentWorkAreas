using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Text.Json;
using PersistentWorkAreas;

int checks = 0;
void Check(bool ok, string name)
{
    if (!ok) throw new Exception("FAIL: " + name);
    checks++;
    Console.WriteLine("PASS: " + name);
}

var pins = new PinSet<EqualBuilding>();
var farm = new EqualBuilding(); var forester = new EqualBuilding();
Check(!pins.Set(null, true) && pins.Count == 0, "Null cannot be pinned");
Check(pins.Set(farm, true) && pins.Contains(farm), "Pin building");
Check(!pins.Set(farm, true) && pins.Count == 1, "Duplicate pin is idempotent");
Check(pins.Set(forester, true) && pins.Count == 2, "Distinct equal-valued buildings retain separate pins");
Check(pins.Set(farm, false) && !pins.Contains(farm) && pins.Contains(forester), "Unpin leaves other building pinned");
Check(!pins.Set(farm, false), "Repeated unpin is safe");
Check(!pins.Contains(new EqualBuilding()), "Replacement building never inherits old pin");
Check(pins.Clear() && pins.Count == 0 && !pins.Contains(forester), "Global clear removes every pin");
Check(!pins.Clear(), "Repeated global clear is safe");
pins.Set(forester, true);
Check(pins.Set(forester, false) && pins.Count == 0, "Deleting last pinned building empties collection");
Check(new PinSet<EqualBuilding>().Count == 0, "New map begins without pins");

var game = Path.GetFullPath(args[0]);
var modPath = Path.GetFullPath(args[1]);
var managed = Path.Combine(game, "Timberborn_Data", "Managed");
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(managed, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
Type GameType(string assembly, string name) => Assembly.LoadFrom(Path.Combine(managed, assembly + ".dll")).GetType(name, true)!;
var nav = Assembly.LoadFrom(Path.Combine(managed, "Timberborn.BuildingsNavigation.dll"));
Type Nav(string name) => nav.GetType("Timberborn.BuildingsNavigation." + name, true)!;
const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
void Field(Type type, string name, Type expected)
{
    Check(type.GetField(name, fields)?.FieldType == expected, type.Name + "." + name + " renderer field");
}
void Method(Type type, string name, Type returns, params Type[] args)
{
    Check(type.GetMethod(name, args)?.ReturnType == returns, type.Name + "." + name + " signature");
}
var drawer = Nav("BoundsNavRangeDrawer"); var calculator = Nav("BoundsNavRangeCalculator");
var bounds = Nav("BoundsMesh"); var layer = Nav("BoundsMeshLayer");
var vector = GameType("UnityEngine.CoreModule", "UnityEngine.Vector3Int");
var material = GameType("UnityEngine.CoreModule", "UnityEngine.Material");
var mesh = GameType("UnityEngine.CoreModule", "UnityEngine.Mesh");
var cells = typeof(IReadOnlyCollection<>).MakeGenericType(vector);
Check(calculator.GetConstructors().Single().GetParameters().Select(x => x.ParameterType.FullName).SequenceEqual(new[] {
    "Timberborn.BlockSystem.IBlockService", "Timberborn.BlockSystem.PreviewBlockService", "Timberborn.LevelVisibilitySystem.ILevelVisibilityService" }), "Native calculator constructor");
Check(drawer.GetConstructors().Single().GetParameters().Select(x => x.ParameterType.FullName).SequenceEqual(new[] {
    calculator.FullName, "Timberborn.MapStateSystem.MapSize", "Timberborn.BlueprintSystem.ISpecService" }), "Native drawer constructor");
Method(drawer, "Load", typeof(void));
Method(drawer, "UpdateArea", typeof(void), cells);
Method(drawer, "Draw", typeof(void));
Field(drawer, "_boundsMesh", bounds);
Field(bounds, "_layers", typeof(Dictionary<,>).MakeGenericType(typeof(int), layer));
Field(layer, "_mesh", mesh);
Field(layer, "_material", material);
var target = RuntimeHelpers.GetUninitializedObject(drawer);
Check(drawer.GetMethod("Draw")!.CreateDelegate(typeof(Action), target) != null, "Internal draw method binds without Harmony");
Check(drawer.GetMethod("UpdateArea")!.CreateDelegate(typeof(Action<>).MakeGenericType(cells), target) != null, "Internal update method binds without Harmony");

var builderHut = GameType("Timberborn.BuilderHubSystem", "Timberborn.BuilderHubSystem.BuilderHubWorkplaceBehavior");
Check(builderHut.IsClass, "Builder's Hut marker component exists (excluded from pinning)");

var mod = Assembly.LoadFrom(modPath);
var service = mod.GetType("PersistentWorkAreas.WorkAreaService", true)!;
var interfaces = service.GetInterfaces().Select(x => x.FullName).ToArray();
Check(interfaces.Contains("Timberborn.SingletonSystem.IPostLoadableSingleton"), "Game post-load lifecycle");
Check(interfaces.Contains("Timberborn.SingletonSystem.IUpdatableSingleton"), "Display refresh lifecycle");
Check(interfaces.Contains("Timberborn.Navigation.ISingletonInstantNavMeshListener") && interfaces.Contains("Timberborn.Navigation.ISingletonPreviewNavMeshListener"), "Live and construction preview invalidation");
Check(!interfaces.Any(x => x.Contains("Saveable") || x.Contains("Tickable") || x.Contains("PersistentEntity")), "No simulation tick or persistence interfaces");
Check(!mod.GetReferencedAssemblies().Any(x => x.Name.Contains("BeaverBuddies") || x.Name.Contains("TimberNet") || x.Name.Contains("Harmony")), "No multiplayer or Harmony assembly dependencies");
Check(mod.GetTypes().All(t => !t.GetCustomAttributesData().Any(a => a.AttributeType.Name.StartsWith("HarmonyPatch"))), "No method patches");
var fragment = mod.GetType("PersistentWorkAreas.WorkAreaFragment", true)!;
Check(fragment.GetInterfaces().Any(x => x.FullName == "Timberborn.EntityPanelSystem.IEntityPanelFragment"), "Building checkbox fragment contract");
foreach (var method in new[] { "OnVisibleLevel", "OnConstruction", "OnSelected", "OnUnselected", "OnDeleted" })
    Check(service.GetMethod(method)!.GetCustomAttributesData().Any(x => x.AttributeType.Name == "OnEventAttribute"), method + " event subscription");

var packaging = Path.GetFullPath(args[2]);
using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(packaging, "manifest.json")));
Check(manifest.RootElement.GetProperty("RequiredMods").GetArrayLength() == 0, "Standalone package has no required mods");
Check(manifest.RootElement.GetProperty("Version").GetString() == mod.GetName().Version!.ToString(3), "Manifest version matches assembly version");
using var binding = JsonDocument.Parse(File.ReadAllText(Path.Combine(packaging, "KeyBindings", "PersistentWorkAreas.Clear.blueprint.json")));
Check(binding.RootElement.GetProperty("KeyBindingSpec").GetProperty("Id").GetString() == (string)service.GetField("ClearKey")!.GetRawConstantValue()!, "Clear key binding matches input handler");
Console.WriteLine($"{checks} checks passed. Unity rendering and multiplayer playtesting still require the game.");

sealed class EqualBuilding
{
    public override bool Equals(object obj) => obj is EqualBuilding;
    public override int GetHashCode() => 1;
}
