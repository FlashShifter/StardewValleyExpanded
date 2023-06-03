using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace StardewValleyExpanded
{
    /// <summary>Harmony patches which override <see cref="GameLocation.getFishingLocation"/> for custom SVE locations.</summary>
    public class HarmonyPatch_GetFishingLocation : GameLocation
    {
        /// <summary>Whether this patch is currently applied.</summary>
        public static bool Applied { get; private set; }

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided to this mod by SMAPI. Used for log messages.</param>
        public static void ApplyPatch(Harmony harmony, IMonitor monitor)
        {
            if (Applied)
                return;

            monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_GetFishingLocation)}\": postfixing SDV method \"{nameof(GameLocation)}.{nameof(GameLocation.getFishingLocation)}()\".");
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.getFishingLocation)),
                postfix: new HarmonyMethod(typeof(HarmonyPatch_GetFishingLocation), nameof(After_GameLocation_GetFishingLocation))
            );

            Applied = true;
        }


        /// <summary>Get the area ID to use when selecting fish from <c>Data/Locations</c>.</summary>
        /// <param name="__instance">The game location being fished.</param>
        /// <param name="tile">The player's current tile (*not* the fishing bobber's tile).</param>
        /// <param name="__result">The fishing area ID.</param>
        /// <remarks>Fish with the ID -1 will be available everywhere at a location. Fish with other IDs are only available when the ID matches this method's result.</remarks>
        private static void After_GameLocation_GetFishingLocation(GameLocation __instance, ref Vector2 tile, ref int __result)
        {
            if (__instance?.Name == "Custom_BlueMoonVineyard")
            {
                __result = tile.Y < 30
                    ? 1
                    : 0;
            }

            else if (__instance?.Name == "Custom_Highlands")
            {
                if (tile.X > 121 && tile.Y < 73)
                    __result = 0;
                else
                    __result = 1;
            }

            else if (__instance?.Name == "Custom_FerngillRepublicFrontier")
            {
                __result = tile.Y < 140
                    ? 1
                    : 0;
            }
        }
    }
}
