using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewValleyExpanded
{
    /// <summary>Disables Shadow monsters' attack behaviors after a specific in-game event.</summary>
    public static class DisableShadowAttacks
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

                //enable SMAPI events
                Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
                Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
                Helper.Events.Player.Warped += Player_Warped;
                
                Enabled = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>The ID of the event that should cause Shadow monsters to stop attacking the player.</summary>
        public static int DisableShadowsEventID { get; set; } = 1090508;

        /// <summary>A list of locations where Shadow monsters should NOT have their attacks disabled.</summary>
        /// <remarks>This is mainly for use by other mods via reflection (direct C# access).</remarks>
        public static List<string> LocationBlacklist { get; set; } = new List<string>()
        {

        };

        /// <summary>Indicates whether Shadow monsters' attacks should be disabled at a given location.</summary>
        /// <returns>True if Shadows' attacks should be disabled. False otherwise.</returns>
        public static bool ShouldDisableShadowsHere(GameLocation location)
        {
            if (
                location == null //if the location doesn't exist
                || Game1.getAllFarmers().Any(farmer => farmer.eventsSeen.Contains(DisableShadowsEventID)) == false //or if NO players have seen the event that disables shadow attacks
                || LocationBlacklist.Contains(location.Name, StringComparer.OrdinalIgnoreCase) //or if this location's name is in the blacklist
                ) 
            {
                return false; //allow shadow attacks
            }

            return true; //disable shadow attacks
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        private static void Multiplayer_ModMessageReceived(object sender, StardewModdingAPI.Events.ModMessageReceivedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsMainPlayer) //if the local player is NOT hosting a game session
                return;

            if (e.Type != nameof(DisableShadowAttacks)) //if this message is NOT for this specific class
                return;

            if (Game1.getLocationFromName(e.ReadAs<string>() ?? "") is GameLocation location) //if the message contains a valid location name, get the location
            {
                DisableShadowsHere(location); //disable shadow attacks at that location if necessary
            }
        }

        private static void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            DisableShadowsHere(Game1.player.currentLocation); //disable shadow attacks at the local player's location if necessary
        }

        private static void Player_Warped(object sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer) //if the player who warped is NOT the current local player
                return;

            DisableShadowsHere(e.NewLocation); //disable shadow attacks at the new location if necessary
        }

        private static void DisableShadowsHere(GameLocation location)
        {
            if (!ShouldDisableShadowsHere(location)) //if shadows should NOT be disabled here
                return;

            if (!Context.IsMainPlayer) //if the current local player is NOT the host
            {
                try
                {
                    //tell the host player to disable shadow attacks at this location (NOTE: it's only partially effective when done by farmhands)
                    Helper.Multiplayer.SendMessage(location.Name, nameof(DisableShadowAttacks), new[] { Helper.ModRegistry.ModID }, new[] { Game1.serverHost.Value.UniqueMultiplayerID });
                }
                catch (Exception ex)
                {
                    Monitor.LogOnce($"{nameof(DisableShadowAttacks)}: Error trying to send a message to the multiplayer host. Shadows' attacks might not be disabled correctly for multiplayer farmhands. Full error message:\n{ex}", LogLevel.Warn);
                }
            }

            int shadows = 0; //count the number of shadows affected
            foreach (NPC npc in location?.characters) //check each NPC at the new location
            {
                switch (npc) //check the NPC's type
                {
                    case ShadowShaman shaman:
                        Helper.Reflection.GetField<int>(shaman, "coolDown", false)?.SetValue(int.MaxValue); //set spell cooldown to max
                        shaman.moveTowardPlayerThreshold.Value = 0; //prevent spotting the player (NOTE: seems to work even for shadows that spotted the player earlier, if they leave and return to the location)
                        shaman.DamageToFarmer = 0; //disable contact damage
                        shaman.MaxHealth = 999999; //resist most damage, including bombs
                        shaman.Health = 999999;
                        shaman.missChance.Value = 99999; //dodge all attacks that check accuracy
                        shadows++;
                        break;
                    case Shooter shooter:
                        shooter.nextShot = float.MaxValue; //set shot cooldown to max
                        shooter.moveTowardPlayerThreshold.Value = 0; //prevent spotting the player
                        shooter.DamageToFarmer = 0; //disable contact damage
                        shooter.MaxHealth = 999999; //resist most damage, including bombs
                        shooter.Health = 999999;
                        shooter.missChance.Value = 99999; //dodge all attacks that check accuracy
                        shadows++;
                        break;
                    case ShadowBrute brute:
                        brute.moveTowardPlayerThreshold.Value = 0; //prevent spotting the player
                        brute.DamageToFarmer = 0; //disable contact damage
                        brute.MaxHealth = 999999; //resist most damage, including bombs
                        brute.Health = 999999;
                        brute.missChance.Value = 99999; //dodge all attacks that check accuracy
                        shadows++;
                        break;
                }
            }
            if (shadows > 0)
                Monitor.VerboseLog($"Disabled all attacks for {shadows} Shadows at location: {location.Name}");
        }
    }
}
