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
using System.Reflection.Emit;
using System.Reflection;
using StardewValley.Network;

namespace StardewValleyExpanded
{
    public static class HarmonyPatch_SpousePatioAnimations
    {
        public static bool IsApplied { get; private set; } = false;

        private static IMonitor Monitor { get; set; } = null;

        public static void ApplyPatch(Harmony harmony, IMonitor monitor = null)
        {
            if (IsApplied)
                return;
            IsApplied = true;

            Monitor = monitor; //use provided monitor
            
            //enable Harmony patch(es)
            Monitor?.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_SpousePatioAnimations)}\": postfixing SDV method \"NPC.doPlaySpousePatioAnimation\".", LogLevel.Trace);
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "doPlaySpousePatioAnimation"),
                postfix: new HarmonyMethod(typeof(HarmonyPatch_SpousePatioAnimations), nameof(NPC_doPlaySpousePatioAnimation_Postfix))
            );
        }

        public static void RemovePatch(Harmony harmony, IMonitor monitor = null)
        {
            if (!IsApplied)
                return;
            IsApplied = false;

            Monitor = monitor; //use provided monitor

            //disable Harmony patch(es)
            Monitor?.Log($"Removing Harmony patch \"{nameof(HarmonyPatch_SpousePatioAnimations)}\": postfix on SDV method \"NPC.doPlaySpousePatioAnimation\".", LogLevel.Trace);
            harmony.Unpatch(
                original: AccessTools.Method(typeof(NPC), "doPlaySpousePatioAnimation"),
                HarmonyPatchType.Postfix,
                harmony.Id
            );
        }

        [HarmonyPriority(Priority.High)]
        /// <summary>Adds new spouse patio animations for certain NPCs.</summary>
        public static void NPC_doPlaySpousePatioAnimation_Postfix(NPC __instance)
        {
            try
            {
                switch (__instance.Name) //based on this NPC's name
                {
                    case "Sophia":
                        __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> //create and set this NPC's spouse patio animation
                        {
                            //Sophia animation
                            new FarmerSprite.AnimationFrame(22, 9000),
                            new FarmerSprite.AnimationFrame(23, 500),
                            new FarmerSprite.AnimationFrame(22, 11000),
                            new FarmerSprite.AnimationFrame(23, 500),
                            new FarmerSprite.AnimationFrame(22, 6000),
                            new FarmerSprite.AnimationFrame(21, 250),
                            new FarmerSprite.AnimationFrame(20, 250),
                            new FarmerSprite.AnimationFrame(0, 6000),
                            new FarmerSprite.AnimationFrame(4, 4500),
                            new FarmerSprite.AnimationFrame(0, 250),
                            new FarmerSprite.AnimationFrame(12, 5000),
                            new FarmerSprite.AnimationFrame(0, 4000),
                            new FarmerSprite.AnimationFrame(39, 3000),
                            new FarmerSprite.AnimationFrame(0, 3000),
                            new FarmerSprite.AnimationFrame(20, 250),
                            new FarmerSprite.AnimationFrame(21, 250)

                        });
                        break;

                    case "Victor":
                        __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> //create and set this NPC's spouse patio animation
                        {
                            //Victor animation
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(19, 4000),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(18, 150),
                            new FarmerSprite.AnimationFrame(17, 150),
                            new FarmerSprite.AnimationFrame(16, 150),
                            new FarmerSprite.AnimationFrame(19, 3000),
                            new FarmerSprite.AnimationFrame(20, 2500),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(21, 150),
                            new FarmerSprite.AnimationFrame(22, 150),
                            new FarmerSprite.AnimationFrame(20, 4000),
                            new FarmerSprite.AnimationFrame(19, 2500)
                        });
                        break;

                    case "Olivia":
                        __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> //create and set this NPC's spouse patio animation
                        {
                            //Olivia animation
                            new FarmerSprite.AnimationFrame(20, 6000),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(23, 2000),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(20, 6000),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(23, 2000),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(20, 6000),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(23, 2000),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(20, 6000),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(23, 2000),
                            new FarmerSprite.AnimationFrame(22, 125),
                            new FarmerSprite.AnimationFrame(21, 125),
                            new FarmerSprite.AnimationFrame(0, 8500)
                        });
                        break;


                    case "Wizard":
                        __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> //create and set this NPC's spouse patio animation
                        {
                            //Magnus animation
                            new FarmerSprite.AnimationFrame(0, 35000),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(21, 250),
                            new FarmerSprite.AnimationFrame(20, 70000),
                            new FarmerSprite.AnimationFrame(21, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                        });
                        break;

                    case "Claire":
                        __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> //create and set this NPC's spouse patio animation
                        {
                            //Claire animation
                            new FarmerSprite.AnimationFrame(0, 4500),
                            new FarmerSprite.AnimationFrame(20, 2000),
                            new FarmerSprite.AnimationFrame(21, 2000),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 600),
                            new FarmerSprite.AnimationFrame(21, 300),
                            new FarmerSprite.AnimationFrame(20, 2000),
                            new FarmerSprite.AnimationFrame(0, 4000),
                            new FarmerSprite.AnimationFrame(20, 2000),
                            new FarmerSprite.AnimationFrame(21, 2000),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 250),
                            new FarmerSprite.AnimationFrame(23, 250),
                            new FarmerSprite.AnimationFrame(24, 250),
                            new FarmerSprite.AnimationFrame(25, 250),
                            new FarmerSprite.AnimationFrame(22, 600),
                            new FarmerSprite.AnimationFrame(21, 300),
                            new FarmerSprite.AnimationFrame(20, 2000),
                            new FarmerSprite.AnimationFrame(0, 4000),
                            new FarmerSprite.AnimationFrame(35, 15000)
                        });
                        break;

                    default: //if the name did not match anything above here
                        break; //do nothing
                };

            }
            catch (Exception ex)
            {
                Monitor?.LogOnce($"Harmony patch \"{nameof(NPC_doPlaySpousePatioAnimation_Postfix)}\" has encountered an error and will not be applied:\n{ex.ToString()}", LogLevel.Error);
            }
        }
    }
}
