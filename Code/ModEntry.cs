using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RidgesideVillage;
using StardewValley.Menus;
using StardewValley.Objects;
using Enumerable = System.Linq.Enumerable;

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
            helper.Events.Player.Warped += PlayerOnWarped;

            SpiritsEveChestEditor.SpiritsEveChestEditor.Enable(helper);

            RidgesideVillage.InstallationChecker.AutoCheck(Helper, Monitor);

            CustomBuffs.Enable(helper, Monitor);
            ClintVolumeControl.Enable(helper, Monitor);
            FireflySpawner.Enable(helper, Monitor);
            ConditionalLightSources.Enable(helper, Monitor);
            CustomBackgrounds.Enable(helper, Monitor);
            SpecialOrderNPCIcons.Enable(helper, Monitor);
            RemoveCropLayerCrops.Enable(helper, Monitor);
            FixIslandToFarmObelisk.Enable(helper, Monitor);
            AddSpecialOrdersAfterEvents.Enable(helper, Monitor);
            TouchActionProperties.Enable(helper, Monitor);
            CustomCauldronEffects.Enable(helper);

            var harmony = new Harmony(this.ModManifest.UniqueID);

            EndNexusMusic.Hook(harmony, Monitor);
            ActionProperties.ApplyPatch(harmony, Monitor);
            DisableShadowAttacks.ApplyPatch(harmony, helper, Monitor);
            HarmonyPatch_Mountain.Apply(harmony, this.Monitor);
            HarmonyPatch_CustomFishPondColors.ApplyPatch(harmony, Monitor);
            HarmonyPatch_FarmComputerLocations.ApplyPatch(harmony, Monitor);
            HarmonyPatch_PiggyBank.ApplyPatch(harmony, Monitor);
            HarmonyPatch_FixDesertBusWarp.ApplyPatch(harmony, Monitor);
            HarmonyPatch_DesertSecretNoteTile.ApplyPatch(harmony, Monitor);
            HarmonyPatch_CatStatue.ApplyPatch(harmony, Monitor);
            HarmonyPatch_DesertFishingItems.ApplyPatch(harmony, Monitor);
            HarmonyPatch_CustomGrangeJudging.ApplyPatch(harmony, helper, Monitor);
            HarmonyPatch_MovieTheaterNPCs.ApplyPatch(harmony, Monitor);
            HarmonyPatch_DestroyableBushesSVE.ApplyPatch(harmony, Monitor);
            HarmonyPatch_TMXLLoadMapFacingDirection.ApplyPatch(harmony, Monitor);
            HarmonyPatch_UntimedSpecialOrders.ApplyPatch(harmony, helper, Monitor);
            HarmonyPatch_FixCommunityShortcuts.ApplyPatch(harmony, helper, Monitor);

            foreach (var ctor in typeof(ShopMenu).GetConstructors())
            {
                if (ctor.GetParameters().Length == 0)
                    continue;
                
                harmony.Patch(
                    original: ctor,
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.setUpShopOwner_Postfix))
                );
            }

            harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.addOneTimeGiftBox)),
                prefix: new HarmonyMethod( typeof( ModEntry ), nameof( FixGuildGiftBoxPatch ) ) );

            harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(Forest.MakeMapModifications)),
                postfix: new HarmonyMethod( typeof( ModEntry ), nameof(ForestMoveDerbyContestantsPatch) ) );
            harmony.Patch(AccessTools.Method(typeof(ResourceClump), nameof(ResourceClump.performToolAction)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(SuppressPrismaticShardPatch1)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SuppressPrismaticShardPatch2)));
            harmony.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.createMultipleItemDebris)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(SuppressPrismaticShardPatch3)));

            harmony.Patch(AccessTools.Method(typeof(Town), nameof(Town.draw)),
                transpiler: new HarmonyMethod(this.GetType(), nameof(MoveBooksellerSign)));
            
            harmony.PatchAll( Assembly.GetExecutingAssembly() );
        }

        private static IEnumerable<CodeInstruction> MoveBooksellerSign(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> ret = new();

            bool foundBookseller = false;
            foreach (var insn in instructions)
            {
                if (insn.opcode == OpCodes.Ldfld && insn.operand is FieldInfo finfo && finfo.Name == "showBookseller")
                    foundBookseller = true;
                else if (insn.opcode == OpCodes.Newobj && foundBookseller) 
                {
                    // First new object is a vector2 that we need
                    ret[ret.Count - 2].operand = 5f;
                    ret[ret.Count - 1].operand = 48f;
                    foundBookseller = false;
                }

                ret.Add(insn);
            }

            return ret;
        }

        private void PlayerOnWarped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation == null)
                return;

            if (e.NewLocation.NameOrUniqueName == "Custom_SVESummit")
            {
                Game1.background = new Background(Game1.getLocationFromName("Summit") as Summit);
            }
            else if (e.OldLocation.NameOrUniqueName == "Custom_SVESummit")
            {
                Game1.background = null;
            }
            
            if (e.NewLocation.NameOrUniqueName == "Town")
            {
                foreach (var tas in Enumerable.ToList(e.NewLocation.TemporarySprites))
                {
                    float tx = tas.Position.X;
                    float ty = tas.Position.Y;
                    if (tx >= 55 * Game1.tileSize && tx <= 57 * Game1.tileSize && ty >= 45 * Game1.tileSize && ty <= 47 * Game1.tileSize
                        && tas.textureName == Game1.mouseCursors1_6Name )
                    {
                        e.NewLocation.TemporarySprites.Remove(tas);
                    }
                }

                foreach (var light in Game1.currentLightSources.ToList())
                {
                    if (light.textureIndex.Value == LightSource.townWinterTreeLight &&
                        light.position.Value == new Vector2(56, 46) * Game1.tileSize +
                        new Vector2(Game1.tileSize * 2, Game1.tileSize * 2.5f))
                    {
                        Game1.currentLightSources.Remove(light);
                    }
                }
            }
            if (e.NewLocation.Name == "Farm" && Game1.whichFarm == 0 && !Game1.getFarm().modData.ContainsKey( "SVE.SpawnedDogHouse" ) )
            {
                int x = -1, y = -1;
                if (Helper.ModRegistry.IsLoaded("flashshifter.immersivefarm2remastered"))
                {
                    x = 52;
                    y = 6;
                }
                else if (Helper.ModRegistry.IsLoaded("flashshifter.GrandpasFarm"))
                {
                    x = 101;
                    y = 37;
                }

                if (x != -1 && y != -1)
                {
                    Game1.getFarm().modData.Add("SVE.SpawnedDogHouse", "meow");
                    Game1.getFarm().furniture.Add(new Furniture("Doghouse", new Vector2( x, y )));
                }
            }

            if (e.NewLocation.Name == "Forest")
            {
                // Yeah apparently the patch wasn't triggering when loading the save? Weird
                ForestMoveDerbyContestantsPatch(e.NewLocation as Forest);
            }
        }

        private static bool shouldSuppressPrismaticShards = false;
        private static void SuppressPrismaticShardPatch1()
        {
            shouldSuppressPrismaticShards = true;
        }
        private static void SuppressPrismaticShardPatch2()
        {
            shouldSuppressPrismaticShards = false;
        }
        private static bool SuppressPrismaticShardPatch3(Item item, GameLocation location)
        {
            location ??= Game1.currentLocation;
            if (location.NameOrUniqueName == "Custom_IridiumQuarry" && item.QualifiedItemId == "(O)74" &&
                shouldSuppressPrismaticShards)
                return false;

            return true;
        }

        public static void FixGuildGiftBoxPatch(ref int x, ref int y, ref int whichGiftBox)
        {
            // Let's hope another map doesn't place one at at (10,4) I guess...
            if (x == 10 && y == 4 && whichGiftBox == 2)
            {
                x = 8;
                y = 5;
            }
        }

        public static void ForestMoveDerbyContestantsPatch(Forest __instance)
        {
            if (__instance.getCharacterFromName("derby_contestent0") == null)
                return;

            var dc4 = __instance.getCharacterFromName("derby_contestent4");
            if (dc4.Position == new Vector2(84, 40) * Game1.tileSize)
            {
                dc4.Position += new Vector2(6, 0) * Game1.tileSize;
            }
            var dc5 = __instance.getCharacterFromName("derby_contestent5");
            if (dc5.Position == new Vector2(88, 49) * Game1.tileSize)
            {
                dc5.Position += new Vector2(1, 2) * Game1.tileSize;
            }
            var dc9 = __instance.getCharacterFromName("derby_contestent9");
            if (dc9.Position == new Vector2(82, 51) * Game1.tileSize)
            {
                dc9.Position += new Vector2(1, -1) * Game1.tileSize;
            }
        }

        public static void setUpShopOwner_Postfix(string shopId, StardewValley.Menus.ShopMenu __instance)
        {
            try
            {
                if ("Traveler".Equals(shopId) //traveler's shop (default)
                || ("TravelerNightMarket").Equals(shopId)) //traveler's shop (night market)
                {
                    __instance.portraitTexture = Game1.content.Load<Texture2D>("Portraits\\Suki");
                    if (Game1.currentLocation is DesertFestival)
                    {
                            __instance.portraitTexture = Game1.content.Load<Texture2D>("Portraits\\Suki_DesertFestival");

                        //imitate skipped code from the original method, adding a dialogue window beneath the portrait
                        string ppDialogue = Game1.content.LoadString("Strings\\StringsFromCSFiles:ShopMenu.cs.11457");
                        __instance.potraitPersonDialogue = Game1.parseText(ppDialogue, Game1.dialogueFont, 304);
                    }
                }
                else if (Game1.dayOfMonth == 8 && Game1.currentSeason == "winter" && (Game1.player.currentLocation.currentEvent?.isFestival ?? false) ) //ice festival shop
                {
                    __instance.portraitTexture = Game1.content.Load<Texture2D>("Portraits\\Suki_IceFestival");

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
            if ((Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.hasCompletedCommunityCenter()) && !this.Helper.ModRegistry.IsLoaded("Yoshimax.MarryMorris"))
            {
                var morris = Game1.getCharacterFromName("MorrisTod");
                morris.currentLocation.characters.Remove(morris);
            }
        }



            private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (Context.IsMainPlayer)
            {
                var migrator = new JsonAssets.ItemMigrator(File.ReadAllLines(Path.Combine(Helper.DirectoryPath, "assets", "ja-legacy.txt")), Helper.Reflection);
                migrator.Migrate();
            }

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

        [HarmonyPatch(typeof(Town), nameof(Town.DayUpdate))]
        public static class HalloweenStuffPatch
        {
            public static void Postfix(Town __instance, int dayOfMonth)
            {
                  if (Game1.IsFall && Game1.dayOfMonth == 17)
                  {
                      __instance.moveObject(9, 86, 7, 89, null);
                      __instance.moveObject(21, 89, 22, 89, null);
                      __instance.moveObject(63, 63, 55, 68, null);
                      __instance.tryPlaceObject(new Vector2(105, 90f), ItemRegistry.Create<StardewValley.Object>("(O)746"));

                      var f1 = __instance.furniture.FirstOrDefault(f => f.TileLocation == new Vector2(43, 89));
                      if (f1 != null)
                          f1.TileLocation = new Vector2(44, 89);
                      
                      var f2 = __instance.furniture.FirstOrDefault(f => f.TileLocation == new Vector2(41, 85));
                      if (f2 != null)
                          f2.TileLocation = new Vector2(41, 86);
                      
                  }
                  if (!Game1.IsWinter || Game1.dayOfMonth != 1)
                    return;
                  if (!__instance.objects.ContainsKey(new Vector2(44, 89)))
                      __instance.removeEverythingFromThisTile(44, 89);
                  if (!__instance.objects.ContainsKey(new Vector2(41, 86)))
                      __instance.removeEverythingFromThisTile(41, 86);
                  __instance.removeObjectAtTileWithName(7, 89, "Rotten Plant");
                  __instance.removeObjectAtTileWithName(22, 86, "Rotten Plant");
                  __instance.removeObjectAtTileWithName(55, 68, "Rotten Plant");
                  __instance.removeObjectAtTileWithName(105, 90, "Rotten Plant");
            }
        }
        
        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.DayUpdate))]
        public static class HalloweenStuffPatch2
        {
            public static void Postfix(GameLocation __instance, int dayOfMonth)
            {
                bool removeObjectAtTileWithName(int x, int y, string name)
                {
                    Vector2 key = new Vector2((float) x, (float) y);
                    if (!__instance.objects.ContainsKey(key) || !__instance.objects[key].Name.Equals(name))
                        return false;
                    __instance.objects.Remove(key);
                    return true;
                }
                
                if (__instance.NameOrUniqueName == "Forest")
                {
                    if (Game1.IsFall && Game1.dayOfMonth == 17)
                    {
                        __instance.tryPlaceObject(new Vector2(103, 34f), ItemRegistry.Create<StardewValley.Object>("(O)746"));
                        __instance.tryPlaceObject(new Vector2(92, 20), ItemRegistry.Create<StardewValley.Object>("(O)746"));
                    }
                    if (!Game1.IsWinter || Game1.dayOfMonth != 1)
                        return;
                    removeObjectAtTileWithName(103, 34, "Rotten Plant");
                    removeObjectAtTileWithName(92, 20, "Rotten Plant");
                    
                }
                else 
                if (__instance.NameOrUniqueName == "Custom_BlueMoonVineyard")
                {
                    if (Game1.IsFall && Game1.dayOfMonth == 17)
                    {
                        __instance.tryPlaceObject(new Vector2(26, 48), ItemRegistry.Create<StardewValley.Object>("(O)746"));
                    }
                    if (!Game1.IsWinter || Game1.dayOfMonth != 1)
                        return;
                    removeObjectAtTileWithName(26, 48, "Rotten Plant");
                    
                }
            }
        }
    }
}