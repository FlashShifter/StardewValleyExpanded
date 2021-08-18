using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public static Mod modInstance;

        public override void Entry(IModHelper helper)
        {
            ModEntry.modInstance = this;

            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;

            AntiSocialManager.DoSetupIfNecessary(this);

            SpiritsEveChestEditor.SpiritsEveChestEditor.Enable(helper);

            CustomCauldronEffects.Enable(Helper);

            CustomBuffs.Enable(Helper);

            ClintVolumeControl.Enable(Helper, Monitor);

            FireflySpawner.Enable(Helper, Monitor);

            SpecialOrderNPCIcons.Enable(Helper, Monitor);

            FixIslandToFarmObelisk.Enable(helper, Monitor);

            AddSpecialOrdersAfterEvents.Enable(Helper, Monitor);

            var harmony = new Harmony(this.ModManifest.UniqueID);

            EndNexusMusic.Hook(harmony, Monitor);

            CustomLocationWateringFixes.ApplyFixes(helper, Monitor, harmony);

            HarmonyPatch_GetFishingLocation.ApplyPatch(harmony, Monitor);

            HarmonyPatch_SpousePatioAnimations.ApplyPatch(harmony, Monitor);

            HarmonyPatch_CustomFishPondColors.ApplyPatch(harmony, Monitor);

            HarmonyPatch_FarmComputerLocations.ApplyPatch(harmony, Monitor);

            HarmonyPatch_FixDesertBusWarp.ApplyPatch(harmony, Monitor);

            HarmonyPatch_DesertSecretNoteTile.ApplyPatch(harmony, Monitor);

            HarmonyPatch_DesertFishingItems.ApplyPatch(harmony, Monitor);

            HarmonyPatch_CustomGrangeJudging.ApplyPatch(harmony, Helper, Monitor);

            HarmonyPatch_MovieTheaterNPCs.ApplyPatch(harmony, Monitor);

            HarmonyPatch_TMXLLoadMapFacingDirection.ApplyPatch(harmony, Monitor);

            HarmonyPatch_UntimedSpecialOrders.ApplyPatch(harmony, helper, Monitor);

            HarmonyPatch_FixCommunityShortcuts.ApplyPatch(harmony, Helper, Monitor);

            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Utility), nameof(StardewValley.Utility.getCelebrationPositionsForDatables), new Type[] { typeof(List<string>) }),
               postfix: new HarmonyMethod(typeof(CustomWeddingGuests), nameof(CustomWeddingGuests.getCelebrationPositionsForDatables_Postfix))
            );

            helper.Content.AssetLoaders.Add(new CustomWeddingGuests(this));

            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Menus.ShopMenu), nameof(StardewValley.Menus.ShopMenu.setUpShopOwner), new Type[] { typeof(string) }),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.setUpShopOwner_Postfix))
            );
        }

        public static void setUpShopOwner_Postfix(string who, ref StardewValley.Menus.ShopMenu __instance)
        {
            try
            {
                if ("Traveler".Equals(who) //traveler's shop (default)
                || ("TravelerNightMarket").Equals(who)) //traveler's shop (night market)
                {
                    NPC suki = new NPC();
                    suki.Portrait = Game1.content.Load<Texture2D>("Portraits\\Suki");
                    __instance.portraitPerson = suki;
                }
                else if (Game1.dayOfMonth == 8 && Game1.currentSeason == "winter" && Game1.player.currentLocation.NameOrUniqueName == "Temp") //ice festival shop
                {
                    NPC suki = new NPC();
                    suki.Portrait = Game1.content.Load<Texture2D>("Portraits\\Suki_IceFestival");
                    __instance.portraitPerson = suki;

                    //imitate skipped code from the original method, adding a dialogue window beneath the portrait
                    string ppDialogue = Game1.content.LoadString("Strings\\StringsFromCSFiles:ShopMenu.cs.11457");
                    __instance.potraitPersonDialogue = Game1.parseText(ppDialogue, Game1.dialogueFont, 304);
                }
            }
            catch (Exception ex)
            {
                modInstance.Monitor.LogOnce($"Harmony patch \"{nameof(setUpShopOwner_Postfix)}\" has encountered an error and may not function correctly:\n{ex.ToString()}", LogLevel.Error);
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            //Removes Morris from the game when community center completion = 'true'
            if (Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.hasCompletedCommunityCenter())
            {
                Game1.removeCharacterFromItsLocation("MorrisTod");
            }
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

            FarmAnimal babyCow1 = new FarmAnimal("Dairy Cow", multiplayer.getNewID(), -1L)
            {
                Position = new Vector2(108 * Game1.tileSize, 16 * Game1.tileSize)
            };

            FarmAnimal babyCow2 = new FarmAnimal("Dairy Cow", multiplayer.getNewID(), -1L)
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