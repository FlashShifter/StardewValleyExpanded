using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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

        //The mod entry point, which is called when the mod is loaded
        //The IModHelper parameter is what provides the simplified api for us to use
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        }

        //Checks whether this instance can load the initial version of the given asset
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

        /* Loading the new assets when the found asset names match.
         * So if the original asset found matches the path in the if statement
         * then we provide the new asset we want to be used which can be found in the provided path
         */
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

        //Checks to see if we can edit an asset
        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Data/NPCDispositions")
                || asset.AssetNameEquals("Data/NPCGiftTastes");
        }

        /* Here's where we actually edit the asset to be what we want it to be.
         * In this case we're editing the disposition and gift taste to be how we want them
         * to be by just setting the strings..
         */
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
        /// Start of day routine. Occurs after game load / game saved. Creates the needed NPCs if applicable, then swaps out the Vanilla
        /// <see cref="NPC"/> instance for the custom <see cref="SocialNPC"/> instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
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
                npc.OriginalNpc.currentLocation.characters.Remove(npc.OriginalNpc);
                npc.ForceReload();
            }
        }

        /// <summary>
        /// Sets up NPC Marlon as a <see cref="SocialNPC"/>, if it wasn't already set up.
        /// </summary>
        private void SetUpMarlon()
        {
            // Check if NPC was already created
            if (Marlon != null) return;
            Marlon = new SocialNPC(Game1.getCharacterFromName("Marlon", mustBeVillager: true), new Vector2(4, 11));
        }

        /// <summary>
        /// Sets up NPC Morris as a <see cref="SocialNPC"/>, if it wasn't already set up.
        /// </summary>
        private void SetUpMorris()
        {
            // Check if NPC was already created
            if (Morris != null) return;

            // If CC hasn't been completed then we create Morris
            if (Game1.MasterPlayer != null && !Game1.MasterPlayer.hasCompletedCommunityCenter())
            {
                var blah = Game1.getCharacterFromName("Morris", mustBeVillager: true);
                if (blah == null)
                {
                    var morris = new NPC
                    {
                        DefaultMap = "JojaMart",
                        FacingDirection = 3,
                        Name = "Morris",
                        Portrait = this.Helper.Content.Load<Texture2D>("[SVE] Morris/assets/Image/Morris.png")
                    };
                    Morris = new SocialNPC(morris, new Vector2(27, 27));
                }
                else
                {
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
            var npcList = new List<SocialNPC>() { Marlon };

            if (Game1.MasterPlayer != null && !Game1.MasterPlayer.hasCompletedCommunityCenter())
            {
                npcList.Add(Morris);
            }

            foreach (SocialNPC npc in npcList)
            {
                npc.currentLocation.characters.Add(npc.OriginalNpc);
                npc.currentLocation.characters.Remove(npc);
            }
        }
    }
}
