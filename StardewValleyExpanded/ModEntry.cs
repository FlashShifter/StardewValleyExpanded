using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile.ObjectModel;

namespace StardewValleyExpanded
{
    /* Implementing the Mod interface gives us the Entry() method and allows SMAPI to load the mod
     * The IAssetLoader and IAssetEditor interfaces give us their respective method for editing and
     * loading in order to override and update the npcs.
     */
    public class ModEntry : Mod, IAssetLoader, IAssetEditor
    {
        //The overridden npcs
        //Having these global variables lets us access them throughout the entire class.
        private SocialNPC Marlon = null;
        private SocialNPC Morris = null;

        private bool firstTick = true;

        //The mod entry point, which is called when the mod is loaded
        //The IModHelper parameter is what provides the simplified api for us to use
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveCreated += this.NpcFixes;
            helper.Events.GameLoop.SaveLoaded += this.NpcFixes;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.ReturnedToTitle += this.Clean;

            helper.ConsoleCommands.Add("listnpcs", "Lists the NPCs currently in memory.\n\nUsage: listnpcs [name]\n - name : Optional value. The name of the npc.", this.ListNpcs);
        }

        /// <summary>
        /// Checks whether an asset can be loaded.
        /// </summary>
        /// <typeparam name="T">The generic asset type.</typeparam>
        /// <param name="asset">The asset data to check against.</param>
        /// <returns>True if the asset can be loaded. False otherwise.</returns>
        public bool CanLoad<T>(IAssetInfo asset)
        {
            /*
             * Order for assets is:
             * Sprites
             * Portraits
             * Dialogue
             * Schedule
             */
            var marlon = asset.AssetNameEquals("Characters/Marlon")
                || asset.AssetNameEquals("Portraits/Marlon")
                || asset.AssetNameEquals("Characters/Dialogue/Marlon")
                || asset.AssetNameEquals("Characters/Schedules/Marlon");

            //Checking if player has not completed the CC before checking for Morris' assets
            if (Game1.MasterPlayer != null && !Game1.MasterPlayer.hasCompletedCommunityCenter())
            {
                var morris = asset.AssetNameEquals("Characters/Morris")
                    || asset.AssetNameEquals("Portraits/Morris")
                    || asset.AssetNameEquals("Characters/Dialogue/Morris")
                    || asset.AssetNameEquals("Characters/Schedules/Morris");

                return marlon || morris;
            }

            return marlon;
        }

        /// <summary>
        /// Loads new assets when the asset name is found to be a match.
        /// </summary>
        /// <typeparam name="T">The generic asset type.</typeparam>
        /// <param name="asset">The asset data to be loaded.</param>
        /// <returns>The asset type, loaded into memory.</returns>
        public T Load<T>(IAssetInfo asset)
        {
            //Sprites
            if (asset.AssetNameEquals("Characters/Marlon"))
                return this.Helper.Content.Load<T>("[SVE] Marlon/assets/Image/Sprite.png");
            if (asset.AssetNameEquals("Characters/Morris"))
                return this.Helper.Content.Load<T>("[SVE] Morris/assets/Image/Sprite.png");

            //Portraits
            if (asset.AssetNameEquals("Portraits/Marlon"))
                return this.Helper.Content.Load<T>("[SVE] Marlon/assets/Image/Marlon.png");
            if (asset.AssetNameEquals("Portraits/Morris"))
                return this.Helper.Content.Load<T>("[SVE] Morris/assets/Image/Morris.png");

            //Dialogue
            if (asset.AssetNameEquals("Characters/Dialogue/Marlon"))
                return this.Helper.Content.Load<T>("[SVE] Marlon/assets/Dialogue/Dialogue.json");
            if (asset.AssetNameEquals("Characters/Dialogue/Morris"))
                return this.Helper.Content.Load<T>("[SVE] Morris/assets/Dialogue/Dialogue.json");

            //Schedules
            if (asset.AssetNameEquals("Characters/Schedules/Marlon"))
                return this.Helper.Content.Load<T>("[SVE] Marlon/assets/Schedule/Schedule.json");
            if (asset.AssetNameEquals("Characters/Schedules/Morris"))
                return this.Helper.Content.Load<T>("[SVE] Morris/assets/Schedule/Schedule.json");

            //throw a new error in the smapi console if we can't find the right matching asset
            throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'");
        }

        /// <summary>
        /// Checks to see if the assets can be editted.
        /// </summary>
        /// <typeparam name="T">The generic asset type.</typeparam>
        /// <param name="asset">The asset data to check against.</param>
        /// <returns>True if the asset can be editted. False otherwise.</returns>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Data/NPCDispositions")
                || asset.AssetNameEquals("Data/NPCGiftTastes");
        }

        /// <summary>
        /// Edits the assets to be reconstructed to their proper form for SVE. Edits the disposition and the 
        /// gift taste of the targeted NPCs.
        /// </summary>
        /// <typeparam name="T">The generic asset type.</typeparam>
        /// <param name="asset">The asset data to edit.</param>
        public void Edit<T>(IAssetData asset)
        {
            var data = asset.AsDictionary<string, string>().Data;

            if (asset.AssetNameEquals("Data/NPCDispositions"))
            {
                data["Marlon"] = "adult/neutral/neutral/positive/male/not-datable/null/Town/winter 5//AdventureGuild 6 13/Marlon";
                if (Game1.MasterPlayer != null && !Game1.MasterPlayer.hasCompletedCommunityCenter())
                {
                    data["Morris"] = "adult/polite/neutral/negative/male/not-datable/null/Town/summer 1//JojaMart 27 26/Morris";
                }
            }
            else if (asset.AssetNameEquals("Data/NPCGiftTastes"))
            {
                data["Marlon"] = "This is a mighty gift, @. Thank you./244 413 437 439 680/Thanks. I'll find some use of this./346 303 348 459 205 422 287 288/Hmmm... that will be a hard pass. My apologies./-81/This unfortunately doesn't suit me./-80 283 233/Thanks./286 424 426 436 438 803 184 186 420 -28/";
                if (Game1.MasterPlayer != null && !Game1.MasterPlayer.hasCompletedCommunityCenter())
                {
                    data["Morris"] = "Ah! A gift worthy of my attention. Thank you, @./730 727 432 578/This is delightful!/72 348 430 428/I don't like this.../346 459 303 -81/What a despicable gift. Get this away from me./167 -74/That's generous of you./-75 -79 -80/";
                }
            }
            //Throw an error to the smapi console if we can't edit the assets
            else throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'");
        }

        /// <summary>
        /// From Spacechase0's CustomNPCFixes. We just need a bit more control when this triggers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void NpcFixes(object sender, EventArgs args) {
            // This needs to be called again so that custom NPCs spawn in locations added after the original call
            Game1.fixProblems();

            // Before we populate the route list, we need to fix doors from conditional CP patches and such.
            // This can be removed once SMAPI 3.0 comes out.
            foreach (var loc in Game1.locations) {
                loc.doors.Clear();
                for (int x = 0; x < loc.map.Layers[0].LayerWidth; ++x) {
                    for (int y = 0; y < loc.map.Layers[0].LayerHeight; ++y) {
                        if (loc.map.GetLayer("Buildings").Tiles[x, y] != null) {
                            PropertyValue propertyValue3 = (PropertyValue)null;
                            loc.map.GetLayer("Buildings").Tiles[x, y].Properties.TryGetValue("Action", out propertyValue3);
                            if (propertyValue3 != null && propertyValue3.ToString().Contains("Warp")) {
                                string[] strArray = propertyValue3.ToString().Split(' ');
                                if (strArray[0].Equals("WarpCommunityCenter"))
                                    loc.doors.Add(new Point(x, y), new NetString("CommunityCenter"));
                                else if ((!loc.name.Equals((object)"Mountain") || x != 8 || y != 20) && strArray.Length > 2)
                                    loc.doors.Add(new Point(x, y), new NetString(strArray[3]));
                            }
                        }
                    }
                }
            }

            // Similarly, this needs to be called again so that pathing works.
            NPC.populateRoutesFromLocationToLocationList();

            // Schedules for new NPCs don't work the first time.
            fixSchedules();
        }

        private void Clean(object sender, EventArgs e) {
            Marlon = null;
            Morris = null;
        }

        /// <summary>
        /// From Spacechase0's CustomNPCFixes. We just need a bit more control when this triggers.
        /// </summary>
        private void fixSchedules() {
            foreach (var npc in Utility.getAllCharacters()) {
                if (npc.Schedule == null) {
                    try {
                        npc.Schedule = npc.getSchedule(Game1.dayOfMonth);
                        npc.checkSchedule(Game1.timeOfDay);
                    } catch (Exception e) {
                        Monitor.Log("Exception doing schedule for NPC " + npc.Name + ": " + e, LogLevel.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Start of day routine. Occurs after game load / game saved. Creates the needed NPCs if applicable, then swaps out the Vanilla
        /// <see cref="NPC"/> instance for the custom <see cref="SocialNPC"/> instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDayStarted(object sender, EventArgs e)
        {
            // Create NPCs, if needed.
            SetUpMarlon();
            SetUpMorris();

            // Generate list of NPCs
            var npcList = new List<SocialNPC>() { Marlon };
            if (Morris != null) npcList.Add(Morris);

            // For each NPC within the list, add wrapped NPC (SocialNPC) and chuck out the vanilla NPC.
            foreach (SocialNPC npc in npcList)
            {
                npc.OriginalNpc.currentLocation.characters.Add(npc);
                if (!npc.OriginalNpc.currentLocation.characters.Remove(npc.OriginalNpc)) {
                    Monitor.Log($"Unable to remove SocialNPC: {npc.Name}");
                }
                npc.ForceReload();
            }

            Game1.fixProblems();
            fixSchedules();

            this.PrintNPCs(Marlon.Name);
            this.PrintNPCs(Morris.Name);

            this.RemoveDuplicates(Marlon.Name);
            this.RemoveDuplicates(Morris.Name);
        }

        /// <summary>
        /// A console command to list NPCs in memory. If a NPC name is provided, display the information regarding NPC.
        /// If no names are provided, displays a list of all NPCs in memory.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="argv"></param>
        private void ListNpcs(string command, string[] argv) {
            if (argv.Length == 0) {
                foreach (GameLocation loc in Game1.locations) {
                    foreach (NPC npc in loc.characters) {
                        this.Monitor.Log($"{npc.Name} - {npc.currentLocation.Name} - {npc.Position}");
                    }
                }
            } else {
                this.PrintNPCs(argv[0]);
                //NPC npc = Game1.getCharacterFromName<NPC>(argv[0]);
                //if (npc != null) {
                //    this.Monitor.Log($"Name: {npc.Name}");
                //    this.Monitor.Log($"Location:  { npc.currentLocation.Name }");
                //    this.Monitor.Log($"Position: {npc.Position}");
                //    this.Monitor.Log($"Schedule: {npc.Schedule}");
                //}
            }
        }

        /// <summary>
        /// Sets up NPC Marlon as a <see cref="SocialNPC"/>, if it wasn't already set up.
        /// </summary>
        private void SetUpMarlon() {
            Marlon = new SocialNPC(Game1.getCharacterFromName("Marlon", mustBeVillager: true), new Vector2(4, 11));
        }

        /// <summary>
        /// Sets up NPC Morris as a <see cref="SocialNPC"/>, if it wasn't already set up.
        /// </summary>
        private void SetUpMorris() {

            // If CC hasn't been completed then we create Morris
            if (Game1.MasterPlayer != null && !Game1.MasterPlayer.hasCompletedCommunityCenter()) {
                var blah = Game1.getCharacterFromName("Morris", mustBeVillager: true);
                if (blah == null) {
                    var morris = new NPC {
                        DefaultMap = "JojaMart",
                        FacingDirection = 3,
                        Name = "Morris",
                        Portrait = this.Helper.Content.Load<Texture2D>("[SVE] Morris/assets/Image/Morris.png")
                    };
                    Morris = new SocialNPC(morris, new Vector2(27, 27));
                } else {
                    Morris = new SocialNPC(Game1.getCharacterFromName("Morris", mustBeVillager: true), new Vector2(27, 27));
                }
            }
        }

        /// <summary>
        /// End of Day routine. Occurs before saving. Swaps the wrapped, custom NPCs (<see cref="SocialNPC"/>) with the Vanilla NPC.
        /// </summary>
        /// <param name="sender">The caller of the event.</param>
        /// <param name="args">Arguments passed in</param>
        private void OnDayEnding(object sender, DayEndingEventArgs args)
        {
            if (Marlon == null && Morris == null) return;

            var npcList = new List<SocialNPC>() { Marlon };

            if (Game1.MasterPlayer != null && !Game1.MasterPlayer.hasCompletedCommunityCenter())
            {
                npcList.Add(Morris);
            }

            foreach (SocialNPC npc in npcList)
            {
                npc.currentLocation.characters.Add(npc.OriginalNpc);
                if(!npc.currentLocation.characters.Remove(npc)) {
                    Monitor.Log($"Unable to remove SocialNPC: {npc.Name}");
                }
            }

            this.PrintNPCs(Marlon.Name);
            this.PrintNPCs(Morris.Name);
        }

        private List<NPC> FindNPC(string name) {
            List<NPC> found = new List<NPC>();
            foreach (GameLocation loc in Game1.locations) {
                foreach (NPC npc in loc.characters) {
                    if (npc.Name == name) {
                        found.Add(npc);
                    }
                }
            }

            return found;  
        }

        private void RemoveDuplicates(string name) {
            List<NPC> list = FindNPC(name);
            foreach (NPC npc in list) {
                if (npc.GetType() != typeof(SocialNPC)) {
                    Monitor.Log($"Removing duplicate of {npc.Name}. Type: {npc.GetType()}", LogLevel.Info);
                    if (npc.currentLocation.characters.Remove(npc)) {
                        Monitor.Log($"Success!", LogLevel.Info);
                    }
                }
            }
        }

        private void PrintNPCs(string name) {
            List<NPC> list = this.FindNPC(name);

            this.Monitor.Log($"Found {list.Count} cases of {name}.", LogLevel.Info);
            foreach (NPC npc in list) {
                Type type = npc.GetType();
                this.Monitor.Log($"{npc.Name} - {npc.currentLocation.Name} - {npc.Position} - {type.ToString()}");
            }
        }
    }
}
