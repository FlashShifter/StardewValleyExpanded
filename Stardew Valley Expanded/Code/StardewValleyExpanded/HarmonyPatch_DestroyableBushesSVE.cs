using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Linq;

namespace StardewValleyExpanded
{
    /// <summary>Makes bushes destroyable under customizable conditions.</summary>
    public static class HarmonyPatch_DestroyableBushesSVE
    {
        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

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

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_DestroyableBushesSVE)}\": postfixing SDV method \"Bush.isDestroyable(GameLocation, Vector2)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Bush), nameof(Bush.isDestroyable)),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_DestroyableBushesSVE), nameof(Bush_isDestroyable))
                );

                Applied = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>Determines whether a bush should be made destroyable.</summary>
        /// <remarks>
        /// This method currently only checks bushes that were NOT already destroyable.
        /// It won't force destroyable bushes to be indestructible (e.g. on some farm types or when players use the Destroyable Bushes mod).
        /// It also does NOT check tea bushes, walnut bushes, or unknown types added by updates/mods.
        /// </remarks>
        /// <param name="bush">The bush being checked.</param>
        /// <param name="location">The in-game location of the bush.</param>
        /// <param name="tile">The tile position of the bush. Typically its left-most "collision" tile.</param>
        /// <returns>True if the bush should be destroyable; false otherwise.</returns>
        public static bool ShouldBeDestroyable(Bush bush, GameLocation location, Vector2 tile)
        {
            if (location?.Name == "Custom_ForestWest")
            {
                if (Game1.getAllFarmers().Any(farmer => farmer.eventsSeen.Contains("746153084")) == true) //if any player has seen this event
                    return true; //bush is destroyable
                else
                    return false;
            }

            return false; //default to false
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>Allows destruction of normally indestructible bushes if this class's custom conditions are met.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        /// <param name="location">The in-game location of the bush.</param>
        /// <param name="tile">The tile position of the bush. Typically its left-most "collision" tile.</param>
        /// <param name="__result">The result of the original method.</param>
        private static void Bush_isDestroyable(Bush __instance, ref bool __result)
        {
            GameLocation location = __instance.Location;
            Vector2 tile = __instance.Tile;
            try
            {
                if (__result) //if this bush is already destroyable
                    return; //do nothing

                if (__instance.size.Value is Bush.smallBush or Bush.mediumBush or Bush.largeBush) //if this bush is one of the target types (to avoid editing tea/walnut bushes or unknown types)
                {
                    if (ShouldBeDestroyable(__instance, location, tile)) //if this bush matches custom conditions
                    {
                        if (Monitor?.IsVerbose == true)
                            Monitor.Log($"Allowing bush destruction. Tile: {tile}. Location: {location?.Name}.", LogLevel.Trace);
                        __result = true; //treat this bush as destroyable
                    }
                    else //if this bush does NOT match custom conditions
                    {
                        if (Monitor?.IsVerbose == true)
                            Monitor.Log($"NOT allowing bush destruction; custom conditions are unmet. Tile: {tile}. Location: {location?.Name}.", LogLevel.Trace);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_DestroyableBushesSVE)}\" has encountered an error. Bushes might be indestructible at some locations. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return;
            }
        }
    }
}
