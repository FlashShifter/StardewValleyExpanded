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


namespace SpiritsEveChestEditor

{
    public static class SpiritsEveChestEditor
    {
        /***                   ***/
        /*** Customizable code ***/
        /***                   ***/

        /// <summary>Generates a list of items to be contained in the Spirit's Eve festival chest.</summary>
        /// <remarks>
        /// Examples for creating items:
        ///     new StardewValley.Object(Vector2.Zero, 373, 999); //creates object ID 373 with stack size 999 (999 golden pumpkins in a single stack)
        ///     new StardewValley.Object(276, 1, false, -1, 4); //creates object ID 276 (a "real" pumpkin) with iridium quality; quality options: 0 = normal, 1 = bronze, 2 = silver, 4 = iridium
        ///     Utility.fuzzyItemSearch("Tempered Galaxy Sword", 1); //creates almost any item with the provided name (if unique), and stack size 1 (if applicable)
        /// </remarks>
        /// 

        private static List<Item> GetChestContents()
        {
            List<Item> itemList = new List<Item>(); //create a blank list

            if (Game1.year % 4 == 1) //if this is an odd-numbered year
            {
                itemList.Add(new StardewValley.Object("373", 1)); //add a golden pumpkin
               //itemList.Add(new StardewValley.Object(276, 1, false, -1, 4)); //add 1 iridium-quality pumpkin
               //itemList.Add(Utility.fuzzyItemSearch("Galaxy Sword", 1)); //add a modded weapon
            }
            else if ( Game1.year % 4 == 3)
            {
                itemList.Add(new StardewValley.Object("373", 3)); //add a stack of 3 golden pumpkins
            }

            return itemList; //return the completed list
        }

        /// <summary>Generates the new tile position for the Spirit's Eve festival chest.</summary>
        private static Vector2 GetChestTile()
        {
            Vector2 newTile = new Vector2(); //the chest's new tile position

            if (Game1.year % 4 == 1) //if this is an odd-numbered year
            {
                newTile = new Vector2(63, 16);
            }
            else if (Game1.year % 4 == 3)
            {
                newTile = new Vector2(71, 3);
            }

            return newTile; //return the chosen tile
        }

        /***                   ***/
        /*** Activation method ***/
        /***                   ***/

        /// <summary>Enables this class's functionality. This should be called once during mod entry.</summary>
        /// <param name="helper">This mod's SMAPI helper, used to add events.</param>
        public static void Enable(IModHelper helper)
        {
            if (enabled) //if this class is already enabled
                return; //do nothing

            //add SMAPI events
            helper.Events.Player.Warped += Player_Warped_SpiritsEve;
            helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked_EditSpiritsEveChest;
        }

        /***                   ***/
        /*** "Background" code ***/
        /***                   ***/

        /// <summary>Whether this class is currently enabled.</summary>
        private static bool enabled = false;
        /// <summary>While true, this class will attempt to find and customize the Spirit's Eve festival's treasure chest.</summary>
        private static bool editSpiritsEveChest = false;

        /// <summary>Determines whether the player's most recent warp destination is the Spirit's Eve festival location. If so, chest editing behavior is enabled.</summary>
        private static void Player_Warped_SpiritsEve(object sender, WarpedEventArgs e)
        {

            if (Game1.dayOfMonth == 27 && Game1.currentSeason == "fall" && (e.NewLocation.currentEvent?.isFestival ?? false)) //if this player just warped to the Spirit's Eve festival
            {
                if ( Game1.year % 2 == 1 )
                    editSpiritsEveChest = true; //enable the chest editing event
            }
            else //if the player warped anywhere else
            {
                editSpiritsEveChest = false; //disable the chest editing event
            }
        }

        /// <summary>Detects the Spirit's Eve festival treasure chest and edits the chest's position and/or contents.</summary>
        private static void GameLoop_OneSecondUpdateTicked_EditSpiritsEveChest(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!editSpiritsEveChest) //if the Spirit's Eve chest should NOT currently be edited
                return; //do nothing

            //look for a chest containing a golden pumpkin (null if not found)
            Chest chest = Game1.currentLocation.Objects.Values.FirstOrDefault(added => added is Chest c && c.Items.Any(item => item.QualifiedItemId == "(O)373" ) == true) as Chest;

            if (chest == null) //if the chest was NOT found
                return; //do nothing (but keep checking)

            List<Item> newItems = GetChestContents(); //get the chest's new contents
            Vector2 newTile = GetChestTile(); //get the chest's new tile position

            Vector2 oldTile = chest.TileLocation; //get the chest's original tile
            Game1.currentLocation.moveContents((int)oldTile.X, (int)oldTile.Y, (int)newTile.X, (int)newTile.Y, null); //move the chest to its new tile
            chest.Items.Clear(); //clear the old list of items
            chest.Items.AddRange(newItems); //add the new list of items

            editSpiritsEveChest = false; //disable this event
        }
    }
}
