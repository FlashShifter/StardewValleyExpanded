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

namespace StardewValleyExpanded
{
    /// <summary>Applies customized buffs to the local player under certain conditions.</summary>
    public static class CustomBuffs
    {
        /// <summary>True if this class's features are currently enabled.</summary>
        public static bool Enabled { get; private set; } = false;

        /// <summary>Enables this class's features by setting up SMAPI events.</summary>
        /// <param name="helper">A SMAPI helper instance, used to set up events.</param>
        public static void Enable(IModHelper helper)
        {
            if (!Enabled) //if NOT enabled
            {
                helper.Events.GameLoop.OneSecondUpdateTicking += OneSecondUpdateTicking_GrandpasGroveBuff;
                Enabled = true;
            }
        }

        /// <summary>How long (in seconds) the player needs to swim before the buff starts applying.</summary>
        private const int secondsBeforeBuffIsApplied = 3;
        /// <summary>The buff's duration (in milliseconds).</summary>
        /// <remarks>
        /// One in-game hour is 43,000ms by default. One full day is 860,000ms.
        /// One real minute is 60,000ms. Food buffs usually have a duration of several minutes.
        /// </remarks>
        private const int millisecondsBuffDuration = 720000;

        private static int secondsSpentSwimming = 0;

        /// <summary>Manages the buffs applied when swimming at the Grandpa's Grove location.</summary>
        private static void OneSecondUpdateTicking_GrandpasGroveBuff(object sender, OneSecondUpdateTickingEventArgs e)
        {
            if (!Context.IsPlayerFree || !Game1.game1.IsActive) //if the player is occupied or the game is inactive
                return; //do nothing

            if (Game1.player.swimming.Value && Game1.currentLocation?.NameOrUniqueName == ("Custom_GrandpasGrove") || Game1.currentLocation?.NameOrUniqueName == "Custom_SpriteSpring2") //if the player is currently swimming at Grandpa's Grove
            {
                secondsSpentSwimming++; //increment swim timer
            }
            else //if the player is NOT swimming there
            {
                secondsSpentSwimming = 0; //reset swim timer
                return; //skip the rest of this method
            }

            if (secondsSpentSwimming >= secondsBeforeBuffIsApplied) //if the buff should be applied
            {
                ApplyGrandpasGroveBuff();
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
