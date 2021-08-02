using System;
using StardewModdingAPI;
using HarmonyLib;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.GameData.FishPond;

namespace StardewValleyExpanded
{
    /// <summary></summary>
    public static class HarmonyPatch_CustomFishPondColors
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

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_CustomFishPondColors)}\": postfixing SDV method \"FishPond.doFishSpecificWaterColoring()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(FishPond), "doFishSpecificWaterColoring"),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_CustomFishPondColors), nameof(FishPond_doFishSpecificWaterColoring))
                );

                Applied = true;
            }
        }


        /****************/
        /* Mod Settings */
        /****************/


        /// <summary>A set of fish item tags and custom fish pond colors.</summary>
        /// <remarks>
        /// Use one tag from "RequiredTags" in the "Data/FishPondData" entry used by the target fish.
        /// If a fish pond is currently using data with that tag, this will apply the associated color to the pond.
        /// Using "Color.White" will apply the default pond color.
        /// </remarks>
        public static Dictionary<string, Color> FishDataTagsAndPondColors { get; set; } = new Dictionary<string, Color>()
        {
            {
                "fish_void_eel",
                new Color(120, 20, 110)
            }
        };


        /*****************/
        /* Internal Code */
        /*****************/


        /// <summary>Applies customized colors to fish ponds based on the fish's name.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        private static void FishPond_doFishSpecificWaterColoring(FishPond __instance, FishPondData ____fishPondData)
        {
            try
            {
                if (__instance.currentOccupants.Value > 2 && ____fishPondData?.RequiredTags?.Count > 0) //if this pond has enough fish to be colored AND has loaded fish tag data
                {
                    foreach (string tag in ____fishPondData.RequiredTags) //for each tag required by this pond's data
                    {
                        if (FishDataTagsAndPondColors.TryGetValue(tag, out Color colorForThisTag)) //if this class has a custom color for this fish tag
                        {
                            __instance.overrideWaterColor.Value = colorForThisTag; //apply this color
                            return; //stop here
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_CustomFishPondColors)}\" has encountered an error. Custom fish pond colors might not be applied. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return;
            }
        }
    }
}
