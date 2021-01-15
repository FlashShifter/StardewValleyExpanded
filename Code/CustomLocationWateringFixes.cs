using System;
using StardewModdingAPI;
using StardewValley;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

namespace StardewValleyExpanded
{
    /// <summary>A set of fixes </summary>
    public static class CustomLocationWateringFixes
    {
        /// <summary>Locations where crop watering fixes should be applied.</summary>
        /// <remarks>Add new farmable locations here when needed. Capitalization generally doesn't matter.</remarks>
        public static List<string> WateringFixLocations { get; set; } = new List<string>()
        {
            "TownEast",
            "GrandpasShedGreenhouse",
            "GrampletonFields"
        };

        /// <summary>Applies all watering fixes to a preset list of custom locations.</summary>
        /// <param name="helper">This mod's SMAPI helper API, used to apply events and generate log messages.</param>
        /// <param name="harmony">This mod's Harmony instance, used to patch Stardew's code.</param>
        public static void ApplyFixes(IModHelper helper, IMonitor monitor, HarmonyInstance harmony)
        {
            Monitor = monitor; //use the provided monitor

            //add SMAPI events
            helper.Events.GameLoop.DayStarted += DayStarted_ApplyRainAndSprinklers;
            helper.Events.GameLoop.DayEnding += DayEnding_SkipGrowthFix;

            //apply Harmony patches
            HarmonyPatch_FixSprinklerGrowth.ApplyPatch(harmony);
        }

        /// <summary>The SMAPI monitor used to log messages about these fixes. Messages disabled if null.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Implements rain and sprinkler watering for each location in <see cref="WateringFixLocations"/>.</summary>
        /// <remarks>Rain will only be applied to locations where <see cref="GameLocation.IsOutdoors"/> is true.</remarks>
        private static void DayStarted_ApplyRainAndSprinklers(object sender, DayStartedEventArgs e)
        {
            HarmonyPatch_FixSprinklerGrowth.SkipGrowthFix = false; //re-enable the growth fix if necessary

            if (!Context.IsMainPlayer) //if this is NOT the main player
                return; //stop here

            foreach (string name in WateringFixLocations) //for each location to fix
            {
                GameLocation location = Game1.getLocationFromName(name); //get the location

                if (location == null) //if this location doesn't exist
                {
                    Monitor?.LogOnce($"Could not find location \"{name}\" to apply crop watering fixes.", LogLevel.Debug);
                    break; //skip to the next location
                }

                if (Game1.IsRainingHere(location) && location.IsOutdoors) //if it's currently raining at this location AND this location is outdoors
                {
                    int watered = 0; //counter for dirt tiles watered
                    foreach (var pair in location.terrainFeatures.Pairs) //for each TerrainFeature at this location
                    {
                        if (pair.Value is HoeDirt dirt && dirt.state.Value == 0) //if this is unwatered HoeDirt
                        {
                            dirt.state.Value = 1; //water it
                            watered++; //increment the counter
                        }
                    }
                    Monitor?.VerboseLog($"Rain watered {watered} tiles at {location.Name}");
                }
                else if (!Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER")) //if this location was NOT watered by rain, but sprinklers are enabled
                {
                    int activated = 0; //counter for sprinklers activated
                    foreach (StardewValley.Object obj in location.Objects.Values) //for each object at this location
                    {
                        if (obj.IsSprinkler() && obj.GetModifiedRadiusForSprinkler() >= 0) //if this object is a working sprinkler
                        {
                            foreach (Vector2 current in obj.GetSprinklerTiles()) //for each tile in range of this sprinkler
                            {
                                obj.ApplySprinkler(location, current); //water the tile
                            }
                            obj.ApplySprinklerAnimation(location); //activate this sprinkler's animation (NOTE: this animation disappears on warp for custom locations)
                            activated++; //increment the counter
                        }
                    }
                    Monitor?.VerboseLog($"Activated {activated} sprinklers at {location.Name}");
                }
            }          
        }

        /// <summary>Prevents the sprinkler growth fix being applied when a day ends normally (i.e. a save was not loaded).</summary>
        private static void DayEnding_SkipGrowthFix(object sender, DayEndingEventArgs e)
        {
            HarmonyPatch_FixSprinklerGrowth.SkipGrowthFix = true; //disable the growth fix
        }

        /// <summary>A Harmony patch that fixes a crop growth issue near sprinklers in custom locations.</summary>
        private static class HarmonyPatch_FixSprinklerGrowth
        {
            /// <summary>While true, this patch's fix should not be applied.</summary>
            public static bool SkipGrowthFix = false;

            public static void ApplyPatch(HarmonyInstance harmony)
            {
                Monitor?.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FixSprinklerGrowth)}\": prefixing SDV method \"Crop.newDay(int, int, int, int, GameLocation)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Crop), nameof(Crop.newDay), new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(GameLocation) }),
                    prefix: new HarmonyMethod(typeof(HarmonyPatch_FixSprinklerGrowth), nameof(newDay))
                );
            }

            /// <summary>A Harmony prefix patch that fixes a crop growth issue near sprinklers in custom locations. Sets crops to an unwatered state, avoiding excess growth.</summary>
            private static void newDay(Crop __instance, ref int state, int xTile, int yTile, GameLocation environment)
            {
                try
                {
                    if (SkipGrowthFix) //if this fix should not currently be applied
                        return; //do nothing

                    if (WateringFixLocations.Contains(environment.Name, StringComparer.OrdinalIgnoreCase)) //if the list of locations to fix includes this crop's location
                        if (state == 1) //if this crop is watered
                            state = 0; //remove the water
                }
                catch (Exception ex)
                {
                    Monitor?.LogOnce($"Encountered an error in Harmony patch \"{nameof(HarmonyPatch_FixSprinklerGrowth)}\". Crops near sprinklers on custom maps might grow twice when loading a saved game. Full error message:\n-----\n{ex.ToString()}", LogLevel.Error);
                }
            }
        }
    }
}
