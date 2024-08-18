using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System;
using SObject = StardewValley.Object;

namespace StardewValleyExpanded
{
    /// <summary>Adds a player interaction with SVE's cat stat object.</summary>
    public static class HarmonyPatch_CatStatue
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
                //store utilities
                Monitor = monitor;

                //apply patches
                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_CatStatue)}\": postfixing SDV method \"Object.DayUpdate(GameLocation)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(SObject), nameof(SObject.DayUpdate), new Type[0]),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_CatStatue), nameof(Object_DayUpdate))
                );

                Applied = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>The internal name of SVE's cat statue object. Used to identify in-game instances.</summary>
        public static string CatStatueName = "Statue Of Treasure";

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>Updates the contents of SVE's cat statue at the start of each day.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        /// <param name="loc">The in-game location of the object.</param>
        private static void Object_DayUpdate(SObject __instance)
        {
            try
            {
                if (__instance.bigCraftable.Value && __instance.Name.Equals(CatStatueName, StringComparison.Ordinal)) //if this item is SVE's cat statue (TODO: if name conflicts arise, convert this check to use "QualifiedItemID" and/or JsonAssets' API)
                {
                    if (__instance.heldObject.Value == null) //if this does NOT already contain an item
                    {
                        //add an item to the statue (note: uses the utility to improve support for SDV 1.6)
                        __instance.MinutesUntilReady = 1;
                        __instance.heldObject.Value = Utility.fuzzyItemSearch("Artifact Trove", 1) as SObject;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_CatStatue)}\" has encountered an error. SVE's cat statue might stop working. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return;
            }
        }
    }
}
