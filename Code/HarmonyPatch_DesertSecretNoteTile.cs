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
    /// <summary>Modifies the position of the Secret Note #18's buried treasure.</summary>
    /// <remarks>
    /// Note that this method only *adds* a valid tile to find the treasure.
    /// It will still appear if players dig at tile 40,55 first (but will only appear once either way).
    /// Removing the Diggable property from tile 40,55 is recommended to avoid confusion.
    /// </remarks>
    public static class HarmonyPatch_DesertSecretNoteTile
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

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_DesertSecretNoteTile)}\": prefixing SDV method \"Desert.checkForBuriedItem(int, int, bool, bool, Farmer)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Desert), nameof(Desert.checkForBuriedItem), new[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(Farmer) }),
                    prefix: new HarmonyMethod(typeof(HarmonyPatch_DesertSecretNoteTile), nameof(Desert_checkForBuriedItem))
                );

                Applied = true;
            }
        }


        /****************/
        /* Mod Settings */
        /****************/


        /// <summary>The new tile to check for Secret Note #18's buried treasure in the desert.</summary>
        /// <remarks>Format: new Vector2(x, y);</remarks>
        public static Vector2 NewSecretNoteTile = new Vector2(9, 43);


        /*****************/
        /* Internal Code */
        /*****************/


        /// <summary>Checks an additional tile in the Desert for Secret Note #18's buried treasure.</summary>
        /// <param name="__instance">The Desert location.</param>
        /// <param name="xLocation">The X value of the tile being checked.</param>
        /// <param name="yLocation">The Y value of the tile being checked.</param>
        /// <param name="who">The player digging.</param>
        /// <param name="__result">The result of the original method.</param>
        /// <returns>True if the original method should run. False if it should be skipped.</returns>
        private static bool Desert_checkForBuriedItem(ref Desert __instance, int xLocation, int yLocation, Farmer who, ref string __result)
        {
            try
            {
                //imitate the original code in Desert.checkForBuriedItem, but check NewSecretNoteTile instead
                if (who.secretNotesSeen.Contains(18) && xLocation == NewSecretNoteTile.X && yLocation == NewSecretNoteTile.Y && !who.mailReceived.Contains("SecretNote18_done"))
                {
                    who.mailReceived.Add("SecretNote18_done");
                    Game1.createObjectDebris("127", xLocation, yLocation, who.UniqueMultiplayerID, __instance);
                    __result = "";
                    return false; //skip the original method
                }
                else return true; //run the original method
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_DesertSecretNoteTile)}\" has encountered an error. Prefix \"{nameof(Desert_checkForBuriedItem)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return true; //run the original method
            }
        }
    }
}
