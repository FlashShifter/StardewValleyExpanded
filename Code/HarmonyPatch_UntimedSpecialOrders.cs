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
    /// <summary>This class modifies a list of special orders to have unlimited durations.</summary>
    public static class HarmonyPatch_UntimedSpecialOrders
    {
        /// <summary>True if this patch is currently applied.</summary>
        public static bool Applied { get; private set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// <param name="helper">The <see cref="IModHelper"/> provided to this mod by SMAPI. Used for events and other API access.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided to this mod by SMAPI. Used for log messages.</param>
        public static void ApplyPatch(Harmony harmony, IModHelper helper, IMonitor monitor)
        {
            if (!Applied && helper != null && monitor != null) //if NOT already applied AND valid tools were provided
            {
                Helper = helper; //store helper
                Monitor = monitor; //store monitor

                Helper.Events.GameLoop.DayEnding += GameLoop_PreventSpecialOrderExpiration;

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_UntimedSpecialOrders)}\": postfixing SDV method \"SpecialOrders.IsTimedQuest()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(SpecialOrder), nameof(SpecialOrder.IsTimedQuest)),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_UntimedSpecialOrders), nameof(SpecialOrders_IsTimedQuest))
                );

                Applied = true;
            }
        }


        /****************/
        /* Mod Settings */
        /****************/


        /// <summary>A list of special order keys (a.k.a. IDs) to give unlimited durations.</summary>
        public static List<string> SpecialOrderKeys = new List<string>()
        {
            "Clint2",
            "Clint3",
            "Lewis2",
            "Morris",
            "Robin3",
            "Robin4",
            "Apples",
            "MarlonFay2",
            "MorrisTod2",
            "MorrisTod3",
            "Lance",
            "Krobus",
            "GilMinecarts",
            "DwarfCaveShortcut",
            "CamillaBridge"
        };


        /*****************/
        /* Internal Code */
        /*****************/


        /// <summary>Gives the listed special orders infinitely long durations.</summary>
        private static void GameLoop_PreventSpecialOrderExpiration(object sender, DayEndingEventArgs e)
        {
            if (!Context.IsMainPlayer) //if this is NOT the main player
                return; //do nothing

            foreach (SpecialOrder order in Game1.player.team.specialOrders) //for each special order the players currently have
            {
                string orderKey = order.questKey.Value; //get this order's key
                if (SpecialOrderKeys.Contains(orderKey, StringComparer.OrdinalIgnoreCase)) //if the key is in this patch's list
                {
                    order.dueDate.Value = Game1.Date.TotalDays + 100000; //update the order's "due date" to prevent expiration and be visibly infinite (in case it gets displayed by unpatched logic)
                }
            }
        }

        /// <summary>Prevents timer displays for the listed special orders where possible.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        /// <param name="__result">The result of the original method.</param>
        private static void SpecialOrders_IsTimedQuest(SpecialOrder __instance, ref bool __result)
        {
            try
            {
                string orderKey = __instance.questKey.Value; //get this order's key
                if (SpecialOrderKeys.Contains(orderKey, StringComparer.OrdinalIgnoreCase)) //if the key is in this patch's list
                {
                    __result = false; //return false
                    return;
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_UntimedSpecialOrders)}\" has encountered an error. Postfix \"{nameof(SpecialOrders_IsTimedQuest)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //do nothing
            }
        }
    }
}
