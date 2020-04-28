using Harmony;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using SuperAardvark.AntiSocial;
using System;
using System.Collections.Generic;

namespace StardewValleyExpanded
{
    public class ModEntry : Mod
    {
        private static Mod modInstance;

        public override void Entry(IModHelper helper)
        {
            ModEntry.modInstance = this;

            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;

            AntiSocialManager.DoSetupIfNecessary(this);

            var harmony = HarmonyInstance.Create(this.ModManifest.UniqueID);

            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Utility), nameof(StardewValley.Utility.getCelebrationPositionsForDatables), new Type[] { typeof(List<string>) }),
               postfix: new HarmonyMethod(typeof(CustomWeddingGuests), nameof(CustomWeddingGuests.getCelebrationPositionsForDatables_Postfix))
            );

            helper.Content.AssetLoaders.Add(new CustomWeddingGuests(this));

        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            //Removes Morris from the game when community center completion = 'true'
            if (Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.hasCompletedCommunityCenter())
            {
                Game1.removeCharacterFromItsLocation("MorrisTod");
            }
            Game1.removeCharacterFromItsLocation("Marlon");
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Reflection to access private 'Game1.Multiplayer'
            Multiplayer multiplayer = this.Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Creates some animal objects
            FarmAnimal whiteCow1 = new FarmAnimal("White Cow", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(96 * Game1.tileSize, 19 * Game1.tileSize)
            };

            FarmAnimal brownCow2 = new FarmAnimal("Brown Cow", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(118 * Game1.tileSize, 18 * Game1.tileSize)
            };

            FarmAnimal brownCow1 = new FarmAnimal("Brown Cow", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(101 * Game1.tileSize, 18 * Game1.tileSize)
            };

            FarmAnimal goat1 = new FarmAnimal("Goat", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(115 * Game1.tileSize, 20 * Game1.tileSize)
            };

            FarmAnimal pig1 = new FarmAnimal("Pig", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(110 * Game1.tileSize, 14 * Game1.tileSize)
            };

            FarmAnimal babyCow1 = new FarmAnimal("Baby Cow", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(108 * Game1.tileSize, 16 * Game1.tileSize)
            };

            FarmAnimal babyCow2 = new FarmAnimal("Baby Cow", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(107 * Game1.tileSize, 8 * Game1.tileSize)
            };

            // Adds animals to Marnie's Ranch
            (Game1.getLocationFromName("Forest") as Forest).marniesLivestock.Add(whiteCow1);
            (Game1.getLocationFromName("Forest") as Forest).marniesLivestock.Add(brownCow1);
            (Game1.getLocationFromName("Forest") as Forest).marniesLivestock.Add(brownCow2);
            (Game1.getLocationFromName("Forest") as Forest).marniesLivestock.Add(goat1);
            (Game1.getLocationFromName("Forest") as Forest).marniesLivestock.Add(pig1);
            (Game1.getLocationFromName("Forest") as Forest).marniesLivestock.Add(babyCow1);
            (Game1.getLocationFromName("Forest") as Forest).marniesLivestock.Add(babyCow2);
        }


    }
}