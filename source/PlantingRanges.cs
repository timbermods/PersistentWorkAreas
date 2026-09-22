using System;

namespace PersistentWorkAreas
{
    // Which buildings' working areas the planting tool shows. Unity-free so the checks can run it on the game's blueprints.
    internal static class PlantingRanges
    {
        // The game pairs a plantable with the planter buildings of its resource group (PlanterBuilding.GetAllowedPlantables):
        // crops with farmhouses, aquatic crops with aquatic farmhouses, trees and bushes with foresters.
        public static bool Shows(string plantableGroup, string planterGroup) =>
            !string.IsNullOrEmpty(plantableGroup) && string.Equals(plantableGroup, planterGroup, StringComparison.Ordinal);
    }
}
