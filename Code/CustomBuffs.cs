using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;

namespace StardewValleyExpanded
{
    /// <summary>Applies customized buffs to the local player under certain conditions.</summary>
    public static class CustomBuffs
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

                Helper.Events.GameLoop.OneSecondUpdateTicking += GameLoop_OneSecondUpdateTicking;

                Enabled = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>How long (in seconds) the player needs to swim before the buff starts applying.</summary>
        private const int secondsBeforeBuffIsApplied = 3;
        /// <summary>The buff's duration (in milliseconds).</summary>
        /// <remarks>
        /// One in-game hour is 43,000ms by default. One full day is 860,000ms.
        /// One real minute is 60,000ms. Food buffs usually have a duration of several minutes.
        /// </remarks>
        private const int millisecondsBuffDuration = 720000;

        /// <summary>How long the current player has been swimming at locations that give buffs.</summary>
        private static PerScreen<int> secondsSpentSwimming = new PerScreen<int>(() => 0); //set each player's value to 0

        /// <summary>Manage the buffs applied when swimming at certain locations.</summary>
        private static void GameLoop_OneSecondUpdateTicking(object sender, OneSecondUpdateTickingEventArgs e)
        {
            if (!Context.IsPlayerFree || !Game1.game1.IsActive) //if the player is occupied or the game is inactive
                return; //do nothing

            if (Game1.player.swimming.Value && (Game1.currentLocation?.NameOrUniqueName == ("Custom_GrandpasGrove") || Game1.currentLocation?.NameOrUniqueName == "Custom_SpriteSpring2")) //if the player is currently swimming at Grandpa's Grove
            {
                secondsSpentSwimming.Value++; //increment swim timer
            }
            else //if the player is NOT swimming there
            {
                secondsSpentSwimming.Value = 0; //reset swim timer
                return; //skip the rest of this method
            }

            if (secondsSpentSwimming.Value >= secondsBeforeBuffIsApplied) //if the buff should be applied
            {
                Monitor.VerboseLog($"{nameof(CustomBuffs)}: Local player has been swimming at {Game1.currentLocation?.NameOrUniqueName ?? "[null location?]"} for at least {secondsBeforeBuffIsApplied} seconds. Applying (or updating) the local buff.");
                ApplyGrandpasGroveBuff();
                secondsSpentSwimming.Value = 0; //reset the swim timer
            }
        }

        /// <summary>Applies the currently available Grandpa's Grove buff to the local player.</summary>
        private static void ApplyGrandpasGroveBuff()
        {
            Random seededRandom = new Random((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2 + 1); //create RNG with a seed based on save ID and days played

            Buff buff = null;
            switch (seededRandom.Next(5)) //based on a random number
            {
                case 0:
                    buff = new Buff(0); //farming buff
                    buff.buffAttributes[0] = 2; //+2 farming
                    break;
                case 1:
                    buff = new Buff(1); //fishing buff
                    buff.buffAttributes[1] = 2; //+2 fishing
                    break;
                case 2:
                    buff = new Buff(2); //mining buff
                    buff.buffAttributes[2] = 2; //+2 mining
                    break;
                case 3:
                    buff = new Buff(5); //foraging buff
                    buff.buffAttributes[5] = 2; //+2 foraging
                    break;
                case 4:
                    buff = new Buff(11); //attack buff
                    buff.buffAttributes[11] = 2; //+2 attack
                    break;
            }

            if (buff != null) //if a buff was selected
            {
                buff.millisecondsDuration = buff.totalMillisecondsDuration = millisecondsBuffDuration; //set the buff's current and total durations
                Game1.buffsDisplay.addOtherBuff(buff); //apply it to the local player
            } 
        }
    }
}
