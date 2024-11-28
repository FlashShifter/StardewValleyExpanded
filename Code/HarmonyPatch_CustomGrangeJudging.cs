using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace StardewValleyExpanded
{
    /// <summary>Allows customization of the grange display judging at the Stardew Valley Fair by replacing hard-coded logic.</summary>
    public static class HarmonyPatch_CustomGrangeJudging
    {

        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

        /// <summary>True if this patch is currently applied.</summary>
        public static bool Applied { get; private set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// /// <param name="helper">The <see cref="IModHelper"/> provided to this mod by SMAPI.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided to this mod by SMAPI. Used for log messages.</param>
        public static void ApplyPatch(Harmony harmony, IModHelper helper, IMonitor monitor)
        {
            if (!Applied && helper != null && monitor != null) //if NOT already enabled
            {
                Helper = helper; //store helper
                Monitor = monitor; //store monitor

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_CustomGrangeJudging)}\": prefixing SDV method \"Event.initiateGrangeJudging()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Event), "initiateGrangeJudging", new Type[0]),
                    prefix: new HarmonyMethod(typeof(HarmonyPatch_CustomGrangeJudging), nameof(Event_initiateGrangeJudging))
                );

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_CustomGrangeJudging)}\": postfixing SDV method \"Event.interpretGrangeResults()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Event), "interpretGrangeResults", new Type[0]),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_CustomGrangeJudging), nameof(Event_interpretGrangeResults))
                );

                Applied = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>The advancedMove where Lewis judges each grange display, then returns to a spot near his starting position. When this ends, "lewisDoneJudgingGrange()" is called.</summary>
        public static string AdvancedMove1 { get; set; } = "advancedMove Lewis False 2 0 0 7 8 0 4 3000 3 0 4 3000 3 0 4 3000 3 0 4 3000 0 1 2 0 0 -1 4 3000 0 3 1 0 1 3000 0 3 2 0 2 3000 -2 0 0 -5 -3 0 0 -1 -14 0 2 1000";
        /// <summary>The advancedMove where Marnie moves down a tile to avoid blocking Lewis's grange display judging.</summary>
        public static string AdvancedMove2 { get; set; } = "advancedMove Marnie False 0 1 4 1000";

        /// <summary>A set of all dialogue added to NPCs during Lewis' grange display judging. Each key is an NPC name. Each value is the target asset with that NPC's dialogue.</summary>
        public static Dictionary<string, string> DialogueWhileJudging { get; set; } = new Dictionary<string, string>()
        {
            //base game dialogue
            /*
            { "Marnie", "Strings\\StringsFromCSFiles:Event.cs.1602" },
            { "Pierre", "Strings\\StringsFromCSFiles:Event.cs.1604" },
            { "Willy", "Strings\\StringsFromCSFiles:Event.cs.1606" },
            */
            
            //SVE dialogue
            { "Sophia", "Strings\\StringsFromCSFiles:SVE_GrangeJudging_Sophia" },
            { "Andy", "Strings\\StringsFromCSFiles:SVE_GrangeJudging_Andy" },
            { "Susan", "Strings\\StringsFromCSFiles:SVE_GrangeJudging_Susan" }
        };

        /// <summary>A set of extra dialogue added to NPCs after grange display judging is complete. Each key is an NPC name. Each value is the target asset with that NPC's dialogue.</summary>
        /// <remarks>
        /// Unlike <see cref="DialogueWhileJudging"/>, this does NOT need to include dialogue for contestants from the base game.
        /// It can replace their dialogue if necessary, though.
        /// </remarks>
        public static Dictionary<string, string> DialogueAfterJudging { get; set; } = new Dictionary<string, string>()
        {
            { "Sophia", "Strings\\StringsFromCSFiles:SVE_AfterJudging_Sophia" },
            { "Andy", "Strings\\StringsFromCSFiles:SVE_AfterJudging_Andy" },
            { "Susan", "Strings\\StringsFromCSFiles:SVE_AfterJudging_Susan" }
        };

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>Performs a modified version of the "Event.initiateGrangeJudging" method.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        /// <returns>True if the original method should run. False if it should be skipped.</returns>
        private static bool Event_initiateGrangeJudging(Event __instance)
        {
            try
            {
                Helper.Reflection.GetMethod(__instance, "judgeGrange", true).Invoke();              //imitate private code from original method:    judgeGrange();
                Helper.Reflection.GetField<string>(__instance, "hostMessageKey", true).SetValue(null); //imitate private code from original method:    hostMessage = null;
                
                NPCController.endBehavior lewisDoneJudgingGrange = Helper.Reflection.GetMethod(__instance, "lewisDoneJudgingGrange", true).MethodInfo.CreateDelegate(typeof(NPCController.endBehavior), __instance) as NPCController.endBehavior; //get the private method "lewisDoneJudgingGrange()" as a delegate for the code below
                
                __instance.setUpAdvancedMove(AdvancedMove1.Split(' '), lewisDoneJudgingGrange); //perform AdvancedMove1, then call __instance.lewisDoneJudgingGrange()
                __instance.getActorByName("Lewis").CurrentDialogue.Clear();
                if (__instance.getActorByName("Marnie") != null)
                {
                    for (int i = __instance.npcControllers.Count - 1; i >= 0; i--)
                    {
                        if (__instance.npcControllers[i].puppet.Name.Equals("Marnie"))
                        {
                            __instance.npcControllers.RemoveAt(i);
                        }
                    }
                }
                __instance.setUpAdvancedMove(AdvancedMove2.Split(' ')); //perform AdvancedMove2

                foreach (NPC actor in __instance.actors)
                {
                    Dialogue dialogue = actor.TryGetDialogue("Fair_Judging");
                    if (dialogue != null)
                    {
                        actor.setNewDialogue(dialogue);
                    }
                }

                return false; //skip the original method
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(Event_initiateGrangeJudging)}\" has encountered an error. The grange display judging event will use default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return true; //run the original method
            }
        }

        /// <summary>Adds additional dialogue to NPCs after the "Event.interpretGrangeResults" method is called.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        private static void Event_interpretGrangeResults(Event __instance)
        {
            try
            {
                foreach (var entry in DialogueAfterJudging) //for each entry in the post-judging dialogue
                {
                    if (__instance.getActorByName(entry.Key) is NPC npc) //if the NPC exists
                        if (Game1.content.LoadStringReturnNullIfNotFound(entry.Value) is string dialogue) //if the dialogue loaded successfully
                            npc.setNewDialogue(new Dialogue(npc, "AfterJudgding", dialogue));
                        else
                            Monitor.Log($"Couldn't load grange judging dialogue. Target asset: \"{entry.Value}\"", LogLevel.Debug);
                    else
                        Monitor.Log($"Couldn't find NPC to load grange judging dialogue. NPC name: \"{entry.Key}\"", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(Event_interpretGrangeResults)}\" has encountered an error. Default dialogue will be used after grange display judging. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return;
            }
        }
    }
}
