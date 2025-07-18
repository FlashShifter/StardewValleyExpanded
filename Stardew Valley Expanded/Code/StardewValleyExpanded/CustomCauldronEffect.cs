using System;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using System.Linq;

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
                Helper.Events.GameLoop.SaveLoaded += SaveLoaded_CreateEffects;
                Helper.Events.Player.Warped += Warped_CreateEffects;
                Helper.Events.GameLoop.UpdateTicking += UpdateTicking_ManageEffects;
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
                Helper.Events.Player.Warped -= Warped_CreateEffects;
                Helper.Events.GameLoop.UpdateTicking -= UpdateTicking_ManageEffects;
                Helper = null;
            }
        }


        /***               ***/
        /*** Tile settings ***/
        /***               ***/


        /// <summary>The set of tiles that ALWAYS have waterfall effects. Used to generate a set of effects whenever a new save is loaded.</summary>
        /// <remarks>Duplicate tiles are supported and will multiply the number of effects on the tile.</remarks>
        private static Dictionary<string, List<Vector2>> WaterfallTiles { get; } = new Dictionary<string, List<Vector2>>()
        {
            {
                "Custom_GrandpasGrove", new List<Vector2>()
                {
                    //small waterfall
                    new Vector2(24, 23 ),
                    new Vector2(21, 25),
                    new Vector2(25, 25),
                    new Vector2(27, 27),
                    new Vector2(23, 27),
                    new Vector2(18, 28),
                    new Vector2(25, 29),
                    new Vector2(30, 28),
                    new Vector2(20, 30),
                    new Vector2(28, 30),
                    new Vector2(24, 32),
                    new Vector2(27, 33),
                    new Vector2(30, 32)
                }
            },
            {
                "Custom_FerngillRepublicFrontier_HotSpring", new List<Vector2>()
                {
                    new Vector2(7, 31),
                    new Vector2(10, 32),
                    new Vector2(9, 29),
                    new Vector2(12, 30),
                    new Vector2(15, 31),
                    new Vector2(11, 27),
                    new Vector2(8, 13),
                    new Vector2(5, 12),
                    new Vector2(7, 10),
                    new Vector2(26, 3),
                    new Vector2(36, 0),
                    new Vector2(39, 0),
                    new Vector2(54, 8),
                    new Vector2(53, 9),
                    new Vector2(42, 12),
                    new Vector2(45, 19),
                    new Vector2(47, 18),
                    new Vector2(46, 21),
                    new Vector2(48, 20),
                    new Vector2(43, 28),
                    new Vector2(44, 27),
                    new Vector2(48, 37),
                    new Vector2(50, 36),
                    new Vector2(52, 37),
                    new Vector2(54, 34),
                    new Vector2(29, 31),
                    new Vector2(28, 29),
                    new Vector2(24, 28),
                    new Vector2(27, 27),
                    new Vector2(21, 26),
                    new Vector2(23, 25),
                    new Vector2(31, 26),
                    new Vector2(29, 25),
                    new Vector2(19, 24),
                    new Vector2(22, 22),
                    new Vector2(25, 23),
                    new Vector2(28, 23),
                    new Vector2(36, 24),
                    new Vector2(32, 22),
                    new Vector2(37, 21),
                    new Vector2(27, 21),
                    new Vector2(20, 20),
                    new Vector2(30, 20),
                    new Vector2(24, 19),
                    new Vector2(28, 19),
                    new Vector2(35, 19),
                    new Vector2(31, 18),
                    new Vector2(34, 16),
                    new Vector2(32, 14),
                    new Vector2(26, 17),
                    new Vector2(21, 17),
                    new Vector2(29, 15),
                    new Vector2(28, 13),
                    new Vector2(25, 14),
                    new Vector2(23, 15),
                }
            },
            {
                "Custom_CrimsonBadlands", new List<Vector2>()
                {
                    //Sandstorm Effects
                    new Vector2(5, 0),
                    new Vector2(0, 12),
                    new Vector2(15, 16),
                    new Vector2(7, 19),
                    new Vector2(23, 16),
                    new Vector2(1, 28),
                    new Vector2(10, 35),
                    new Vector2(19, 30),
                    new Vector2(28, 22),
                    new Vector2(29, 40),
                    new Vector2(35, 12),
                    new Vector2(38, 4),
                    new Vector2(53, 9),
                    new Vector2(45, 17),
                    new Vector2(39, 26),
                    new Vector2(57, 31),
                    new Vector2(71, 14),
                    new Vector2(67, 22),
                    new Vector2(74, 26),
                    new Vector2(90, 18),
                    new Vector2(93, 33),
                    new Vector2(44, 35),
                    new Vector2(51, 46),
                    new Vector2(20, 52),
                    new Vector2(37, 54),
                    new Vector2(30, 61),
                    new Vector2(17, 66),
                    new Vector2(43, 64),
                    new Vector2(59, 67),
                    new Vector2(4, 47),
                    new Vector2(2, 63),
                    new Vector2(14, 73),
                    new Vector2(5, 79),
                    new Vector2(8, 91),
                    new Vector2(2, 103),
                    new Vector2(23, 85),
                    new Vector2(20, 94),
                    new Vector2(19, 105),
                    new Vector2(28, 98),
                    new Vector2(36, 87),
                    new Vector2(27, 73),
                    new Vector2(47, 80),
                    new Vector2(46, 96),
                    new Vector2(59, 93),
                    new Vector2(48, 109),
                    new Vector2(63, 101),
                    new Vector2(76, 87),
                    new Vector2(67, 78),
                    new Vector2(68, 54),
                    new Vector2(77, 40),
                    new Vector2(111, 24),
                    new Vector2(103, 41),
                    new Vector2(92, 49),
                    new Vector2(94, 64),
                    new Vector2(107, 56),
                    new Vector2(105, 69),
                    new Vector2(86, 79),
                    new Vector2(119, 51),
                    new Vector2(129, 41),
                    new Vector2(141, 44),
                    new Vector2(136, 62),
                    new Vector2(123, 72),
                    new Vector2(125, 86),
                    new Vector2(111, 89),
                    new Vector2(78, 65),
                    new Vector2(88, 95),
                    new Vector2(76, 111),
                    new Vector2(105, 105),
                    new Vector2(91, 119),
                    new Vector2(69, 125),
                    new Vector2(76, 132),
                    new Vector2(87, 133),
                    new Vector2(110, 130),
                    new Vector2(71, 141),
                    new Vector2(82, 146),
                    new Vector2(96, 152),
                    new Vector2(116, 138),
                    new Vector2(118, 149),
                    new Vector2(123, 117),
                    new Vector2(61, 118),
                    new Vector2(137, 153),
                    new Vector2(129, 132),
                    new Vector2(142, 129),
                    new Vector2(124, 102),
                    new Vector2(136, 97),
                    new Vector2(142, 82),
                    new Vector2(124, 31),
                    new Vector2(132, 20),
                    new Vector2(149, 30),
                    new Vector2(156, 24),
                    new Vector2(172, 18),
                    new Vector2(177, 29),
                    new Vector2(186, 23),
                    new Vector2(201, 17),
                    new Vector2(212, 22),
                    new Vector2(218, 15),
                    new Vector2(227, 14),
                    new Vector2(198, 32),
                    new Vector2(223, 35),
                    new Vector2(203, 37),
                    new Vector2(216, 45),
                    new Vector2(183, 42),
                    new Vector2(177, 49),
                    new Vector2(160, 41),
                    new Vector2(152, 51),
                    new Vector2(163, 55),
                    new Vector2(171, 61),
                    new Vector2(153, 70),
                    new Vector2(163, 79),
                    new Vector2(159, 97),
                    new Vector2(178, 82),
                    new Vector2(173, 92),
                    new Vector2(148, 104),
                    new Vector2(140, 113),
                    new Vector2(153, 114),
                    new Vector2(150, 137),
                    new Vector2(163, 148),
                    new Vector2(187, 144),
                    new Vector2(178, 136),
                    new Vector2(168, 126),
                    new Vector2(171, 107),
                    new Vector2(178, 118),
                    new Vector2(187, 112),
                    new Vector2(212, 149),
                    new Vector2(199, 139),
                    new Vector2(195, 131),
                    new Vector2(186, 124),
                    new Vector2(226, 138),
                    new Vector2(215, 130),
                    new Vector2(212, 119),
                    new Vector2(198, 117),
                    new Vector2(204, 110),
                    new Vector2(202, 100),
                    new Vector2(230, 101),
                    new Vector2(189, 90),
                    new Vector2(198, 87),
                    new Vector2(208, 83),
                    new Vector2(233, 72),
                    new Vector2(195, 76),
                    new Vector2(187, 69),
                    new Vector2(205, 68),
                    new Vector2(215, 72),
                    new Vector2(226, 60),
                    new Vector2(194, 60),
                    new Vector2(195, 50),
                    new Vector2(208, 53),
                    new Vector2(117, 15),
                    new Vector2(11, 58),
                    new Vector2(74, 1),
                    new Vector2(162, 15),
                    new Vector2(154, 122),
                    new Vector2(133, 25),
                    new Vector2(233, 114),
                    new Vector2(228, 151),
                    new Vector2(216, 92),
                    new Vector2(232, 49),
                    new Vector2(221, 4),
                    new Vector2(76, 8),
                    new Vector2(102, 7),
                    new Vector2(128, 10),
                    new Vector2(133, 0),
                    new Vector2(149, 6),
                    new Vector2(172, 0),
                    new Vector2(176, 10),
                    new Vector2(196, 6),
                    new Vector2(165, 33),
                    new Vector2(235, 23),
                    new Vector2(175, 55),
                    new Vector2(99, 140),
                    new Vector2(205, 0),
                    new Vector2(96, 0),
                    new Vector2(0, 40),
                    new Vector2(0, 84),
                    new Vector2(37, 105),
                    new Vector2(75, 155),
                    new Vector2(46, 0),

                }
            },
        };


        /// <summary>Generates extra waterfall tiles based on any necessary conditions. Checked each time the player loads a game or warps.</summary>
        /// <remarks>Duplicate tiles are supported and will multiply the number of effects on the tile.</remarks>
        /// <param name="locationName">The name of the player's current location.</param>
        private static List<Vector2> GetConditionalWaterfallTiles(string locationName)
        {
            if (locationName == "Farm")
            {
                if (Game1.whichFarm == Farm.default_layout && Helper.ModRegistry.IsLoaded("flashshifter.immersivefarm2remastered")) //if the player is using IF2R
                {
                    return new List<Vector2>()
                    {
                        new Vector2(14, 91),
                        new Vector2(11, 93),
                        new Vector2(7, 92),
                        new Vector2(16, 95),
                        new Vector2(14, 97),
                        new Vector2(9, 96),
                        new Vector2(4, 95),
                        new Vector2(6, 97),
                        new Vector2(10, 100),
                        new Vector2(14, 102),
                        new Vector2(17, 99),
                        new Vector2(4, 100),
                        new Vector2(7, 103),
                        new Vector2(12, 104),
                        new Vector2(3, 107),
                        new Vector2(1, 109)
                    };
                }
            }

            return new List<Vector2>(); //if no conditions matched, return a blank list
        }

        /// <summary>Applies conditional settings to each specific effect as it's generated. Used to apply changes based on location name, tile, etc.</summary>
        /// <param name="effect">The cauldron effect to modify.</param>
        /// <param name="locationName">The name of the effect's <see cref="GameLocation"/>. Empty ("") if unavailable.</param>
        /// <param name="tile">The tile coordinates of the effect.</param>
        /// <returns>The provided effect with any necessary changes applied.</returns>
        private static CauldronEffect ApplyConditionalEffectSettings(CauldronEffect effect, string locationName, Vector2 tile)
        {
            if (locationName == "Farm")
            {
                effect.DrawAboveAlwaysFront = false; //draw above all map layers (avoids issues with Grandpa's Farm layers, etc)
            }

            else if (locationName == "Custom_CrimsonBadlands")
            {
                effect.TextureName = "Maps\\SandstormEffect";
                effect.SourceRect = new Rectangle(0, 0, 200, 200);
                effect.MinTickRate = 35;
                effect.MaxTickRate = 70;
                effect.Flipped = false;
                effect.AlphaFade = 0.0012f;
                //effect.TintColor = new Color(240, 248, 255);
                effect.DrawAboveAlwaysFront = true;
                effect.Alpha = 0.50f;
                effect.Motion = new Vector2(3f, -0.05f);
                effect.Acceleration = new Vector2(0.1f, -0.01f);
                effect.Scale = 2f;
                effect.ScaleChange = 0.005f;
                effect.MinRotation = 1;
                effect.MaxRotation = 6;
                effect.MinRotationChange = -5;
                effect.MaxRotationChange = 6;
            }

            return effect;
        }


        /***               ***/
        /*** Internal code ***/
        /***               ***/


        /// <summary>Populates the set of effects to use when a game is loaded.</summary>
        private static void SaveLoaded_CreateEffects(object sender, SaveLoadedEventArgs e)
        {
            UpdateEffects();
        }

        /// <summary>Populates the set of effects to use when the current player's location changes.</summary>
        private static void Warped_CreateEffects(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer) //if the current player is the one who warped
                UpdateEffects();
        }

        /// <summary>Update the current player's list of effects for their location.</summary>
        private static void UpdateEffects()
        {
            if (Context.IsWorldReady == false) //if a game is NOT currently loaded
                return; //do nothing

            CauldronEffects.Value = new List<CauldronEffect>(); //clear the existing list
            Vector2 offset = new Vector2(32, 32); //define the offset within each tile (e.g. 32,32 = effects appear at the center of a tile)

            string locationName = Game1.player.currentLocation?.Name ?? ""; //get the local player's current location name (use "" if null)
            WaterfallTiles.TryGetValue(locationName, out List<Vector2> staticTiles); //get static waterfall tiles for this location, if any
            if (staticTiles == null) //if no static tiles exist
                staticTiles = new List<Vector2>(); //use a blank list

            foreach (Vector2 tile in staticTiles.Concat(GetConditionalWaterfallTiles(locationName))) //for each waterfall tile in the static list AND conditional list
            {
                Vector2 effectPosition = (tile * 64) + offset; //get the position of this tile's effect
                CauldronEffect effect = new CauldronEffect(effectPosition); //create a new effect
                effect = ApplyConditionalEffectSettings(effect, locationName, tile); //apply conditional changes
                CauldronEffects.Value.Add(effect); //add the effect to the list
            }
        }

        /// <summary>A list of cauldron effects at the player's current location. In splitscreen mode, this contains a separate list for each player.</summary>
        private static PerScreen<List<CauldronEffect>> CauldronEffects { get; set; } = new PerScreen<List<CauldronEffect>>(() => new List<CauldronEffect>());

        /// <summary>A set of information used to define repeating cauldron-style sprite effects.</summary>
        private class CauldronEffect
        {
            /// <summary>The number of the next tick when this effect should "spawn" a new sprite.</summary>
            public uint NextTick { get; set; } = uint.MinValue;
            /// <summary>The minimum random number of ticks between "spawned" sprites for this effect.</summary>

            public int MinTickRate { get; set; } = 75;
            /// <summary>The maximum random number of ticks between "spawned" sprites for this effect.</summary>
            public int MaxTickRate { get; set; } = 125;
            /// <summary>The "base" pixel position of this effect (prior to per-sprite randomization and movement).</summary>
            public Vector2 Position { get; set; } = Vector2.Zero;


            /*** Animated sprite settings ***/


            /// <summary>The next of this effect's spritesheet. Defaults to "LooseSprites\\Cursors".</summary>
            public string TextureName { get; set; } = "LooseSprites\\Cursors";
            /// <summary>The spritesheet X, Y, Width, and Height values for this effect. Defaults to the cauldron effect from "LooseSprites\\Cursors".</summary>
            public Rectangle SourceRect { get; set; } = new Rectangle(372, 1956, 10, 10);
            /// <summary>True if this effect's sprite should be flipped horizontally.</summary>
            public bool Flipped { get; set; } = false;
            /// <summary>The amount of opacity removed from this effect each tick. Defaults to 0.002f, which is -12% opacity per second.</summary>
            public float AlphaFade { get; set; } = 0.0009f;
            /// <summary>The color used to tint this effect's sprite. Defaults to a light blue "waterfall" color. Use <see cref="Color.White"/> for no tint.</summary>
            public Color TintColor { get; set; } = new Color(240, 248, 255);
            /// <summary>True if this effect should appear above all map layers. Defaults to false.</summary>
            public bool DrawAboveAlwaysFront { get; set; } = false;
            /// <summary>The starting opacity of this effect. Defaults to 0.75f, which is 75% opaque.</summary>
            public float Alpha { get; set; } = 0.45f;
            /// <summary>The number of pixels down (X) and right (Y) the effect moves each tick. Negative values are allowed.</summary>
            public Vector2 Motion { get; set; } = new Vector2(0f, -0.35f);
            /// <summary>X and Y values added to <see cref="Motion"/> each tick. Negative values are allowed.</summary>
            public Vector2 Acceleration { get; set; } = Vector2.Zero;
            /// <summary>The sprite size multiplier applied to the effect. Defaults to 3f (300% size).</summary>
            public float Scale { get; set; } = 4f;
            /// <summary>A value added to <see cref="Scale"/> each tick. Defaults to 0.01f (+60% size per second).</summary>
            public float ScaleChange { get; set; } = 0.01f;
            /// <summary>The minimum random starting rotation speed of this effect. Defaults to 0.</summary>
            public int MinRotation { get; set; } = 0;
            /// <summary>The maximum random starting rotation speed of this effect. Defaults to 0.</summary>
            public int MaxRotation { get; set; } = 0;
            /// <summary>The minimum random acceleration applied to this effect's rotation each tick. Defaults to -5 (based on cauldron effect rotation).</summary>
            public int MinRotationChange { get; set; } = -5;
            /// <summary>The maximum random acceleration applied to this effect's rotation each tick. Defaults to 6 (based on cauldron effect rotation).</summary>
            public int MaxRotationChange { get; set; } = 6;


            public CauldronEffect(Vector2 position)
            {
                Position = position;
            }
        }

        private static void UpdateTicking_ManageEffects(object sender, UpdateTickingEventArgs e)
        {
            foreach (CauldronEffect effect in CauldronEffects.Value) //for each of the current player's existing effects
            {
                if (!Context.IsWorldReady || !Game1.game1.IsActive) //if the game is currently paused or inactive
                {
                    effect.NextTick++; //increment this effect's previous tick (effectively skipping this tick)
                }
                else if (effect.NextTick <= e.Ticks) //if this effect should spawn during this tick
                {
                    effect.NextTick = e.Ticks + (uint)Game1.random.Next(effect.MinTickRate, effect.MaxTickRate + 1); //randomly assign this effect's next spawn tick

                    Game1.player.currentLocation?.temporarySprites.Add //create a sprite for this effect at the player's current location
                    (
                        new TemporaryAnimatedSprite
                        (
                            effect.TextureName,
                            effect.SourceRect,
                            effect.Position + new Vector2(Game1.random.Next(-32, 33), Game1.random.Next(-16, 17)), //randomize the new sprite's position in a limited range
                            effect.Flipped,
                            effect.AlphaFade,
                            effect.TintColor
                        )
                        {
                            alpha = effect.Alpha,
                            motion = effect.Motion,
                            acceleration = effect.Acceleration,
                            interval = 99999f, //animation framerate? (currently not applicable to this class)
                            layerDepth = 0.144f - (float)Game1.random.Next(100) / 10000f, //draw layer depth, a.k.a. Z-level (should only affect other effects, not map layers/etc)
                            scale = effect.Scale,
                            scaleChange = effect.ScaleChange,
                            rotation = Game1.random.Next(effect.MinRotation, effect.MaxRotation + 1) * (float)Math.PI / 256f, //randomize starting rotation speed (applying pi/256 to imitate SDV speeds)
                            rotationChange = Game1.random.Next(effect.MinRotationChange, effect.MaxRotationChange + 1) * (float)Math.PI / 256f, //randomize rotation acceleration (applying pi/256 to imitate SDV speeds)
                            drawAboveAlwaysFront = effect.DrawAboveAlwaysFront
                        }
                    );
                }
            }
        }
    }
}