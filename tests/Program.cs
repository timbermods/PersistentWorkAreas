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

// Refresh planning, against a fake navigation world that counts range queries and outline rebuilds.
var around = CellBox.Around(10.5f, 20.5f, 3f, 22f);
Check(around.MinX == -12 && around.MaxX == 33 && around.MinY == -2 && around.MaxY == 43 && around.MinZ == -19 && around.MaxZ == 25,
    "Pin bounds match BuildingTerrainRange: floor(access - 22) to ceil(access + 22)");
Check(CellBox.Point(0, 0, 0).Intersects(new CellBox(0, 0, 0, 1, 1, 1)) && !CellBox.Point(0, 0, 0).Intersects(CellBox.Point(1, 0, 0)) &&
    !CellBox.Empty.Intersects(CellBox.Unbounded), "Fake-world cell boxes are inclusive and empty boxes meet nothing");
int queries = 0, rebuilds = 0;
bool DescribeFakePin(FakePin pin, out (int X, int Y, int Z, bool Preview) key, out CellBox reach)
{
    key = (pin.X, pin.Y, pin.Z, pin.Preview);
    reach = pin.RoadSpill ? CellBox.Unbounded : CellBox.Around(pin.X, pin.Y, pin.Z, 22f);
    return !pin.Gone;
}
void QueryFakeRange(FakePin pin, (int X, int Y, int Z, bool Preview) key, HashSet<(int, int, int)> cells)
{
    queries++;
    for (int dx = -pin.Radius; dx <= pin.Radius; dx++)
        for (int dy = Math.Abs(dx) - pin.Radius; dy <= pin.Radius - Math.Abs(dx); dy++)
            cells.Add((key.X + dx, key.Y + dy, key.Z));
}
var planner = new PinRefreshPlanner<FakePin, (int X, int Y, int Z, bool Preview), (int, int, int)>(
    DescribeFakePin, QueryFakeRange, cell => CellBox.Point(cell.Item1, cell.Item2, cell.Item3));
var dropped = new List<FakePin>();
(int Queries, int Rebuilds) Pass()
{
    int queried = queries, rebuilt = rebuilds;
    dropped.Clear();
    if (planner.Pending && planner.Refresh(dropped)) rebuilds++;
    return (queries - queried, rebuilds - rebuilt);
}
void NavigationChange(CellBox bounds) => planner.Touch(bounds, (box, changed) => box.Intersects(changed));
var elsewhere = new CellBox(300, 300, 0, 310, 310, 10);
var pinA = new FakePin(0, 0, 5); var pinB = new FakePin(100, 0, 5); var pinC = new FakePin(0, 100, 5);
planner.Add(pinA); planner.Add(pinB); planner.Add(pinC);
Check(Pass() == (3, 1) && planner.Cells.Count == 3 * 13, "New pins are queried once each and drawn as one outline");
Check(!planner.Pending && Pass() == (0, 0), "No refresh work until something changes");
NavigationChange(elsewhere);
Check(Pass() == (0, 0), "Navigation change missing every pin: 0 queries and 0 rebuilds");
NavigationChange(CellBox.Point(15, 0, 5));
Check(Pass() == (1, 0), "Navigation change near pin A only: 1 query, and unchanged cells need no rebuild");
pinA.Radius = 3; NavigationChange(CellBox.Point(3, 0, 5));
Check(Pass() == (1, 1) && planner.Cells.Contains((3, 0, 5)), "Navigation change that alters pin A's range: 1 query and 1 rebuild");
NavigationChange(CellBox.Point(1, 0, 5));
Check(Pass() == (1, 1), "Navigation change inside a drawn range redraws even when its cells are unchanged");
planner.Select(pinA);
Check(Pass() == (0, 1) && !planner.Cells.Contains((3, 0, 5)) && planner.Cells.Contains((100, 0, 5)),
    "Selecting a pinned building: 0 queries and 1 rebuild from the cached ranges");
planner.Select(null);
Check(Pass() == (0, 1) && planner.Cells.Contains((3, 0, 5)), "Deselecting it restores its cached range without a query");
planner.Select(new FakePin(0, 0, 5));
Check(!planner.Pending && Pass() == (0, 0), "Selecting an unpinned building does no refresh work");
planner.Select(null);
planner.InvalidateAll();
Check(Pass() == (3, 1), "Construction-mode toggle: N queries for N pins");
planner.Redraw();
Check(Pass() == (0, 1), "Visible-level change redraws without queries");
pinB.Preview = true; NavigationChange(elsewhere);
Check(Pass() == (1, 0), "A pin whose access or graph changed is re-queried even when the navigation change was elsewhere");
planner.Select(pinA); Pass();
pinA.Radius = 2; NavigationChange(CellBox.Point(0, 0, 5));
Check(Pass() == (0, 0), "A selected pin's query waits while the game draws its outline");
planner.Select(null);
Check(Pass() == (1, 1) && !planner.Cells.Contains((3, 0, 5)), "Deselecting a pin that went stale queries it once");
var spill = new FakePin(0, 200, 5) { RoadSpill = true };
planner.Add(spill); Pass();
NavigationChange(elsewhere);
Check(Pass() == (1, 0), "Road-spill ranges have no bound, so every navigation change re-queries them");
Check(planner.Remove(spill) && Pass() == (0, 1) && !planner.Cells.Contains((0, 200, 5)), "Unpinning rebuilds from the cache without queries");
pinC.Gone = true; NavigationChange(elsewhere);
Check(Pass() == (0, 1) && dropped.SequenceEqual(new[] { pinC }) && planner.Count == 2 && !planner.Cells.Contains((0, 100, 5)),
    "A pin that can no longer be shown is dropped with its range");
var twins = new PinRefreshPlanner<EqualBuilding, int, int>((EqualBuilding _, out int key, out CellBox reach) =>
    { key = 0; reach = CellBox.Empty; return true; }, (_, _, _) => { }, _ => CellBox.Empty);
Check(twins.Add(new EqualBuilding()) && twins.Add(new EqualBuilding()) && twins.Count == 2, "Refresh planning keeps equal-valued buildings apart");
planner.Clear(); NavigationChange(CellBox.Point(0, 0, 5));
Check(planner.Count == 0 && planner.Cells.Count == 0 && !planner.Pending, "Clearing forgets every pin and cached range");

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
var distance = GameType("Timberborn.Navigation", "Timberborn.Navigation.NavigationDistance");

var mod = Assembly.LoadFrom(modPath);
var service = mod.GetType("PersistentWorkAreas.WorkAreaService", true)!;
var boundingBox = GameType("Timberborn.Common", "Timberborn.Common.BoundingBox");
object GameBox(int x, int y, int z)
{
    var builderType = boundingBox.GetNestedType("Builder")!;
    var builder = Activator.CreateInstance(builderType)!;
    builderType.GetMethod("Expand")!.Invoke(builder, new[] { Activator.CreateInstance(vector, x, y, z) });
    return builderType.GetMethod("Build")!.Invoke(builder, null)!;
}
var touches = (Delegate)service.GetField("Touches", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
var pinBox = Activator.CreateInstance(mod.GetType("PersistentWorkAreas.CellBox", true)!, 0, 0, 0, 10, 20, 30);
bool Touched(int x, int y, int z) => (bool)touches.DynamicInvoke(pinBox, GameBox(x, y, z))!;
Check(Touched(0, 0, 0) && Touched(10, 20, 30) && Touched(5, 5, 25) && !Touched(11, 5, 5) && !Touched(5, 25, 5) && !Touched(5, 5, -1),
    "Navigation-change test uses the game's BoundingBox with the same axes and inclusive edges");

// Service wiring, on an instance built outside the game: the constructor only stores its dependencies and reads NavigationDistance.
var distanceValue = Activator.CreateInstance(distance)!;
var serviceCtor = service.GetConstructors().Single();
var live = serviceCtor.Invoke(serviceCtor.GetParameters().Select(p => p.ParameterType == distance ? distanceValue : null).ToArray());
Check((float)service.GetField("_reach", fields)!.GetValue(live)! == (float)distance.GetProperty("ResourceBuildings")!.GetValue(distanceValue)! + 2f,
    "Pin reach is the game's range distance plus BuildingTerrainRange's 2-cell margin");
T Get<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(target)!;
void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(target, value);
var livePlanner = Get<object>(live, "_planner");
var livePin = RuntimeHelpers.GetUninitializedObject(GameType("Timberborn.EntitySystem", "Timberborn.EntitySystem.EntityComponent"));
Check((bool)livePlanner.GetType().GetMethod("Add")!.Invoke(livePlanner, new[] { livePin })!, "Service planner accepts a pinned entity");
var liveEntry = Get<System.Collections.IDictionary>(livePlanner, "_entries")[livePin]!;
Set(liveEntry, "Reach", pinBox);
var navMeshUpdate = GameType("Timberborn.Navigation", "Timberborn.Navigation.NavMeshUpdate").GetConstructors(fields).Single();
object Update(int x, int y, int z) => navMeshUpdate.Invoke(navMeshUpdate.GetParameters()
    .Select((p, i) => i == 0 ? GameBox(x, y, z) : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray());
// What a handler leaves behind: a pass pending, the outline marked for a redraw, the pin marked for a query, the throttle skipped.
(bool Pending, bool Redraw, bool Query, bool Now) Handle(string handler, object argument)
{
    Set(livePlanner, "_pending", false); Set(livePlanner, "_redraw", false); Set(liveEntry, "Stale", false); Set(live, "_nextRefresh", 99f);
    service.GetMethod(handler)!.Invoke(live, new[] { argument });
    return (Get<bool>(livePlanner, "_pending"), Get<bool>(livePlanner, "_redraw"), Get<bool>(liveEntry, "Stale"), Get<float>(live, "_nextRefresh") == 0f);
}
Check(Handle("OnVisibleLevel", null) == (true, true, false, true), "Visible-level event redraws the pinned outline without re-querying");
Check(Handle("OnConstruction", null) == (true, true, true, true), "Construction-mode event re-queries every pin");
Check(Handle("OnInstantNavMeshUpdated", Update(5, 5, 25)) == (true, false, true, false) &&
    Handle("OnPreviewNavMeshUpdated", Update(10, 20, 30)) == (true, false, true, false),
    "Live and preview navigation updates inside a pin's reach re-query it at the throttled rate");
Check(Handle("OnInstantNavMeshUpdated", Update(11, 5, 5)) == (true, false, false, false) &&
    Handle("OnPreviewNavMeshUpdated", Update(5, 25, 5)) == (true, false, false, false),
    "Navigation updates outside a pin's reach keep its cached range");
Set(livePlanner, "_selected", livePin);
Check(Handle("OnUnselected", null) == (true, false, false, true) && Get<object>(livePlanner, "_selected") == null,
    "Deselecting a pinned building rebuilds the pinned outline from the cache");
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

// Radius stands in for the navigation state: changing it changes the range the next query returns.
sealed class FakePin
{
    public readonly int X, Y, Z;
    public int Radius = 2;
    public bool Preview, RoadSpill, Gone;
    public FakePin(int x, int y, int z) { X = x; Y = y; Z = z; }
}
