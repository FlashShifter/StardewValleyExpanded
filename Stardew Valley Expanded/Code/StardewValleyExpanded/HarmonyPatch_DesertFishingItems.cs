using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;

namespace StardewValleyExpanded
{
    /// <summary>Allows modification of the special item results for players fishing in the Desert.</summary>
    public static class HarmonyPatch_DesertFishingItems
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

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_DesertFishingItems)}\": postfixing SDV method \"Desert.getFish\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getFish)),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_DesertFishingItems), nameof(Desert_getFish))
                );

                Applied = true;
            }
        }

        /****************/
        /* Mod Settings */
        /****************/

        /// <summary>Conditionally replaces the original result of fishing in the Desert with any object.</summary>
        /// <param name="tile">The tile being fished at.</param>
        /// <returns>The object the player fishing should catch. If null, the original result will be used.</returns>
        private static StardewValley.Object OverrideDesertFishingResults(Vector2 tile)
        {
            if (tile.X >= 26 && tile.X <= 33 && tile.Y >= 4 && tile.Y <= 9) //if this tile is between 26,4 and 33,9
            {
                if (Game1.random.NextDouble() < 0.1) //10% chance
                    return new Furniture("2334", Vector2.Zero); //Pyramid Decal
            }

            return null; //if nothing else was chosen, use the normal result
        }

        /*****************/
        /* Internal Code */
        /*****************/

        /// <summary>Adds a chance to catch a Pyramid Decal to additional fishing areas in the Desert.</summary>
        /// <remarks>
        /// The original method includes a 10% chance to catch a Pyramid Decal whenever players fish on tiles where Y > 55 in the Desert.
        /// This patch does NOT disable that check; it adds an additional check that will override any Desert fishing results in if they match its conditions.
        /// </remarks>
        /// <param name="bobberTile">The tile being fished at.</param>
        /// <param name="__result">The result of the original method.</param>
        private static void Desert_getFish(GameLocation __instance, Vector2 bobberTile, ref StardewValley.Item __result)
        {
            if (__instance is not Desert)
                return;

            try
            {                
                if (OverrideDesertFishingResults(bobberTile) is StardewValley.Object newResult) //if the result should be replaced with a new object
                {
                    Monitor.VerboseLog($"Replacing Desert fishing result with new object: \"{newResult?.Name}\"");
                    __result = newResult;
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_DesertFishingItems)}\" has encountered an error. Special item(s) might not be catchable when fishing in the Desert. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }
    }
}
