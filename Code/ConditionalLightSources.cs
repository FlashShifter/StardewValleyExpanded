using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace StardewValleyExpanded
{
    /// <summary>Adds light sources based on custom conditions.</summary>
    public static class ConditionalLightSources
    {
        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

        /// <summary>True if this class's features are currently enabled.</summary>
        public static bool Enabled { get; private set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Enables this class's features by setting up SMAPI events.</summary>
        /// <param name="helper">A SMAPI helper instance, used to set up events.</param>
        public static void Enable(IModHelper helper, IMonitor monitor)
        {
            if (!Enabled && helper != null && monitor != null) //if not already enabled AND valid tools were provided
            {
                Helper = helper; //store the helper
                Monitor = monitor; //store the monitor

                Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
                Helper.Events.Player.Warped += Player_Warped;

                Enabled = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>Conditionally adds light sources to the player's location.</summary>
        /// <param name="location">The local player's current location (or warp destination).</param>
        public static void UpdateLights(GameLocation location)
        {
            if (!Context.IsWorldReady)
                return;

            if (location is FarmHouse) //if this is a farmhouse or cabin
            {
                for (int x = 0; x < location.Map.Layers[0].TileWidth; x++)
                {
                    for (int y = 0; y < location.Map.Layers[0].TileHeight; y++)
                    {
                        if (location.getTileIndexAt(x, y, "Front") == 3189) //if this tile should have a lightsource
                        {
                            //add a light to it
                            Game1.currentLightSources.Add($"SVE_FH_{x}_{y}_FrontTile3189", new LightSource(
                                $"SVE_FH_{x}_{y}_FrontTile3189",
                                LightSource.sconceLight,                    //light type (affects shape; see the Light map property)
                                new Vector2((x * 64) + 32, (y * 64) + 32),  //pixel position
                                1f,                                         //radius (2f = double size, etc; higher values may ignore color)
                                new Color(127, 127, 0, 191),                //tint (default is Color.Black; use Color.Name or new Color(R,G,B,A))
                                LightSource.LightContext.None               //use WindowLight to disable during night/rain
                            ));
                        }
                    }
                }
            }
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        private static void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            UpdateLights(Game1.player.currentLocation);
        }
        private static void Player_Warped(object sender, WarpedEventArgs e)
        {
            UpdateLights(e.NewLocation);
        }
    }
}
