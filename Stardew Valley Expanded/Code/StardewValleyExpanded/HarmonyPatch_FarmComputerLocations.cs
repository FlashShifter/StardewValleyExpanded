using System;
using StardewModdingAPI;
using StardewValley;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.Locations;
using StardewValley.Monsters;
using System.Diagnostics;
using StardewValley.Objects;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Events;
using StardewValley.Characters;
using xTile.Dimensions;
using Netcode;
using StardewValley.Network;
using System.Reflection.Emit;
using System.Reflection;
using xTile.ObjectModel;

namespace StardewValleyExpanded
{
    /// <summary></summary>
    public static class HarmonyPatch_FarmComputerLocations
    {
        /// <summary>True if this patch is currently applied.</summary>
        public static bool Applied { get; private set; } = false;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided to this mod by SMAPI. Used for log messages.</param>
        public static void ApplyPatch(Harmony harmony, IMonitor monitor)
        {
            if (!Applied && monitor != null) //if NOT already applied
            {
                Monitor = monitor; //store monitor

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\": postfixing SDV method \"Farm.getTotalCrops()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getTotalCrops), new Type[0]),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_FarmComputerLocations), nameof(Farm_getTotalCrops))
                );

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\": postfixing SDV method \"Farm.getTotalCropsReadyForHarvest()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getTotalCropsReadyForHarvest), new Type[0]),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_FarmComputerLocations), nameof(Farm_getTotalCropsReadyForHarvest))
                );

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\": postfixing SDV method \"Farm.getTotalUnwateredCrops()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getTotalUnwateredCrops), new Type[0]),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_FarmComputerLocations), nameof(Farm_getTotalUnwateredCrops))
                );

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\": postfixing SDV method \"Farm.getTotalOpenHoeDirt()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getTotalOpenHoeDirt), new Type[0]),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_FarmComputerLocations), nameof(Farm_getTotalOpenHoeDirt))
                );

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\": postfixing SDV method \"Farm.getTotalGreenhouseCropsReadyForHarvest()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getTotalGreenhouseCropsReadyForHarvest), new Type[0]),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_FarmComputerLocations), nameof(Farm_getTotalGreenhouseCropsReadyForHarvest))
                );

                Applied = true;
            }
        }

        /****************/
        /* Mod Settings */
        /****************/

        /// <summary>A list of extra location names the Farm Computer should count for "Total Crops", "Crops Ready", "Unwatered Crops", and "Open Tilled Soil".</summary>
        /// <remarks>
        /// As of SDV 1.5.4, the original code only checks the "Farm" location.
        /// Add any other farm location names to this list.
        /// 
        /// The Farm Computer does NOT normally count greenhouse locations in these numbers, so don't add those unless you want to change that behavior.
        /// </remarks>
        public static List<string> FarmLocations = new List<string>()
        {
            "Custom_TownEast", "Custom_Garden", "Custom_ForestWest"
        };

        /// <summary>A list of extra location names the Farm Computer should count for "Crops Ready In Greenhouse".</summary>
        /// <remarks>
        /// As of SDV 1.5.4, the original code only checks the "Greenhouse" location.
        /// Add any other location names to this list.
        /// </remarks>
        public static List<string> GreenhouseLocations = new List<string>()
        {
            "Custom_GrandpasShedGreenhouse"
        };

        /// <summary>Adds any extra farm locations' crops to this method's total.</summary>
        /// <param name="__result">The result of the original method.</param>
        private static void Farm_getTotalCrops(ref int __result)
        {
            try
            {
                foreach (string name in FarmLocations) //for each extra farm location
                {
                    if (name.Equals("Farm", StringComparison.OrdinalIgnoreCase)) //if the extra location list included "Farm"
                        continue; //skip it

                    GameLocation loc = Game1.getLocationFromName(name); //get the the current location
                    if (loc != null) //if the location exists
                    {
                        //imitate the original method's crop counting logic
                        int amount = 0;
                        foreach (TerrainFeature t in loc.terrainFeatures.Values)
                        {
                            if (t is HoeDirt && (t as HoeDirt).crop != null && !(t as HoeDirt).crop.dead.Value)
                            {
                                amount++;
                            }
                        }
                        __result += amount; //add the amount to the original method's result
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\" has encountered an error. Postfix \"{nameof(Farm_getTotalCrops)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }

        /// <summary>Adds any extra farm locations' crops to this method's total.</summary>
        /// <param name="__result">The result of the original method.</param>
        private static void Farm_getTotalCropsReadyForHarvest(ref int __result)
        {
            try
            {
                foreach (string name in FarmLocations) //for each extra farm location
                {
                    if (name.Equals("Farm", StringComparison.OrdinalIgnoreCase)) //if the extra location list included "Farm"
                        continue; //skip it

                    GameLocation loc = Game1.getLocationFromName(name); //get the the current location
                    if (loc != null) //if the location exists
                    {
                        //imitate the original method's crop counting logic
                        int amount = 0;
                        foreach (TerrainFeature t in loc.terrainFeatures.Values)
                        {
                            if (t is HoeDirt && (t as HoeDirt).readyForHarvest())
                            {
                                amount++;
                            }
                        }
                        __result += amount; //add the amount to the original method's result
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\" has encountered an error. Postfix \"{nameof(Farm_getTotalCropsReadyForHarvest)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }

        /// <summary>Adds any extra farm locations' crops to this method's total.</summary>
        /// <param name="__result">The result of the original method.</param>
        private static void Farm_getTotalUnwateredCrops(ref int __result)
        {
            try
            {
                foreach (string name in FarmLocations) //for each extra farm location
                {
                    if (name.Equals("Farm", StringComparison.OrdinalIgnoreCase)) //if the extra location list included "Farm"
                        continue; //skip it

                    GameLocation loc = Game1.getLocationFromName(name); //get the the current location
                    if (loc != null) //if the location exists
                    {
                        //imitate the original method's crop counting logic
                        int amount = 0;
                        foreach (TerrainFeature t in loc.terrainFeatures.Values)
                        {
                            if (t is HoeDirt && (t as HoeDirt).crop != null && (t as HoeDirt).needsWatering() && (int)(t as HoeDirt).state.Value != 1)
                            {
                                amount++;
                            }
                        }
                        __result += amount; //add the amount to the original method's result
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\" has encountered an error. Postfix \"{nameof(Farm_getTotalUnwateredCrops)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }

        /// <summary>Adds any extra farm locations' crops to this method's total.</summary>
        /// <param name="__result">The result of the original method.</param>
        private static void Farm_getTotalOpenHoeDirt(ref int __result)
        {
            try
            {
                foreach (string name in FarmLocations) //for each extra farm location
                {
                    if (name.Equals("Farm", StringComparison.OrdinalIgnoreCase)) //if the extra location list included "Farm"
                        continue; //skip it

                    GameLocation loc = Game1.getLocationFromName(name); //get the the current location
                    if (loc != null) //if the location exists
                    {
                        //imitate the original method's crop counting logic
                        int amount = 0;
                        foreach (TerrainFeature t in loc.terrainFeatures.Values)
                        {
                            if (t is HoeDirt && (t as HoeDirt).crop == null && !loc.objects.ContainsKey(t.Tile))
                            {
                                amount++;
                            }
                        }
                        __result += amount; //add the amount to the original method's result
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\" has encountered an error. Postfix \"{nameof(Farm_getTotalOpenHoeDirt)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }

        /// <summary>Adds any extra greenhouse locations' crops to this method's total.</summary>
        /// <param name="__result">The result of the original method.</param>
        private static void Farm_getTotalGreenhouseCropsReadyForHarvest(ref int? __result)
        {
            try
            {
                foreach (string name in GreenhouseLocations) //for each extra greenhouse location
                {
                    if (name.Equals("Greenhouse", StringComparison.OrdinalIgnoreCase)) //if the extra location list included "Greenhouse"
                        continue; //skip it

                    GameLocation loc = Game1.getLocationFromName(name); //get the the current location
                    if (loc != null) //if the location exists
                    {
                        //imitate the original method's crop counting logic
                        int amount = 0;
                        foreach (TerrainFeature t in loc.terrainFeatures.Values)
                        {
                            if (t is HoeDirt && (t as HoeDirt).readyForHarvest())
                            {
                                amount++;
                            }
                        }
                        __result += amount; //add the amount to the original method's result
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FarmComputerLocations)}\" has encountered an error. Postfix \"{nameof(Farm_getTotalGreenhouseCropsReadyForHarvest)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }
    }
}
