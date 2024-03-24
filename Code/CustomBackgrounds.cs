using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace StardewValleyExpanded
{
    /// <summary>Renders customizable backgrounds, similar to SDV's Summit location.</summary>
    public static class CustomBackgrounds
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

                Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted_UpdateBackgrounds;
                Helper.Events.Player.Warped += Player_Warped_UpdateBackgrounds;
                Helper.Events.GameLoop.TimeChanged += GameLoop_TimeChanged_UpdateBackgrounds;

                Helper.Events.GameLoop.ReturnedToTitle += GameLoop_ReturnedToTitle_ClearBackgrounds;

                Helper.Events.Display.RenderingStep += Display_RenderingWorld_DrawBackgrounds;
                Helper.Events.Display.RenderedWorld += Display_RenderedWorld_DrawForegrounds;

                Enabled = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        private static Rectangle highlandsCloudZone = new Rectangle(58, 0, 151, 8); //tiles where clouds can spawn: min X, min Y, max X, max Y
        private static Random r = new Random();

        /// <summary>Adds or updates the set of currently active backgrounds.</summary>
        /// <remarks>
        /// Backgrounds are stored in the <see cref="ActiveBackgrounds"/> dictionary with unique string keys.
        /// See the <see cref="CustomBackground"/> class for information on available settings.
        /// Note that overwriting an existing background with a <see cref="CustomBackground.Movement"/> setting will cause it to reset.
        /// </remarks>
        private static void UpdateBackgrounds()
        {
            if (!Context.IsWorldReady)
                return;

            if (r == null)
                r = new Random((int)(Game1.uniqueIDForThisGame % 1000) + SDate.Now().DaysSinceStart); //use game ID + day count as a seed for randomization

            if (ActiveBackgrounds.Count == 0) //if no backgrounds are currently loaded
            {
                //add any backgrounds that should always exist
                ActiveBackgrounds["Highlands Background"] = new CustomBackground("Custom_Highlands", "Maps/HighlandsBackground")
                {
                    RepeatX = true,
                    Parallax = new Vector2(0.2f, 0.2f),
                };
                /*
                ActiveBackgrounds["Custom summit Background"] = new CustomBackground("Custom_SVESummit", "Maps/HighlandsBackground")
                {
                    RepeatX = true,
                    Parallax = new Vector2(0.2f, 0.2f),
                };
                */

                //
                //commented out below: small cloud sprite creation
                //
                /***
                
                for (int numberOfClouds = r.Next(1, 6); numberOfClouds > 0; numberOfClouds--) //generate 1 to 5 clouds (note the max number is exclusive)
                {
                    //choose a random tile and convert it to pixels
                    Vector2 position = new Vector2(r.Next(highlandsCloudZone.X, highlandsCloudZone.Width), r.Next(highlandsCloudZone.Y, highlandsCloudZone.Height)) * 64;

                    ActiveBackgrounds["Highlands Cloud " + numberOfClouds] = new CustomBackground("Custom_Highlands", "LooseSprites/Cursors")
                    {
                        TextureArea = new Rectangle(230, 1303, 58, 19),
                        Position = position,
                        Parallax = new Vector2(0.1f, 0.1f),
                        Movement = new Vector2(0.2f, 0f),
                        Priority = 1 //higher than the main background
                    };
                }

                ***/
            }

            //add conditional backgrounds below or update/replace existing ones

            //
            //commented out below: logic that resets cloud positions if they leave the screen
            //
            /***
             
            foreach (var background in ActiveBackgrounds)
            {
                if (background.Key.StartsWith("Highlands Cloud", StringComparison.Ordinal)) //if this is a cloud
                {
                    if (background.Value.Position.X >= 9984) //if this cloud is completely past the right edge of the map
                    {
                        //move it before the left edge of the background & randomly choose a new height
                        background.Value.Position = new Vector2(53, r.Next(highlandsCloudZone.Y, highlandsCloudZone.Height)) * 64;
                    }
                }
            }

            ***/
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>A set of all active custom backgrounds.</summary>
        private static Dictionary<string, CustomBackground> ActiveBackgrounds = new();

        private static void GameLoop_DayStarted_UpdateBackgrounds(object sender, DayStartedEventArgs e)
        {
            r = null; //reset random seed
            ActiveBackgrounds.Clear(); //reset backgrounds each day
            UpdateBackgrounds();
        }

        private static void GameLoop_TimeChanged_UpdateBackgrounds(object sender, TimeChangedEventArgs e)
        {
            UpdateBackgrounds();
        }

        private static void Player_Warped_UpdateBackgrounds(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
                UpdateBackgrounds();
        }
        private static void GameLoop_ReturnedToTitle_ClearBackgrounds(object sender, ReturnedToTitleEventArgs e)
        {
            ActiveBackgrounds.Clear();
        }

        /// <summary>Draws each active background for the current player's location.</summary>
        private static void Display_RenderingWorld_DrawBackgrounds(object sender, RenderingStepEventArgs e)
        {
            if (e.Step != StardewValley.Mods.RenderSteps.World_Background)
                return;

            foreach (var entry in ActiveBackgrounds)
            {
                //if this is NOT a foreground and its location matches the player's (or its location is null)
                if (!entry.Value.Foreground &&
                    (entry.Value.LocationName == null ||
                    string.Equals(entry.Value.LocationName, Game1.player.currentLocation?.Name, System.StringComparison.OrdinalIgnoreCase)))
                {
                    entry.Value.Draw(e.SpriteBatch); //draw this background
                }
            }
        }

        /// <summary>Draws each active foreground for the current player's location.</summary>
        private static void Display_RenderedWorld_DrawForegrounds(object sender, RenderedWorldEventArgs e)
        {
            foreach (var entry in ActiveBackgrounds.OrderBy(b => b.Value.Priority)) //draw in ascending priority order
            {
                //if this is a foreground and its location matches the player's (or its location is null)
                if (entry.Value.Foreground &&
                    (entry.Value.LocationName == null ||
                    string.Equals(entry.Value.LocationName, Game1.player.currentLocation?.Name, System.StringComparison.OrdinalIgnoreCase)))
                {
                    entry.Value.Draw(e.SpriteBatch); //draw this foreground
                }
            }
        }

        /// <summary>A custom background to be rendered by the <see cref="CustomBackgrounds"/> class.</summary>
        private class CustomBackground
        {
            /// <summary>The name of the in-game location where this background should appear. Null will draw the background at all locations.</summary>
            public string LocationName = null;
            /// <summary>True if this should be drawn in the foreground instead (in front of everything else on the screen).</summary>
            public bool Foreground = false;
            /// <summary>The loaded texture to use.</summary>
            public Texture2D Texture = null;
            /// <summary>The dimensions (x, y, width, height) of the texture section to use. If null, this will use the whole texture.</summary>
            public Rectangle? TextureArea = null;
            /// <summary>The pixel position of this background at the location.</summary>
            public Vector2 Position = Vector2.Zero;
            /// <summary>The per-pixel scale at which this background should be drawn. Defaults to (4,4), which matches the majority of in-game sprites.</summary>
            public Vector2 Scale = new Vector2(4, 4);
            /// <summary>The number of pixels this sprite should move each tick (~60 times per second).</summary>
            public Vector2 Movement = Vector2.Zero;
            /// <summary>The speed at which this background should move relative to the player's viewpoint.</summary>
            public Vector2 Parallax = Vector2.Zero;
            /// <summary>The color to tint this background. Defaults to white, which uses the original unmodified colors.</summary>
            public Color Color = Color.White;
            /// <summary>True if this sprite should repeat horizontally (X axis).</summary>
            public bool RepeatX = false;
            /// <summary>True if this sprite should repeat vertically (Y axis).</summary>
            public bool RepeatY = false;
            /// <summary>Backgrounds with higher priority will be drawn above others. Also known as z-axis or layer.</summary>
            public int Priority = 0;

            /// <summary>Create a custom background by loading the named texture asset.</summary>
            /// <param name="locationName">The name of the in-game location where this background should appear. Null will draw the background at all locations.</param>
            /// <param name="textureName">The name of the texture asset to load and use. Use two backspaces to separate content subfolders, e.g. "Maps\\springobjects".</param>
            public CustomBackground(string locationName, string textureName)
            {
                LocationName = locationName;
                Texture = Game1.content.Load<Texture2D>(textureName); //load the named texture asset with SDV's content manager (allowing Content Patcher edits, etc)
            }

            /// <summary>Create a custom background using a loaded texture asset.</summary>
            /// <param name="locationName">The name of the in-game location where this background should appear. Null will draw the background at all locations.</param>
            /// <param name="texture">The loaded texture to use.</param>
            public CustomBackground(string locationName, Texture2D texture)
            {
                LocationName = locationName;
                Texture = texture;
            }

            protected int previousDrawTick = 0;
            public void Draw(SpriteBatch spriteBatch)
            {
                int ticksSincePreviousDraw = Game1.ticks - previousDrawTick; //get the number of ticks that have passed since this was last drawn
                previousDrawTick = Game1.ticks; //update tick tracking
                Position += Movement * ticksSincePreviousDraw; //update movement tracking

                int width = (int)((TextureArea?.Width ?? Texture.Width) * Scale.X); //get width from dimensions if provided; default to texture size
                int height = (int)((TextureArea?.Height ?? Texture.Height) * Scale.Y); //get height from dimensions if provided; default to texture size

                Vector2 parallaxOffset = new Vector2(Game1.viewport.X * Parallax.X, Game1.viewport.Y * Parallax.Y); //amount of parallax movement to apply for the current viewport

                //get position adjusted for movement and the game's viewport
                Vector2 adjustedPosition = new Vector2(
                    Position.X + parallaxOffset.X - Game1.viewport.X,
                    Position.Y + parallaxOffset.Y - Game1.viewport.Y
                ); 

                int? repeatOffsetX = null;
                int? repeatOffsetY = null;
                if (RepeatX && adjustedPosition.X > 0) //if this background repeats horizontally & would start within/past the screen
                {
                    //get new X (NOTE: this effectively moves left in units of "width" until X <= 0)
                    int remainder = ((int)adjustedPosition.X) % width;
                    if (remainder == 0)
                        repeatOffsetX = 0;
                    else
                        repeatOffsetX = remainder - width;
                }
                if (RepeatY && adjustedPosition.Y > 0) //if this background repeats vertically & would start within/past the screen
                {
                    //get new Y (NOTE: this effectively moves up in units of "height" until Y <= 0)
                    int remainder = ((int)adjustedPosition.Y) % height;
                    if (remainder == 0)
                        repeatOffsetY = 0;
                    else
                        repeatOffsetY = remainder - height;
                }
                //if applicable, replace existing position with offsets from viewport
                adjustedPosition = new Vector2(
                    repeatOffsetX ?? adjustedPosition.X,
                    repeatOffsetY ?? adjustedPosition.Y
                );

                //Vector2 endOfViewport = new Vector2(Game1.viewport.X + Game1.viewport.Width, Game1.viewport.Y + Game1.viewport.Height); //get the bottom right corner of the viewport
                
                for (int x = (int)adjustedPosition.X; x < Game1.viewport.Width; x += width) //for each on-screen X value where this bg would be drawn
                {
                    for (int y = (int)adjustedPosition.Y; y < Game1.viewport.Height; y += height) //for each on-screen Y value where this bg would be drawn
                    {
                        Vector2 drawPosition = new Vector2(x, y);
                        spriteBatch.Draw(Texture, drawPosition, TextureArea, Color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f); //draw it

                        if (!RepeatY) //if this bg is NOT repeating vertically
                            break; //only use one Y value
                    }

                    if (!RepeatX) //if this bg is NOT repeating horizontally
                        break; //only use one X value
                }
            }
        }
    }
}
