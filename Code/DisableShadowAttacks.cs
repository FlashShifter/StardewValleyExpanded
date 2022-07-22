using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace StardewValleyExpanded
{
    /// <summary>Disables Shadow monsters' attack behaviors after a specific in-game event.</summary>
    public static class DisableShadowAttacks
    {
        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

        /// <summary>True if this patch is currently applied.</summary>
        public static bool Applied { get; private set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// <param name="helper">The SMAPI helper instance to use for events and other API access.</param>
        /// <param name="monitor">The monitor instance to use for log messages.</param>
        public static void ApplyPatch(Harmony harmony, IModHelper helper, IMonitor monitor)
        {
            if (!Applied && helper != null && monitor != null) //if NOT already applied
            {
                Helper = helper; //store helper
                Monitor = monitor; //store monitor

                //enable SMAPI events
                Helper.Events.Multiplayer.ModMessageReceived += Multiplayer_ModMessageReceived;
                Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
                Helper.Events.Player.Warped += Player_Warped;
                Helper.Events.GameLoop.TimeChanged += GameLoop_TimeChanged;

                //apply patch
                Monitor.Log($"Applying Harmony patch \"{nameof(DisableShadowAttacks)}\": postfixing SDV method \"AdventureGuild.killListLine(string, int, int)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(AdventureGuild), "killListLine", new[] { typeof(string), typeof(int), typeof(int) }),
                    postfix: new HarmonyMethod(typeof(DisableShadowAttacks), nameof(AdventureGuild_killListLine))
                );

                Applied = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>The ID of the event that should cause Shadow monsters to stop attacking the player.</summary>
        public static int DisableShadowsEventID { get; set; } = 1090508;

        /// <summary>A list of locations where Shadow monsters should have their attacks disabled.</summary>
        /// <remarks>
        /// "Mineshaft" locations will be whitelisted by default.
        /// Other locations that should disable shadows' attacks must be added here.
        /// If a location is added to both lists (e.g. by another C# mod), the blacklist will take priority (shadow attacks will be enabled).
        /// </remarks>
        public static List<string> LocationWhitelist { get; set; } = new List<string>()
        { 
            "Custom_HighlandsCavern"
        };

        /// <summary>A list of locations where Shadow monsters should NOT have their attacks disabled.</summary>
        /// <remarks>This is mainly for use by other mods via reflection (direct C# access).</remarks>
        public static List<string> LocationBlacklist { get; set; } = new List<string>()
        {

        };

        /// <summary>Indicates whether Shadow monsters' attacks should be disabled at a given location.</summary>
        /// <returns>True if Shadows' attacks should be disabled. False otherwise.</returns>
        public static bool ShouldDisableShadowsHere(GameLocation location)
        {
            if (location == null //if the location doesn't exist
                || Game1.getAllFarmers().Any(farmer => farmer.eventsSeen.Contains(DisableShadowsEventID)) == false //or if NO players have seen the event that disables shadow attacks
                || LocationBlacklist.Contains(location.Name, StringComparer.OrdinalIgnoreCase)) //or if this location's name is in the blacklist
            {
                return false; //allow shadow attacks
            }

            if (location.Name.StartsWith("UndergroundMine", StringComparison.Ordinal) //if this seems to be a normal mine level
                || LocationWhitelist.Contains(location.Name, StringComparer.OrdinalIgnoreCase)) //or this location's name is in the whitelist
            {
                return true; //disable shadow attacks
            }

            //if the "disable shadows" event has been seen, but this location does not match any known rules
            return false; //default: allow shadow attacks
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>Blanks any lines that refer to the "void spirits" (shadow) slayer quest, effectively hiding it from the Adventurer's Guild billboard.</summary>
        /// <param name="monsterType">The internal name for the monster type targeted by a slayer quest.</param>
        /// <param name="__result">The result of the original method: localized text describing the player's progress on a slayer quest.</param>
        private static void AdventureGuild_killListLine(string monsterType, ref string __result)
        {
            try
            {
                if (monsterType == "VoidSpirits" //if this method is currently describing the shadow slayer quest (see "AdventureGuild.showMonsterKillList()")
                    && Game1.getAllFarmers().Any(farmer => farmer.eventsSeen.Contains(DisableShadowsEventID))) //and ANY player has seen the "disable shadows" event
                {
                    __result = String.Empty; //return a blank string instead
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(DisableShadowAttacks)}.{nameof(AdventureGuild_killListLine)}\" has encountered an error. This only affects a single line of text, but should be reported to Stardew Valley Expanded's developers if possible. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }

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

        private static void GameLoop_TimeChanged(object sender, StardewModdingAPI.Events.TimeChangedEventArgs e)
        {
            DisableShadowsHere(Game1.player.currentLocation);
        }

        private static void DisableShadowsHere(GameLocation location)
        {
            FixMonsterSlayerQuest(); //if necessary, auto-complete and hide the local player's shadow slayer quest

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

            if (location is MineShaft mineLevel)
                FixProgressOnInfestedMineLevels(mineLevel);
        }

        /// <summary>Spawns a ladder when all non-Shadow enemies have been defeated in an infested mine level (or others that require combat to progress).</summary>
        private static void FixProgressOnInfestedMineLevels(MineShaft mineLevel)
        {
            try
            {
                //imitate MineShaft.EnemyCount but exclude shadows
                int enemyCountWithoutShadows = mineLevel.characters.OfType<Monster>().Where(m => m is not ShadowShaman and not Shooter and not ShadowBrute).Count();

                if (mineLevel.mustKillAllMonstersToAdvance() && enemyCountWithoutShadows <= 0)
                {
                    Vector2 p = Helper.Reflection.GetProperty<Vector2>(mineLevel, "tileBeneathLadder", true).GetValue();
                    if (mineLevel.getTileIndexAt(Utility.Vector2ToPoint(p), "Buildings") == -1)
                    {
                        mineLevel.createLadderAt(p, "newArtifact");
                        if (Game1.player.currentLocation == mineLevel)
                        {
                            Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:MineShaft.cs.9484"));
                        }

                        if (Monitor.IsVerbose)
                            Monitor.Log($"All non-shadow enemies defeated. Spawned ladder and displayed message for players at location: {mineLevel.Name}", LogLevel.Trace);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"{typeof(DisableShadowAttacks)}: An error occurred while trying to spawn a ladder in a mine level. Full error message: \n{ex}", LogLevel.Error);
            }
        }

        /// <summary>Silently auto-completes the "void spirits" monster slayer quest for the local player if necessary.</summary>
        private static void FixMonsterSlayerQuest()
        {
            if (Game1.player.mailReceived.Contains("Gil_Savage Ring") || Game1.player.hasCompletedAllMonsterSlayerQuests.Value) //if the player has already retrieved the "void spirits" slayer reward OR has already completed all slayer quests
                return; //do nothing

            if (Game1.getAllFarmers().Any(farmer => farmer.eventsSeen.Contains(DisableShadowsEventID)) == false) //if NO players have seen the "disable shadows" event
                return; //do nothing

            bool increasedShadowGuyKills = true;
            if (Game1.stats.specificMonstersKilled.ContainsKey("Shadow Guy")) //if the shadow guy kill entry already exists
            {
                if (Game1.stats.specificMonstersKilled["Shadow Guy"] < 100000) //if it's less than 100k
                    Game1.stats.specificMonstersKilled["Shadow Guy"] += 100000; //increase it (100k + the actual number)
                else //if the entry is already 100k+
                    increasedShadowGuyKills = false;
            }
            else //if the shadow guy kill entry does NOT exist yet
            {
                Game1.stats.specificMonstersKilled.Add("Shadow Guy", 100000); //add it and set it to 100k
            }

            if (increasedShadowGuyKills) //if the player's kill count was updated, perform update tasks (based on the method "Stats.monsterKilled")
            {
                Game1.player.hasCompletedAllMonsterSlayerQuests.Value = AdventureGuild.areAllMonsterSlayerQuestsComplete(); //update the player's overall "slayer" progress
                if (AdventureGuild.areAllMonsterSlayerQuestsComplete()) //if all slayer quests are complete now
                {
                    Game1.getSteamAchievement("Achievement_KeeperOfTheMysticRings"); //activate the related achievement
                }
            }
        }
    }
}