using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System;
using SObject = StardewValley.Object;

namespace StardewValleyExpanded
{
    /// <summary>Adds a player interaction with SVE's piggy bank object.</summary>
    public static class HarmonyPatch_PiggyBank
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
                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_PiggyBank)}\": postfixing SDV method \"Object_checkForAction(Farmer, bool)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction), new[] { typeof(Farmer), typeof(bool) }),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_PiggyBank), nameof(Object_checkForAction))
                );

                Applied = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>The internal name of SVE's piggy bank object. Used to identify in-game instances.</summary>
        public static string PiggyBankName = "Golden Piggy Bank";

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>Activates the piggy bank interaction when a matching object is used.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        /// <param name="who">The farmer performing this action.</param>
        /// <param name="__result">The result of the original method.</param>
        private static void Object_checkForAction(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
        {
            try
            {
                if (__result == false && __instance.bigCraftable.Value) //if this is a BC that did NOT successfully perform an action
                {
                    if (__instance.Name == PiggyBankName) //if this item is SVE's piggy bank (TODO: if name conflicts arise, convert this check to use "QualifiedItemID" and/or JsonAssets' API)
                    {
                        __result = InteractWithPiggyBank(who, __instance, justCheckingForActivity); //try to perform the piggy bank action & override the original result
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_PiggyBank)}\" has encountered an error. SVE's piggy bank might stop working. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return;
            }
        }

        /// <summary>Performs the piggy bank interaction.</summary>
        /// <param name="who">The player interacting with the piggy bank.</param>
        /// <param name="piggy">The piggy bank object.</param>
        /// <returns>True if the interaction was successfully handled. False if this method could not handle the interaction.</returns>
        private static bool InteractWithPiggyBank(Farmer who, SObject piggy, bool justCheckingForActivity)
        {
            if (who == null) //if the action wasn't initiated by a player (uncaught "probing" logic, etc)
                return false; //handling failed

            if (who.ActiveObject != null) //if the player is holding an object (basically anything other than a tool)
                return false; //handling failed

            if (!justCheckingForActivity)
            {
                if (who.Money > 0) //if this player at least 1 gold
                {
                    who.Money--;
                    who.currentLocation.playSound("money");
                    piggy.shakeTimer = 100;
                }
                else
                {
                    who.currentLocation.playSound("cancel");
                }
            }

            return true; //handling succeeded
        }
    }
}
