using System;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

namespace StardewValleyExpanded
{
    /// <summary>Creates and manages a predefined set of sprite effects based on the Wizard's cauldron.</summary>
    public static class CustomCauldronEffects
    {
        /// <summary>True if these cauldron effects are currently enabled.</summary>
        public static bool IsEnabled { get; private set; } = false;
        /// <summary>The SMAPI helper provided for use with these effects.</summary>
        private static IModHelper Helper { get; set; } = null;


        /***                        ***/
        /*** Enable/disable methods ***/
        /***                        ***/


        /// <summary>Enables this class's predefined set of effects.</summary>
        /// <param name="helper">The SMAPI helper to use for events and necessary checks.</param>
        public static void Enable(IModHelper helper)
        {
            if (!IsEnabled && helper != null) //if disabled AND a helper was provided
            {
                //enable cauldron SMAPI events
                IsEnabled = true;
                Helper = helper;
                helper.Events.GameLoop.SaveLoaded += SaveLoaded_CreateEffects;
                helper.Events.GameLoop.UpdateTicking += UpdateTicking_ManageEffects;
            }
        }

        /// <summary>Disables this class's predefined set of effects.</summary>
        public static void Disable()
        {
            if (IsEnabled) //if enabled
            {
                //disable cauldron SMAPI events
                IsEnabled = false;
                Helper.Events.GameLoop.SaveLoaded -= SaveLoaded_CreateEffects;
                Helper.Events.GameLoop.UpdateTicking -= UpdateTicking_ManageEffects;
                Helper = null;
            }
        }


        /***               ***/
        /*** Tile settings ***/
        /***               ***/


        /// <summary>The set of tiles with waterfall effects when SVE's "Grandpa's Farm" is installed.</summary>
        /// <remarks>Duplicate tiles are supported and will multiply the number of effects.</remarks>
        private static Dictionary<string, List<Vector2>> GrandpasFarm_WaterfallTiles { get; } = new Dictionary<string, List<Vector2>>()
        {
            {
                "Farm", new List<Vector2>() //add tiles for the farm here
                {
                    //grandpa's big waterfall
					new Vector2(31, 28),
					new Vector2(31, 28),
					new Vector2(32, 28),
					new Vector2(32, 28),
					new Vector2(33, 28),
					new Vector2(33, 28),

					//small waterfall 1
					new Vector2(18, 7),
					new Vector2(18, 7),
					new Vector2(19, 7),
					new Vector2(19, 7),

					//small waterfall 2
					new Vector2(31, 9),
					new Vector2(31, 9),
					new Vector2(32, 9),
					new Vector2(32, 9)
                }
            },
            {
                "GrandpasGrove", new List<Vector2>() //add tiles for Grandpa's Grove here
                {
                    //small waterfall
                    new Vector2(21, 12),
                    new Vector2(21, 12),
                    new Vector2(22, 12),
                    new Vector2(22, 12),
                }
            }
        };


        /***               ***/
        /*** Internal code ***/
        /***               ***/


        /// <summary>Populates the set of effects to use when a game is loaded.</summary>
        private static void SaveLoaded_CreateEffects(object sender, SaveLoadedEventArgs e)
        {
            CauldronEffects = new List<CauldronEffect>(); //clear the existing list
            Vector2 offset = new Vector2(32, 32); //define the offset within each tile (e.g. 32,32 = effects appear at the center of a tile)

            if (Game1.whichFarm == Farm.default_layout && Helper.ModRegistry.IsLoaded("flashshifter.GrandpasFarm")) //if this save is using Grandpa's Farm
            {
                Color waterfallColor = new Color(240, 248, 255); //define the waterfall effects' color

                foreach (var entry in GrandpasFarm_WaterfallTiles) //for each entry in the waterfall tile dictionary
                {
                    foreach (Vector2 tile in entry.Value) //for each tile in this location's list
                    {
                        Vector2 effectPosition = (tile * 64) + offset; //define the position of this tile's effects

                        CauldronEffects.Add //create a new effect
                        (
                            new CauldronEffect
                            (
                                entry.Key, //use this list's location name
                                effectPosition,
                                waterfallColor,
                                Game1.random.Next(75, 126) //random tick rate
                            )
                        );
                    }
                }
            }
        }

        private static List<CauldronEffect> CauldronEffects { get; set; } = new List<CauldronEffect>();

        /// <summary>A set of information used to define repeating cauldron-style sprite effects.</summary>
        private class CauldronEffect
        {
            /// <summary>The tick value last time this effect "spawned" a new sprite.</summary>
            public uint PreviousTick { get; set; } = uint.MinValue;
            /// <summary>The number of ticks between "spawned" sprites for this effect.</summary>
            public int TickRate { get; set; }
            /// <summary>The name of the <see cref="GameLocation"/> where this effect appears.</summary>
            public string LocationName { get; set; }
            /// <summary>The "base" position of this effect (prior to randomization or movement over time).</summary>
            public Vector2 Position { get; set; }
            /// <summary>The color used to tint this effect's sprites. Default value is <see cref="Color.White"/> (no tint).</summary>
            public Color TintColor { get; set; }

            public CauldronEffect(string locationName, Vector2 position, Color? tintColor = null, int tickRate = 100)
            {
                LocationName = locationName;
                Position = position;
                TintColor = tintColor ?? Color.White; //default to white (no tint) if no color is provided
                TickRate = tickRate;
            }
        }

        private static void UpdateTicking_ManageEffects(object sender, UpdateTickingEventArgs e)
        {
            GameLocation location = Game1.player.currentLocation; //get the local player's current location
            string locationName = Game1.player.currentLocation?.NameOrUniqueName; //get the current location's name (note: faster than repeatedly checking the net-synched values)

            foreach (CauldronEffect effect in CauldronEffects) //for each existing effect
            {
                if (!Context.IsWorldReady || !Game1.game1.IsActive) //if the game is currently paused or inactive
                {
                    effect.PreviousTick++; //increment this effect's previous tick (effectively skipping this tick)
                }
                else if (effect.PreviousTick + effect.TickRate <= e.Ticks) //if this effect should spawn during this tick
                {
                    effect.PreviousTick = e.Ticks; //set this tick as the effect's "previous" tick

                    if (effect.LocationName == locationName) //if this effect is for the player's current location
                    {
                        float randomSpin = Game1.random.Next(-5, 6); //get a random left/right spin value (shared by motion.X and rotationChange)

                        location.temporarySprites.Add //create a sprite for this effect
                        (
                            new TemporaryAnimatedSprite
                            (
                                "LooseSprites\\Cursors", //tilesheet
                                new Rectangle(372, 1956, 10, 10), //x, y, width, height on tilesheet
                                effect.Position + new Vector2(Game1.random.Next(-32, 33), Game1.random.Next(-16, 17)), //in-game position
                                false, //true to flip sprite horizontally
                                0.002f, //transparency added each tick
                                effect.TintColor //sprite tint color (Color.White = no tint)
                            )
                            {
                                alpha = 0.75f, //starting transparency (0 = invisible, 1 = opaque)
                                motion = new Vector2(0f, -0.5f), //X, Y pixel movement each tick
                                acceleration = new Vector2(0f, 0f), //X, Y added to "motion" each tick
                                interval = 99999f, //animation framerate? (currently not applicable to this class)
                                layerDepth = 0.144f - (float)Game1.random.Next(100) / 10000f, //draw layer, a.k.a. Z-level
                                scale = 3f, //sprite size multiplier (1 = original size)
                                scaleChange = 0.01f, //value added to "scale" each tick
                                rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f, //value added to "rotation" per tick (in radians?)
                                drawAboveAlwaysFront = true //true to draw this effect in front of anything else (if possible)
                            }
                        );
                    }
                }
            }
        }
    }
}
