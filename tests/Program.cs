using System.IO.Compression;
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
var tracked = new PinSet<EqualBuilding>();
Check(!tracked.Changed && tracked.Set(farm, true) && tracked.Changed, "Pinning marks the pins for saving");
tracked.Changed = false;
Check(!tracked.Set(farm, true) && !tracked.Set(forester, false) && !tracked.Set(null, true) && !tracked.Changed, "A pin or unpin that changes nothing needs no save");
Check(tracked.Set(farm, false) && tracked.Changed, "Unpinning marks the pins for saving");
tracked.Set(farm, true); tracked.Changed = false;
Check(tracked.Clear() && tracked.Changed, "Clearing marks the pins for saving");
tracked.Changed = false;
Check(!tracked.Clear() && !tracked.Changed, "Clearing no pins needs no save");

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

// The planting tool shows the ranges of the buildings that plant the selected crop or tree, alongside the pins.
Check(PlantingRanges.Shows("Farmhouse", "Farmhouse") && PlantingRanges.Shows("Forester", "Forester") && PlantingRanges.Shows("AquaticFarmhouse", "AquaticFarmhouse"),
    "The planting tool shows the planter buildings of the plant's resource group");
Check(!PlantingRanges.Shows("Farmhouse", "Forester") && !PlantingRanges.Shows("Forester", "Farmhouse") && !PlantingRanges.Shows("AquaticFarmhouse", "Farmhouse"),
    "Crops never show foresters, trees never show farmhouses, and aquatic crops show only aquatic farmhouses");
Check(!PlantingRanges.Shows("Farmhouse", null) && !PlantingRanges.Shows(null, null) && !PlantingRanges.Shows("", ""),
    "Buildings that plant nothing, and plants without a group, show nothing");
Check(!PlantingRanges.Shows("farmhouse", "Farmhouse"), "Groups match exactly, as the game compares them");
var farmA = new FakePin(0, 0, 5); var farmB = new FakePin(100, 0, 5); var grove = new FakePin(0, 100, 5); var kept = new FakePin(200, 0, 5);
planner.Add(kept); Pass();
planner.Show(new[] { farmA, farmB });
Check(Pass() == (2, 1) && planner.Cells.Count == 3 * 13 && planner.Cells.Contains((100, 0, 5)) && planner.Cells.Contains((200, 0, 5)),
    "Opening the planting tool for a crop: 1 query per farmhouse and 1 rebuild, drawn with the pins");
planner.Show(new[] { farmB, farmA });
Check(!planner.Pending && Pass() == (0, 0), "Switching to another crop for the same farmhouses reuses their ranges: 0 queries and 0 rebuilds");
NavigationChange(CellBox.Point(15, 0, 5));
Check(Pass() == (1, 0), "A navigation change near one shown farmhouse re-queries only that farmhouse");
planner.Select(farmB);
Check(Pass() == (0, 1) && !planner.Cells.Contains((100, 0, 5)), "A shown building that is selected is left to the game's own outline");
planner.Select(null); Pass();
planner.InvalidateAll();
Check(Pass() == (3, 1), "Construction-mode toggle re-queries pins and shown buildings alike");
planner.Show(new[] { grove });
Check(Pass() == (1, 1) && planner.Count == 2 && planner.Cells.Contains((0, 100, 5)) && !planner.Cells.Contains((0, 0, 5)),
    "Switching to a tree: the forester is queried and the farmhouses are dropped with their ranges");
planner.Show(Array.Empty<FakePin>());
Check(Pass() == (0, 1) && planner.Count == 1 && planner.Cells.Count == 13 && planner.Cells.Contains((200, 0, 5)),
    "Closing the planting tool: 0 queries and 1 rebuild back to the pinned outline");
planner.Show(new FakePin[] { null });
Check(!planner.Pending && Pass() == (0, 0) && planner.Count == 1, "Closing it again does no work");
planner.Show(new[] { kept, farmA });
Check(Pass() == (1, 1), "A pinned building the tool also shows keeps its cached range: only the other farmhouse is queried");
Check(planner.Remove(kept) && !planner.Remove(kept) && Pass() == (0, 0) && planner.Cells.Contains((200, 0, 5)),
    "Unpinning a building the tool shows keeps its range drawn while the tool is open");
Check(planner.Add(farmA) && !planner.Add(farmA) && Pass() == (0, 0), "Pinning a building the tool shows reuses its range");
planner.Show(Array.Empty<FakePin>());
Check(Pass() == (0, 1) && planner.Count == 1 && planner.Cells.Contains((0, 0, 5)) && !planner.Cells.Contains((200, 0, 5)),
    "Closing the tool keeps the ranges that are pinned and drops the rest");
planner.Clear();

// Pins are remembered per settlement in a local file, by the entity id the save keeps for each building, never in the save.
var idA = new Guid("18c0e8f4-5b0e-4d7a-9a52-0c7c1d3b9e01");
var idB = new Guid("6d2f9a31-0e47-4b8c-8f1d-5a9e7c3b2d44");
var idC = new Guid("c4a7e2d9-3f61-4e0b-b8a5-9d2c6f1e7a53");
var remembered = PinStore.Write(null, "Beaverton", new[] { idB, idA, idB, Guid.Empty });
Check(PinStore.Read(remembered, "Beaverton").SequenceEqual(new[] { idA, idB }) && remembered.Split(idB.ToString("D")).Length == 2 && !remembered.Contains(Guid.Empty.ToString("D")),
    "Pins round-trip through the pin file as entity ids, written once each");
Check(PinStore.Read(remembered, "Otter Bay").Count == 0 && PinStore.Read(remembered, "beaverton").Count == 0 && PinStore.Read(remembered, "Beaverton ").Count == 0,
    "A different settlement shows none: the settlement name must match exactly");
remembered = PinStore.Write(remembered, "Otter Bay", new[] { idC });
Check(PinStore.Read(remembered, "Beaverton").SequenceEqual(new[] { idA, idB }) && PinStore.Read(remembered, "Otter Bay").SequenceEqual(new[] { idC }),
    "Saving one settlement's pins keeps every other settlement's");
var forgotten = PinStore.Write(remembered, "Beaverton", Array.Empty<Guid>());
Check(PinStore.Read(forgotten, "Beaverton").Count == 0 && !forgotten.Contains("Beaverton") && PinStore.Read(forgotten, "Otter Bay").SequenceEqual(new[] { idC }),
    "Clearing a settlement's pins removes its entry and keeps the others");
Check(PinStore.Read(null, "Beaverton").Count == 0 && PinStore.Read("", "Beaverton").Count == 0, "A missing or empty pin file remembers no pins");
Check(PinStore.Read("PK\u0003\u0004\uFFFD\u0000 not a pin file\n" + remembered, "Beaverton").Count == 0 &&
    PinStore.Read(remembered.Replace(PinStore.Header, PinStore.Header + "0"), "Beaverton").Count == 0,
    "A corrupt pin file, or one in another format, remembers no pins");
var damaged = remembered.Replace(idA.ToString("D"), "18c0e8f4-5b0e") + "Bad\\qname\t" + idC + "\n\t" + idC + "\nhalf a li";
Check(PinStore.Read(damaged, "Beaverton").SequenceEqual(new[] { idB }) && PinStore.Read(damaged, "Otter Bay").SequenceEqual(new[] { idC }) &&
    PinStore.Read(damaged, "Badqname").Count == 0 && PinStore.Read(damaged, "Bad\\qname").Count == 0 && PinStore.Read(damaged, "").Count == 0,
    "Damaged entries in the pin file are skipped and the rest survive");
Check(PinStore.Write("garbage\n\u0000", "Beaverton", new[] { idA }) == PinStore.Write(null, "Beaverton", new[] { idA }), "Saving over a corrupt pin file replaces it");
var names = new[] { "Tab\tTown", "Two\nLines\r", "Back\\slash\\t", "Back\\slash\t", "\u00DCbersee \u6CB3\u72F8", " spaced ", "Online Games" };
string awkward = null;
for (int i = 0; i < names.Length; i++) awkward = PinStore.Write(awkward, names[i], new[] { i % 2 == 0 ? idA : idB });
Check(names.Select((name, i) => PinStore.Read(awkward, name).SequenceEqual(new[] { i % 2 == 0 ? idA : idB })).All(x => x) &&
    PinStore.Read(awkward, "Tab").Count == 0 && PinStore.Read(awkward, "Two").Count == 0 && awkward.Split('\n').Length == names.Length + 2,
    "Settlement names with tabs, line breaks, backslashes and other scripts keep their own pins, one line each");
string crowded = null;
for (int i = 0; i <= PinStore.MaxSettlements; i++) crowded = PinStore.Write(crowded, "Settlement " + i, new[] { idA });
Check(PinStore.Read(crowded, "Settlement 0").Count == 0 && PinStore.Read(crowded, "Settlement 1").Count == 1 &&
    PinStore.Read(crowded, "Settlement " + PinStore.MaxSettlements).Count == 1, "The pin file keeps only the most recently changed settlements");
crowded = PinStore.Write(PinStore.Write(crowded, "Settlement 1", new[] { idB }), "Settlement new", new[] { idA });
Check(PinStore.Read(crowded, "Settlement 1").SequenceEqual(new[] { idB }) && PinStore.Read(crowded, "Settlement 2").Count == 0,
    "Changing a settlement's pins makes it the most recent");
var touched = PinStore.Touch(PinStore.Write(PinStore.Write(null, "Beaverton", new[] { idA, idB }), "Otter Bay", new[] { idC }), "Beaverton");
Check(touched != null && touched.Split('\n')[1].StartsWith("Beaverton\t") && PinStore.Read(touched, "Beaverton").SequenceEqual(new[] { idA, idB }) &&
    PinStore.Read(touched, "Otter Bay").SequenceEqual(new[] { idC }), "Loading a settlement moves its entry first and keeps its pins and every other settlement's");
Check(PinStore.Touch(touched, "Beaverton") == null && PinStore.Touch(touched, "Nowhere") == null && PinStore.Touch(null, "Beaverton") == null &&
    PinStore.Touch("garbage\nBeaverton\t" + idA, "Beaverton") == null, "Loading a settlement that is already first, has no entry or has no readable file rewrites nothing");
crowded = PinStore.Write(PinStore.Touch(crowded, "Settlement 3")!, "Settlement newer", new[] { idA });
Check(PinStore.Read(crowded, "Settlement 3").SequenceEqual(new[] { idA }) && PinStore.Read(crowded, "Settlement 4").Count == 0,
    "A settlement loaded recently is kept over one changed longer ago");
var doubled = PinStore.Header + "\nBeaverton\t" + idA + "\t" + idA + "\t" + Guid.Empty + "\t" + idB + "\nBeaverton\t" + idC + "\n";
Check(PinStore.Read(doubled, "Beaverton").SequenceEqual(new[] { idA, idB }), "A hand-edited pin file that lists a settlement twice uses its first line, each building once");
var built = new[] { new SavedBuilding(idA), new SavedBuilding(idB), new SavedBuilding(idC) };
remembered = PinStore.Write(null, "Beaverton", built.Take(2).Select(x => x.Id));
var reloaded = new[] { new SavedBuilding(idA), new SavedBuilding(idB), new SavedBuilding(idC) };
SavedBuilding Resolve(Guid id) => reloaded.FirstOrDefault(x => x.Id == id);
Check(PinStore.Restore(PinStore.Read(remembered, "Beaverton"), Resolve, _ => true).SequenceEqual(reloaded.Take(2), PinSet<SavedBuilding>.Identity),
    "After a load, pins find the reloaded buildings by entity id, not the objects that were pinned");
Check(PinStore.Restore(new[] { idA, Guid.NewGuid(), idA }, Resolve, _ => true).SequenceEqual(new[] { reloaded[0] }, PinSet<SavedBuilding>.Identity),
    "Ids of buildings the loaded save no longer has are dropped, and each building is pinned once");
Check(PinStore.Restore(new[] { idA, idB }, Resolve, x => !ReferenceEquals(x, reloaded[0])).SequenceEqual(new[] { reloaded[1] }, PinSet<SavedBuilding>.Identity),
    "A remembered building that can no longer be pinned stays unpinned");
var pinFolder = Path.Combine(Path.GetTempPath(), "pwa-checks-" + Guid.NewGuid().ToString("N"));
try
{
    var pinFile = new PinFile(Path.Combine(pinFolder, "PersistentWorkAreas", "Pins.txt"));
    Check(pinFile.Load("Beaverton").Count == 0 && !Directory.Exists(pinFolder), "Before any pin is saved there is no pin file, and loading needs none");
    pinFile.Save("Beaverton", new[] { idB, idA });
    pinFile.Save("Otter Bay", new[] { idC });
    Check(new PinFile(pinFile.FilePath).Load("Beaverton").SequenceEqual(new[] { idA, idB }) && new PinFile(pinFile.FilePath).Load("Otter Bay").SequenceEqual(new[] { idC }),
        "Pins round-trip through the file on disk, whose folder is created on the first save");
    var pinFiles = Path.GetDirectoryName(pinFile.FilePath)!;
    string[] FileNames() => Directory.GetFiles(pinFiles).Select(x => Path.GetFileName(x)!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    var beforeTouch = File.ReadAllText(pinFile.FilePath);
    pinFile.Touch("Nowhere");
    bool untouched = File.ReadAllText(pinFile.FilePath) == beforeTouch;
    pinFile.Touch("Beaverton");
    Check(untouched && File.ReadAllLines(pinFile.FilePath)[1].StartsWith("Beaverton\t") && pinFile.Load("Beaverton").SequenceEqual(new[] { idA, idB }) &&
        pinFile.Load("Otter Bay").SequenceEqual(new[] { idC }) && FileNames().SequenceEqual(new[] { "Pins.txt" }),
        "Loading a settlement moves its entry first on disk, with no copy or temporary file left behind");
    var corrupt = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0xFF, 0xFE, 0x0A, 0x09, 0xC3 };
    File.WriteAllBytes(pinFile.FilePath, corrupt);
    Check(pinFile.Load("Beaverton").Count == 0, "A corrupt pin file on disk is ignored");
    pinFile.Save("Beaverton", new[] { idA });
    Check(pinFile.Load("Beaverton").SequenceEqual(new[] { idA }) && FileNames().SequenceEqual(new[] { "Pins.txt", "Pins.txt.bak" }) &&
        File.ReadAllBytes(pinFile.FilePath + ".bak").SequenceEqual(corrupt), "Saving replaces an unreadable pin file, keeps a copy of it and leaves no temporary file behind");
    var newer = PinStore.Header.Replace("v1", "v2") + "\nBeaverton\t" + idB + "\n";
    File.WriteAllText(pinFile.FilePath, newer);
    pinFile.Save("Beaverton", new[] { idA });
    Check(pinFile.Load("Beaverton").SequenceEqual(new[] { idA }) && File.ReadAllText(pinFile.FilePath + ".bak") == newer,
        "A pin file from a newer version is kept as a copy before this version replaces it");
}
finally
{
    if (Directory.Exists(pinFolder)) Directory.Delete(pinFolder, true);
}

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

// Planting tool events, with the game's own tool types. The handlers only record the plant's group; the next update finds the buildings.
var plantableSpec = GameType("Timberborn.Planting", "Timberborn.Planting.PlantableSpec");
var plantingTool = GameType("Timberborn.PlantingUI", "Timberborn.PlantingUI.PlantingTool");
object Planting(string group)
{
    var spec = RuntimeHelpers.GetUninitializedObject(plantableSpec);
    Set(spec, "<ResourceGroup>k__BackingField", group);
    var tool = RuntimeHelpers.GetUninitializedObject(plantingTool);
    Set(tool, "<PlantableSpec>k__BackingField", spec);
    return tool;
}
var cancelPlanting = RuntimeHelpers.GetUninitializedObject(GameType("Timberborn.PlantingUI", "Timberborn.PlantingUI.CancelPlantingTool"));
var toolEntered = GameType("Timberborn.ToolSystem", "Timberborn.ToolSystem.ToolEnteredEvent");
var toolExited = GameType("Timberborn.ToolSystem", "Timberborn.ToolSystem.ToolExitedEvent");
// What a tool switch leaves behind: the plant group shown, and whether the next update scans for its buildings.
// ToolService.SwitchTool posts the old tool's exit and then the new tool's entry within one call.
(string Group, bool Scan) Switch(object from, object to)
{
    Set(live, "_plantersChanged", false);
    if (from != null) service.GetMethod("OnToolExited")!.Invoke(live, new[] { Activator.CreateInstance(toolExited, from) });
    service.GetMethod("OnToolEntered")!.Invoke(live, new[] { Activator.CreateInstance(toolEntered, to, false) });
    return (Get<string>(live, "_planting"), Get<bool>(live, "_plantersChanged"));
}
var crops = Planting("Farmhouse"); var trees = Planting("Forester");
Check(Switch(null, crops) == ("Farmhouse", true), "Opening a crop's planting tool shows the farmhouses from the next update");
Check(Switch(crops, trees) == ("Forester", true), "Switching from a crop to a tree shows the foresters instead");
Check(Switch(trees, cancelPlanting) == (null, true), "Leaving the planting tools, even for the cancel-planting tool beside them, hides the planters");
Check(Switch(null, cancelPlanting) == (null, false), "Other tools show no planter ranges and cause no scan");
Check(GameType("Timberborn.WorkSystem", "Timberborn.WorkSystem.Workplace").GetInterfaces().Any(x => x.FullName == "Timberborn.EntitySystem.IRegisteredComponent"),
    "Workplaces are registered components, so the entity registry lists every planter building");

// The service's Clear and scan with the planting tool's buildings, on the same instance: one building only the tool shows, one only pinned.
var entityComponent = GameType("Timberborn.EntitySystem", "Timberborn.EntitySystem.EntityComponent");
var shownOnly = RuntimeHelpers.GetUninitializedObject(entityComponent);
var pinnedOnly = RuntimeHelpers.GetUninitializedObject(entityComponent);
var liveEntries = Get<System.Collections.IDictionary>(livePlanner, "_entries");
var livePins = Get<object>(live, "_pins");
livePlanner.GetType().GetMethod("Clear")!.Invoke(livePlanner, null);
var shownSet = Array.CreateInstance(entityComponent, 1);
shownSet.SetValue(shownOnly, 0);
livePlanner.GetType().GetMethod("Show")!.Invoke(livePlanner, new object[] { shownSet });
livePlanner.GetType().GetMethod("Add")!.Invoke(livePlanner, new[] { pinnedOnly });
livePins.GetType().GetMethod("Set")!.Invoke(livePins, new object[] { pinnedOnly, true });
service.GetMethod("ClearAll")!.Invoke(live, null);
Check(liveEntries.Count == 1 && liveEntries.Contains(shownOnly) && (int)service.GetProperty("Count")!.GetValue(live)! == 0,
    "Clear pinned areas removes the pins and keeps drawing the planting tool's buildings");
// UpdateSingleton reads Time.unscaledTime, which only runs in the game, so the scan it starts with is invoked directly.
Set(live, "_planting", null!);
Set(live, "_plantersChanged", true);
service.GetMethod("ShowPlanters", fields)!.Invoke(live, null);
Check(liveEntries.Count == 0 && !Get<bool>(live, "_plantersChanged"), "Leaving the planting tool with no pins drops its buildings at the next update");

// Remembered pins, on the same instance: its pin file in a temporary folder, and the settlement as the game's own service names it.
// SavePins is what the frame update starts with; UpdateSingleton itself only reads Time.unscaledTime (which runs only in the game)
// and passes it to UpdateAt, which the last check here invokes.
var settlementReference = GameType("Timberborn.GameSaveRepositorySystem", "Timberborn.GameSaveRepositorySystem.SettlementReference");
var settlements = RuntimeHelpers.GetUninitializedObject(GameType("Timberborn.SettlementNameSystem", "Timberborn.SettlementNameSystem.SettlementReferenceService"));
Set(live, "_settlements", settlements);
var liveFolder = Path.Combine(Path.GetTempPath(), "pwa-checks-" + Guid.NewGuid().ToString("N"));
var livePinPath = Path.Combine(liveFolder, "Pins.txt");
var pinFileType = mod.GetType("PersistentWorkAreas.PinFile", true)!;
Set(live, "_pinFile", Activator.CreateInstance(pinFileType, livePinPath)!);
void PinEntity(Guid id)
{
    var entity = RuntimeHelpers.GetUninitializedObject(entityComponent);
    Set(entity, "<EntityId>k__BackingField", id);
    livePins.GetType().GetMethod("Set")!.Invoke(livePins, new object[] { entity, true });
}
bool PinsChanged() => (bool)livePins.GetType().GetProperty("Changed")!.GetValue(livePins)!;
List<Guid> Remembered(string settlement) => File.Exists(livePinPath) ? PinStore.Read(File.ReadAllText(livePinPath), settlement) : new List<Guid>();
void SavePins() => service.GetMethod("SavePins", fields)!.Invoke(live, null);
try
{
    PinEntity(idB); PinEntity(idA);
    SavePins();
    Check(!File.Exists(livePinPath) && PinsChanged(), "A new game's pins wait until the player names the settlement");
    Set(settlements, "<SettlementReference>k__BackingField", Activator.CreateInstance(settlementReference, "Beaverton", "Saves")!);
    SavePins();
    Check(Remembered("Beaverton").SequenceEqual(new[] { idA, idB }) && !PinsChanged(), "A pin change is saved under the game's settlement name at the next update");
    File.WriteAllText(livePinPath, PinStore.Write(File.ReadAllText(livePinPath), "Otter Bay", new[] { idC }));
    service.GetMethod("ClearAll")!.Invoke(live, null);
    Check(PinsChanged() && Remembered("Beaverton").Count == 2, "Clear pinned areas leaves the file to the next update");
    SavePins();
    Check(Remembered("Beaverton").Count == 0 && Remembered("Otter Bay").SequenceEqual(new[] { idC }), "Clear pinned areas forgets this settlement's pins and keeps other settlements'");
    PinEntity(idC);
    service.GetMethod("Reset", fields)!.Invoke(live, null);
    SavePins();
    Check(Remembered("Beaverton").SequenceEqual(new[] { idC }) && (int)service.GetProperty("Count")!.GetValue(live)! == 0 && !PinsChanged(),
        "Leaving the map saves a pending pin change, then forgets the pins in memory only");

    // Loading: the settlement's remembered ids go through the game's own EntityRegistry. idD is a building only a newer save has.
    // PostLoad passes Supports, which needs live game objects, so this passes a test that turns down building A.
    var idD = new Guid("f3b1c8e2-7a4d-4c59-9e06-2b8d5f1a6c37");
    File.WriteAllText(livePinPath, PinStore.Write(PinStore.Write(null, "Beaverton", new[] { idA, idB, idD }), "Otter Bay", new[] { idC }));
    var entityRegistry = Activator.CreateInstance(GameType("Timberborn.EntitySystem", "Timberborn.EntitySystem.EntityRegistry"))!;
    object Loaded(Guid id)
    {
        var entity = RuntimeHelpers.GetUninitializedObject(entityComponent);
        Set(entity, "<EntityId>k__BackingField", id);
        Get<System.Collections.IDictionary>(entityRegistry, "_entities")[id] = entity;
        return entity;
    }
    var loadedA = Loaded(idA); var loadedB = Loaded(idB); Loaded(idC);
    Set(live, "_entities", entityRegistry);
    var beforeLoad = File.ReadAllText(livePinPath);
    Func<object, bool> pinnable = x => !ReferenceEquals(x, loadedA);
    service.GetMethod("RestorePins", fields)!.Invoke(live, new object[] { pinnable });
    Check((int)service.GetProperty("Count")!.GetValue(live)! == 1 && (bool)livePins.GetType().GetMethod("Contains")!.Invoke(livePins, new[] { loadedB })! &&
        liveEntries.Count == 1 && liveEntries.Contains(loadedB) && !PinsChanged() && File.ReadAllText(livePinPath) == beforeLoad,
        "Loading pins this settlement's remembered buildings that can be pinned, draws them, and writes nothing yet");
    SavePins();
    Check(File.ReadAllLines(livePinPath)[1].StartsWith("Beaverton\t") && Remembered("Beaverton").SequenceEqual(new[] { idA, idB, idD }) &&
        Remembered("Otter Bay").SequenceEqual(new[] { idC }),
        "The first update after loading moves the settlement first in the pin file and keeps the buildings this save lacks");

    // The frame update, given the clock: it saves before anything else, and waits 10 seconds after a failed write.
    // The failure warning goes to Unity's log, which runs only in the game, so it is marked as already given.
    service.GetMethod("ClearAll")!.Invoke(live, null);
    Set(live, "_active", true);
    Set(live, "_saveWarned", true);
    var goodPinFile = Get<object>(live, "_pinFile");
    Set(live, "_pinFile", Activator.CreateInstance(pinFileType, Path.Combine(livePinPath, "Pins.txt"))!);
    void UpdateAt(float now) => service.GetMethod("UpdateAt", fields)!.Invoke(live, new object[] { now });
    UpdateAt(100f);
    bool retryLater = PinsChanged() && Get<float>(live, "_nextSave") == 110f;
    Set(live, "_pinFile", goodPinFile);
    UpdateAt(109f);
    bool waited = Remembered("Beaverton").Count == 3;
    UpdateAt(110f);
    Check(retryLater && waited && Remembered("Beaverton").Count == 0 && Remembered("Otter Bay").SequenceEqual(new[] { idC }) && !PinsChanged() && liveEntries.Count == 0,
        "The frame update saves a pin change with nothing left to draw, and retries a failed write 10 seconds later");
    Set(live, "_active", false);
}
finally
{
    if (Directory.Exists(liveFolder)) Directory.Delete(liveFolder, true);
}

// The planting tool's rule on the game's own blueprints: crops show farmhouses, trees and bushes show foresters.
var plantables = new List<(string Name, string Group, bool Crop, bool Tree)>();
var planters = new List<(string Name, string Group, bool FarmHouse, bool Forester, bool Outlined)>();
using (var blueprints = ZipFile.OpenRead(Path.Combine(game, "Timberborn_Data", "StreamingAssets", "Modding", "Blueprints.zip")))
    foreach (var entry in blueprints.Entries.Where(x => x.FullName.EndsWith(".blueprint.json", StringComparison.OrdinalIgnoreCase)))
    {
        using var stream = entry.Open();
        using var blueprint = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var root = blueprint.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("TemplateSpec", out var template)) continue;
        var name = template.GetProperty("TemplateName").GetString()!;
        bool Has(string spec) => root.TryGetProperty(spec, out _);
        if (root.TryGetProperty("PlantableSpec", out var plantable))
            plantables.Add((name, plantable.GetProperty("ResourceGroup").GetString()!, Has("CropSpec"), Has("TreeComponentSpec") || Has("BushSpec")));
        if (root.TryGetProperty("PlanterBuildingSpec", out var planter))
            planters.Add((name, planter.GetProperty("PlantableResourceGroup").GetString()!, Has("FarmHouseSpec"), Has("ForesterSpec"),
                Has("WorkplaceSpec") && Has("BuildingAccessibleSpec")));
    }
string[] ShownFor(string plant) => planters.Where(p => PlantingRanges.Shows(plantables.Single(x => x.Name == plant).Group, p.Group))
    .Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();
Check(planters.Count >= 4 && planters.All(p => p.Outlined),
    "Every planter building is a workplace with an access point, so the planting tool finds and outlines it");
Check(plantables.Count >= 20 && plantables.All(x => x.Crop != x.Tree && ShownFor(x.Name).Length > 0), "Every plantable in the game shows at least one planter building");
Check(plantables.All(x => ShownFor(x.Name).All(n => x.Crop ? planters.Single(p => p.Name == n).FarmHouse : planters.Single(p => p.Name == n).Forester)),
    "Crops show only farmhouses; trees and bushes show only foresters");
Check(ShownFor("Carrot").SequenceEqual(new[] { "EfficientFarmHouse.Folktails", "FarmHouse.IronTeeth" }) &&
    ShownFor("Cattail").SequenceEqual(new[] { "AquaticFarmhouse.Folktails" }) &&
    ShownFor("Pine").SequenceEqual(new[] { "Forester.Folktails", "Forester.IronTeeth" }) &&
    ShownFor("BlueberryBush").SequenceEqual(new[] { "Forester.Folktails", "Forester.IronTeeth" }),
    "Carrots show both factions' farmhouses, cattails only the aquatic farmhouse, pines and blueberries only foresters");
var interfaces = service.GetInterfaces().Select(x => x.FullName).ToArray();
Check(interfaces.Contains("Timberborn.SingletonSystem.IPostLoadableSingleton"), "Game post-load lifecycle");
Check(interfaces.Contains("Timberborn.SingletonSystem.IUpdatableSingleton"), "Display refresh lifecycle");
Check(interfaces.Contains("Timberborn.Navigation.ISingletonInstantNavMeshListener") && interfaces.Contains("Timberborn.Navigation.ISingletonPreviewNavMeshListener"), "Live and construction preview invalidation");
Check(!interfaces.Any(x => x.Contains("Saveable") || x.Contains("Tickable") || x.Contains("PersistentEntity")), "No simulation tick or persistence interfaces");
Check(mod.GetTypes().All(t => !t.GetInterfaces().Any(x => x.Namespace?.StartsWith("Timberborn") == true &&
        (x.Name.Contains("Sav") || x.Name.Contains("Persist") || x.Name.Contains("Tick")))) &&
    !mod.GetReferencedAssemblies().Any(x => x.Name is "Timberborn.Persistence" or "Timberborn.WorldPersistence" or "Timberborn.SaveSystem" or "Timberborn.GameSaveRuntimeSystem"),
    "Pins stay out of the save: no mod type saves, persists or ticks, and the mod never references the game's save writers");
Check(!mod.GetReferencedAssemblies().Any(x => x.Name.Contains("BeaverBuddies") || x.Name.Contains("TimberNet") || x.Name.Contains("Harmony")), "No multiplayer or Harmony assembly dependencies");
Check(mod.GetTypes().All(t => !t.GetCustomAttributesData().Any(a => a.AttributeType.Name.StartsWith("HarmonyPatch"))), "No method patches");
var fragment = mod.GetType("PersistentWorkAreas.WorkAreaFragment", true)!;
Check(fragment.GetInterfaces().Any(x => x.FullName == "Timberborn.EntityPanelSystem.IEntityPanelFragment"), "Building checkbox fragment contract");
foreach (var method in new[] { "OnVisibleLevel", "OnConstruction", "OnSelected", "OnUnselected", "OnDeleted", "OnInitialized", "OnToolEntered", "OnToolExited" })
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

// A building as a save knows it: only its entity id survives a load. Equal-valued so that restoring must go by id.
sealed class SavedBuilding
{
    public readonly Guid Id;
    public SavedBuilding(Guid id) { Id = id; }
    public override bool Equals(object obj) => obj is SavedBuilding;
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
