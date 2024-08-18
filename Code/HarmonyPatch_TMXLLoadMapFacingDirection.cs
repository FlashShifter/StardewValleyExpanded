using System;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
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
    /// <summary>Implements an optional "facing direction" parameter for TMXL's LoadMap tile property.</summary>
    public static class HarmonyPatch_TMXLLoadMapFacingDirection
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

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_TMXLLoadMapFacingDirection)}\": prefixing SDV method \"Game1.warpFarmer(LocationRequest, int, int, int)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Game1), nameof(Game1.warpFarmer), new[] { typeof(LocationRequest), typeof(int), typeof(int), typeof(int) }),
                    prefix: new HarmonyMethod(typeof(HarmonyPatch_TMXLLoadMapFacingDirection), nameof(Game1_warpFarmer))
                );

                Applied = true;
            }
        }

        /// <summary>The index of the parameter to use as the "facing direction" integer of a LoadMap property.</summary>
        /// <remarks>
        /// As of this writing, TMXL LoadMap properties are formatted like this: "TouchAction": "LoadMap mapName x y"
        /// This patch adds an optional parameter like this: "TouchAction": "LoadMap mapName x y facingDirection"
        /// 
        /// If a TMXL update adds other parameters to LoadMap, editing this number might fix this patch.
        /// Note that this is a breaking change; it will require updates for any mods/tiles that use the facing direction property.
        /// </remarks>
        public static int WhichParameterIsFacingDirection { get; set; } = 4;

        /// <summary>Detects the "facing direction" parameter in a TMXL LoadMap tile property and, if found, modifies the local player's facing direction after warping.</summary>
        /// <param name="__instance">The instance calling the original method.</param>
        /// <param name="facingDirectionAfterWarp">The direction the player will face after warping.</param>
        private static void Game1_warpFarmer(Game1 __instance, ref int facingDirectionAfterWarp)
        {
            try
            {
                //check the player's current tile for a LoadMap property and, if found, get its value
                Vector2 tile = Game1.player.Tile;
                string property = Game1.player.currentLocation?.doesTileHaveProperty((int)tile.X, (int)tile.Y, "TouchAction", "Back");

                if (property?.StartsWith("LoadMap", StringComparison.OrdinalIgnoreCase) == true) //if this is a TMXL LoadMap property
                {
                    string[] args = property.Split(' '); //split into separate arguments

                    if (args.Length > WhichParameterIsFacingDirection) //if the facing direction argument exists
                    {
                        if (int.TryParse(args[WhichParameterIsFacingDirection], out int facingDirection) && facingDirection >= 0 && facingDirection <= 3) //if the value is valid
                        {
                            Monitor.VerboseLog($"Applying custom facing direction for LoadMap warp: {facingDirection}");
                            facingDirectionAfterWarp = facingDirection; //edit the original method's argument
                        }
                        else if (string.IsNullOrWhiteSpace(args[WhichParameterIsFacingDirection]) == false) //if the argument was invalid but NOT blank
                        {
                            Monitor.LogOnce($"Couldn't parse the custom 'facing direction' value for a TMXL LoadMap property; ignoring it. Debug information will be displayed below.\nLocation: {Game1.currentLocation?.Name ?? "null"}.\nTile: {$"{tile.X},{tile.Y}"}.\nFacing direction value: \"{args[WhichParameterIsFacingDirection]}\".\nFull property value: \"{property}\".", LogLevel.Debug);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_TMXLLoadMapFacingDirection)}\" has encountered an error. LoadMap tile properties might ignore their custom \"facing direction\" values. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }
    }
}
