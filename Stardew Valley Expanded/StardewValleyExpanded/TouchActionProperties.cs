using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;

namespace StardewValleyExpanded
{
    /// <summary>Implements a set of TouchAction tile properties, i.e. effects that activate when players move onto those tiles.</summary>
    /// <remarks>
    /// Additional credits:
    /// * Based on initial design & template code by Pathoschild
    /// * Adds backup implementations for certain TMXLoader features due to maintenance issues; TMXLoader is by Platonymous
    ///     ** When TMXL is unavailable, this implements TMXL's "TouchAction LoadMap" based on code from SDV 1.5.5's "TouchAction Warp"
    /// </remarks>
    public static class TouchActionProperties
    {
        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

        /// <summary>True if this fix is currently enabled.</summary>
        private static bool Enabled { get; set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Initialize and enable this class.</summary>
        /// <param name="helper">The SMAPI helper instance to use for events and other API access.</param>
        /// <param name="monitor">The monitor instance to use for log messages.</param>
        public static void Enable(IModHelper helper, IMonitor monitor)
        {
            if (!Enabled && helper != null && monitor != null) //if NOT already enabled
            {
                Helper = helper; //store helper
                Monitor = monitor; //store monitor

                //enable SMAPI event(s)
                Helper.Events.Player.Warped += OnWarped;
                Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

                Enabled = true;
            }
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>Causes specific effects based on the given tile's TouchAction property, if any.</summary>
        /// <param name="x">The horizontal tile position where the TouchAction is activating.</param>
        /// <param name="y">The vertical tile position where the TouchAction is activating.</param>
        private static void CheckTouchAction(int x, int y)
        {
            try
            {
                string action = Game1.currentLocation?.doesTileHaveProperty(x, y, "TouchAction", "Back"); //get the activated TouchAction's value
                if (string.IsNullOrWhiteSpace(action)) //if the tile doesn't have a touch action or its value is blank
                    return; //stop here
                string[] fields = action.TrimStart().Split(' '); //split the value into multiple fields between each space character

                switch (fields[0].ToLower()) //check the TouchAction's name in lowercase
                {
                    /* 
                     * NOTE: Add a case for each custom TouchAction, do whatever it should do, and then "break" to finish.
                     * 
                     * Remember to use lowercase for each "case" action name here!
                     * 
                     * If an action has additional parameters, they'll be in the fields array as text strings.
                     * For example, if a tile's TouchAction property is set to "SVE_PlaySound dog_bark loud":
                     *      field[0] is "SVE_PlaySound", field[1] is "dog_bark", field[2] is "loud"
                     */

                    case "sve_playsound":
                        if (fields.Length > 1) //if 1 parameter exists ("SVE_PlaySound <SoundName>")
                        {
                            try { Game1.soundBank.GetCue(fields[1]); } //check whether this sound exists (case-sensitive; throws an error if the sound doesn't exist)
                            catch
                            {
                                Monitor.LogOnce($"{nameof(TouchActionProperties)}: Tried to play a sound effect with an invalid name.\nSound name: \"{fields[1]}\". Tile: {x},{y}. Location: {Game1.player.currentLocation?.Name}.", LogLevel.Debug);
                                break; //stop here
                            }
                            Game1.playSound(fields[1]); //if the sound exists, play it
                        }
                        break;

                    case "loadmap": //imitation of the TMXL feature; only active while that mod is not installed
                        if (Context.CanPlayerMove && !Helper.ModRegistry.IsLoaded("Platonymous.TMXLoader")) //if the player can move AND TMXLoader is NOT available
                        {
                            if (fields.Length > 3) //if 3 or more parameters exist ("LoadMap <location name> <x> <y> [facing direction]")
                            {
                                if (Game1.getLocationFromName(fields[1]) != null) //if the named location exists
                                {
                                    if (int.TryParse(fields[2], out int destinationX) && int.TryParse(fields[3], out int destinationY)) //if successfully parsed the X,Y fields
                                    {
                                        Game1.warpFarmer(fields[1], destinationX, destinationY, flip: false);
                                    }
                                    else
                                    {
                                        Monitor.LogOnce($"{nameof(TouchActionProperties)}: Tried to LoadMap but couldn't parse the X or Y values.\nX: \"{fields[2]}\". Y: \"{fields[3]}\". Tile: {x},{y}. Location: {Game1.player.currentLocation?.Name}.", LogLevel.Debug);
                                    }
                                }
                                else
                                {
                                    Monitor.LogOnce($"{nameof(TouchActionProperties)}: Tried to LoadMap but failed to find the location \"{fields[1]}\".\nTile: {x},{y}. Location: {Game1.player.currentLocation?.Name}.", LogLevel.Debug);
                                }
                            }
                            else
                            {
                                Monitor.LogOnce($"{nameof(TouchActionProperties)}: Tried to LoadMap but it doesn't have enough parameters.\nTile: {x},{y}. Location: {Game1.player.currentLocation?.Name}.\nKnown format: LoadMap <location name> <x> <y> [facing direction]\nCurrent value: {action}", LogLevel.Debug);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) //if an unexpected error occurs
            {
                Monitor.LogOnce($"{nameof(TouchActionProperties)}: Error while running {nameof(CheckTouchAction)}. A custom TouchAction tile property failed to work at {Game1.player.currentLocation?.Name} (tile {x},{y}). Full error message: \n{ex.ToString()}", LogLevel.Error);
                return;
            }
        }

        /// <summary>The tile most recently touched by each local player.</summary>
        private static readonly PerScreen<Vector2> LastPlayerTile = new(() => new Vector2(-1));

        private static void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer) //if the local player just warped
                LastPlayerTile.Value = new Vector2(-1); //reset their most recently touched tile
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Vector2 tile = Game1.player.Tile; //get the current local player's tile
            if (tile != LastPlayerTile.Value) //if their tile has changed since the last check
            {
                LastPlayerTile.Value = tile; //update their most recent tile
                CheckTouchAction((int)tile.X, (int)tile.Y); //perform the new tile's touch action (if any)
            }
        }
    }
}
