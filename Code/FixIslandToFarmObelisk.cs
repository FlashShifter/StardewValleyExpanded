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
    /// <summary>Fixes an issue with IslandWest's farm obelisk ignoring the farm's "WarpTotemEntry" property.</summary>
    public static class FixIslandToFarmObelisk
    {
        /// <summary>True if this fix is currently enabled.</summary>
        private static bool Enabled { get; set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided to this mod by SMAPI. Used for log messages.</param>
        public static void Enable(IModHelper helper, IMonitor monitor)
        {
            if (!Enabled && helper != null && monitor != null) //if NOT already enabled
            {
                Helper = helper; //store helper
                Monitor = monitor; //store monitor

                //enable SMAPI event(s)
                Helper.Events.Player.Warped += Warped_FixObeliskFarmTile;

                Enabled = true;
            }
        }

        /// <summary>When the local player warps from IslandWest to Farm and arrives as the "unfixed" default tile, this moves that player to the "TotemWarpEntry" tile if necessary.</summary>
        private static void Warped_FixObeliskFarmTile(object sender, WarpedEventArgs e)
        {
            if (e.Player == Game1.player && e.OldLocation is IslandWest west && e.NewLocation is Farm farm) //if the local player warped from IslandWest to Farm
            {
                Point unfixedTile; //the "unfixed" arrival tile that SDV uses (as of SDV 1.5.3)

                //get SDV's hard-coded default warp tiles for each farm type
                if (Game1.whichFarm == 5) //Four Corners
                {
                    unfixedTile = new Point(48, 39);
                }
                else if (Game1.whichFarm == 6) //Beach
                {
                    unfixedTile = new Point(82, 29);
                }
                else //any other farm type
                {
                    unfixedTile = new Point(48, 7);
                }

                if (Game1.player.getTileLocationPoint().Equals(unfixedTile)) //if the player warped to the "unfixed" tile
                {
                    Point fixedTile = Game1.getFarm().GetMapPropertyPosition("WarpTotemEntry", (int)unfixedTile.X, (int)unfixedTile.Y); //get the customizable map property tile if available
                    if (fixedTile.Equals(unfixedTile) == false) //if the "fixed" tile is different from the "unfixed" tile
                    {
                        Game1.player.setTileLocation(new Vector2(fixedTile.X, fixedTile.Y +1)); //warp to the "fixed" tile
                        Monitor?.Log($"IslandWest -> Farm obelisk warp detected. Moving local player from {unfixedTile.X},{unfixedTile.Y} to {fixedTile.X},{fixedTile.Y}.", LogLevel.Trace);
                    }
                }
            }
        }
    }
}
