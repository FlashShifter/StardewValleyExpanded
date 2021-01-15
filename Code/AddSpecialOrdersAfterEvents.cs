using System;
using StardewModdingAPI;
using StardewValley;
using Harmony;
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
    /// <summary>Adds specific <see cref="SpecialOrder"/>s to all players' quest logs after viewing specific events.</summary>
    public static class AddSpecialOrdersAfterEvents
    {
        /// <summary>True if this class's features are currently enabled.</summary>
        public static bool Enabled { get; private set; } = false;

        /// <summary>Enables this class's features by setting up SMAPI events.</summary>
        /// <param name="helper">A SMAPI helper instance, used to set up events.</param>
        public static void Enable(IModHelper helper)
        {
            if (!Enabled) //if NOT enabled
            {
                helper.Events.GameLoop.UpdateTicked += CurrentEventEnded_UpdateSpecialOrders;
                helper.Events.GameLoop.DayStarted += DayStarted_UpdateSpecialOrders;
                Enabled = true;
            }
        }

        /// <summary>A set of event IDs with lists of special order IDs. After any player sees the event, the orders will be added to their journals until completed.</summary>
        private static Dictionary<int, List<string>> EventsAndSpecialOrders = new Dictionary<int, List<string>>()
        {
            {
                0505050, new List<string>()
                {
                    "Sophia"
                }
            }
        };



        /*****                     *****/
        /***** Internal code below *****/
        /*****                     *****/



        private static int? lastEventID = null;

        private static void CurrentEventEnded_UpdateSpecialOrders(object sender, UpdateTickedEventArgs e)
        {
            int? currentEventID = Game1.CurrentEvent?.id; //get the current quest ID (if any)
            
            if (currentEventID == null && lastEventID.HasValue) //if an event just ended
            {
                UpdateSpecialOrders();
            }

            lastEventID = currentEventID; //update recorded event ID
        }

        private static void DayStarted_UpdateSpecialOrders(object sender, DayStartedEventArgs e)
        {
            UpdateSpecialOrders();
        }

        private static void UpdateSpecialOrders()
        {
            foreach (var entry in EventsAndSpecialOrders) //for each entry in the event dictionary
            {
                foreach (Farmer farmer in Game1.getAllFarmers()) //for each existing player
                {
                    if (farmer.eventsSeen.Contains(entry.Key)) //if a player has seen this entry's event ID
                    {
                        foreach (string orderName in entry.Value) //for each special order that should be added
                        {
                            if (!Game1.player.team.SpecialOrderActive(orderName) && //if the players do NOT currently have this order
                                !Game1.player.team.completedSpecialOrders.ContainsKey(orderName)) //AND if the players have NOT completed this order before
                            {
                                SpecialOrder order = SpecialOrder.GetSpecialOrder(orderName, null); //create this order
                                Game1.player.team.specialOrders.Add(order); //add it to the players' log
                            }
                        }

                        break; //skip the rest of the player checks for this entry
                    }
                }
            }
        }
    }
}
