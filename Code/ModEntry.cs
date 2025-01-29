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
using StardewValley.Tools;
using StardewValley.Buildings;
using StardewValley.GameData.LocationContexts;
using StardewValley.TokenizableStrings;
using StardewValley.GameData.Buildings;

namespace StardewValleyExpanded
{
    public class ModEntry : Mod
    {
        public static Mod modInstance;
        public static IContentPack cpPack;

        public override void Entry(IModHelper helper)
        {
            ModEntry.modInstance = this;

            I18n.Init(Helper.Translation);

            var mi = Helper.ModRegistry.Get("FlashShifter.StardewValleyExpandedCP");
            cpPack = mi.GetType().GetProperty("ContentPack")?.GetValue(mi) as IContentPack;

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

            Event.RegisterCommand("SVE_BadlandsDeath", BadlandsDeathEventCommand);

            void DoShrineCommon(GameLocation loc, string id, string item, Point pos, string translationKey, Action<bool> doStuff, Color smokeCol)
            {
                loc.createQuestionDialogue(cpPack.Translation.Get(translationKey).ToString(),
                    loc.createYesNoResponses(),
                    translationKey);
                loc.afterQuestion = (Farmer who, string whichAnswer) =>
                {
                    if (whichAnswer != "Yes")
                        return;

                    if (!Game1.player.Items.ContainsId(item, 1))
                    {
                        Game1.drawObjectDialogue(cpPack.Translation.Get("DarkShrine.OfferingRejected").ToString());
                        return;
                    }
                    Game1.player.Items.ReduceId(item, 1);

                    for (int j = 0; j < 20; j++)
                    {
                        Game1.Multiplayer.broadcastSprites(loc, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), pos.ToVector2() * 64f + new Vector2(Game1.random.Next(-32, 64), Game1.random.Next(16)), flipped: false, 0.002f, smokeCol)
                        {
                            alpha = 0.75f,
                            motion = new Vector2(0f, -0.5f),
                            acceleration = new Vector2(-0.002f, 0f),
                            interval = 99999f,
                            layerDepth = (pos.Y * Game1.tileSize) / 10000f + (float)Game1.random.Next(100) / 10000f,
                            scale = 3f,
                            scaleChange = 0.01f,
                            rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f,
                            delayBeforeAnimationStart = j * 25
                        });
                    }

                    string stateId = $"SVE_{id}ShrineActivated";
                    if (Game1.netWorldState.Value.hasWorldStateID(stateId))
                    {
                        Game1.netWorldState.Value.removeWorldStateID(stateId);
                        Game1.worldStateIDs.Remove(stateId);

                        Helper.Reflection.GetField<HashSet<string>>(loc, "_appliedMapOverrides").GetValue().Clear();
                        loc.loadMap(loc.mapPath.Value, true);

                        (string id, Point pos)[] allshrines =
                        {
                            new("Betrayal", new(26, 23)),
                            new("Moss", new(36, 23)),
                            new("Invasion", new(31, 18))
                        };
                        foreach (var shrine in allshrines)
                        {
                            if (shrine.id == id)
                                continue;

                            if (Game1.netWorldState.Value.hasWorldStateID($"SVE_{shrine.id}ShrineActivated"))
                            {
                                string mapPath = $"assets/Maps/MapPatches/HenchmanBackyard_{shrine.id}Shrine_Fire.tmx";
                                var map = cpPack.ModContent.Load<xTile.Map>(mapPath);
                                loc.ApplyMapOverride(map, shrine.id, new(0, 0, 1, 1), new(shrine.pos, new(1, 1)));
                            }
                        }

                        doStuff(false);
                    }
                    else
                    {
                        Game1.netWorldState.Value.addWorldStateID(stateId);
                        Game1.worldStateIDs.Add(stateId);

                        string mapPath = $"assets/Maps/MapPatches/HenchmanBackyard_{id}Shrine_Fire.tmx";
                        var map = cpPack.ModContent.Load<xTile.Map>(mapPath);
                        loc.ApplyMapOverride(map, stateId, new(0, 0, 1, 1), new(pos, new(1, 1)));

                        doStuff(true);
                    }
                };
            }

            GameLocation.RegisterTileAction("DarkShrineInvasion", (GameLocation loc, string[] args, Farmer who, Point tilePos) =>
            {
                DoShrineCommon(loc, "Invasion", "(O)FlashShifter.StardewValleyExpandedCP_Ornate_Treasure_Chest", new(31, 18), "DarkShrine.Invasion.01", (newState) =>
                {
                    Game1.playSound("fireball");
                    Game1.playSound("serpentDie");

                    Game1.drawObjectDialogue(newState ? I18n.InvasionWarningOn() : I18n.InvasionWarningOff()); ;
                }, Color.Red);
                return true;
            });
            GameLocation.RegisterTileAction("DarkShrineMoss", (GameLocation loc, string[] args, Farmer who, Point tilePos) =>
            {
                DoShrineCommon(loc, "Moss", "(O)FlashShifter.StardewValleyExpandedCP_Gold_Carrot", new(36, 23), "DarkShrine.Moss.01", (newState) =>
                {
                    Game1.playSound("fireball");
                    Game1.playSound("leafrustle");
                    Game1.playSound("leafrustle");

                    Utility.ForEachLocation((loc) =>
                    {
                        foreach (var t in loc.terrainFeatures.Values.Where(tf => tf is Tree).Cast<Tree>())
                        {
                            if (newState)
                                t.hasMoss.Value = false;
                            t.stopGrowingMoss.Value = newState;
                        }
                        return true;
                    }, true, true);
                }, Color.Green);
                return true;
            });
            GameLocation.RegisterTileAction("DarkShrineBetrayal", (GameLocation loc, string[] args, Farmer who, Point tilePos) =>
            {
                if (!who.eventsSeen.Contains(DisableShadowAttacks.DisableShadowsEventID.ToString()))
                {
                    Game1.drawObjectDialogue(cpPack.Translation.Get("DarkShrine.Betrayal.02").ToString());
                    return true;
                }

                DoShrineCommon(loc, "Betrayal", "(O)FlashShifter.StardewValleyExpandedCP_Void_Shard", new(26, 23), "DarkShrine.Betrayal.01", (newState) =>
                {
                    Game1.playSound("fireball");
                    Game1.playSound("shadowpeep");
                }, Color.DarkGray);
                return true;
            });

            var harmony = new Harmony(this.ModManifest.UniqueID);
            EndNexusMusic.Hook(harmony, Monitor);
            ActionProperties.ApplyPatch(harmony, Monitor);
            DisableShadowAttacks.ApplyPatch(harmony, helper, Monitor);
            HarmonyPatch_Mountain.Apply(harmony, this.Monitor);
            HarmonyPatch_FarmComputerLocations.ApplyPatch(harmony, Monitor);
            HarmonyPatch_PiggyBank.ApplyPatch(harmony, Monitor);
            HarmonyPatch_FixDesertBusWarp.ApplyPatch(harmony, Monitor);
            HarmonyPatch_DesertSecretNoteTile.ApplyPatch(harmony, Monitor);
            HarmonyPatch_DesertFishingItems.ApplyPatch(harmony, Monitor);
            HarmonyPatch_CustomGrangeJudging.ApplyPatch(harmony, helper, Monitor);
            HarmonyPatch_MovieTheaterNPCs.ApplyPatch(harmony, Monitor);
            HarmonyPatch_DestroyableBushesSVE.ApplyPatch(harmony, Monitor);
            HarmonyPatch_TMXLLoadMapFacingDirection.ApplyPatch(harmony, Monitor);
            HarmonyPatch_UntimedSpecialOrders.ApplyPatch(harmony, helper, Monitor);
            HarmonyPatch_FixCommunityShortcuts.ApplyPatch(harmony, helper, Monitor);

            harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.addOneTimeGiftBox)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(FixGuildGiftBoxPatch)));

            harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(Forest.MakeMapModifications)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ForestMoveDerbyContestantsPatch)));
            harmony.Patch(AccessTools.Method(typeof(ResourceClump), nameof(ResourceClump.performToolAction)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(SuppressPrismaticShardPatch1)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SuppressPrismaticShardPatch2)));
            harmony.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.createMultipleItemDebris)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(SuppressPrismaticShardPatch3)));

            harmony.Patch(AccessTools.Method(typeof(Town), nameof(Town.draw)),
                transpiler: new HarmonyMethod(this.GetType(), nameof(MoveBooksellerSign)));

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void BadlandsDeathEventCommand(Event @event, string[] args, EventContext context)
        {
            if (!Game1.dialogueUp)
            {
                int numberOfItemsLost = Game1.player.LoseItemsOnDeath();
                Game1.player.Stamina = Math.Min(Game1.player.Stamina, 2f);
                int moneyToLose = Math.Min(33333 + Game1.random.Next(66666 + 1), Game1.player.Money);
                Game1.player.Money -= moneyToLose;
                Game1.drawObjectDialogue(((moneyToLose > 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1068", moneyToLose) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1070")) + ((numberOfItemsLost > 0) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1071") + ((numberOfItemsLost == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1061") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1062", numberOfItemsLost))) : ""));
                @event.InsertNextCommand("showItemsLost");
            }
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

            if (e.NewLocation.Name == "Custom_HenchmanBackyard")
            {
                (string id, Point pos)[] allshrines =
                {
                    new("Betrayal", new(26, 23)),
                    new("Moss", new(36, 23)),
                    new("Invasion", new(31, 18))
                };
                foreach (var shrine in allshrines)
                {
                    if (Game1.netWorldState.Value.hasWorldStateID($"SVE_{shrine.id}ShrineActivated"))
                    {
                        string mapPath = $"assets/Maps/MapPatches/HenchmanBackyard_{shrine.id}Shrine_Fire.tmx";
                        var map = cpPack.ModContent.Load<xTile.Map>(mapPath);
                        e.NewLocation.ApplyMapOverride(map, shrine.id, new(0, 0, 1, 1), new(shrine.pos, new(1, 1)));
                    }
                }
            }

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
                        && tas.textureName == Game1.mouseCursors1_6Name)
                    {
                        e.NewLocation.TemporarySprites.Remove(tas);
                    }
                }

                foreach (var light in Game1.currentLightSources.Values.ToList())
                {
                    if (light.textureIndex.Value == LightSource.townWinterTreeLight &&
                        light.position.Value == new Vector2(56, 46) * Game1.tileSize +
                        new Vector2(Game1.tileSize * 2, Game1.tileSize * 2.5f))
                    {
                        Game1.currentLightSources.Remove(light.Id);
                    }
                }
            }
            if (e.NewLocation.Name == "Farm" && !Game1.getFarm().modData.ContainsKey("SVE.SpawnedDogHouse"))
            {
                int x = -1, y = -1;
                if (Game1.whichFarm == 0)
                {
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
                }
                else if (Game1.whichFarm == Farm.mod_layout && Game1.whichModFarm.Id == "FrontierFarm")
                {
                    x = 119;
                    y = 14;
                }

                if (x != -1 && y != -1)
                {
                    Game1.getFarm().modData.Add("SVE.SpawnedDogHouse", "meow");
                    Game1.getFarm().furniture.Add(new Furniture("Doghouse", new Vector2(x, y)));
                }
            }

            if (e.NewLocation.Name == "Forest")
            {
                // Yeah apparently the patch wasn't triggering when loading the save? Weird
                ForestMoveDerbyContestantsPatch(e.NewLocation as Forest);
            }

            foreach (var b in e.NewLocation.buildings)
            {
                if (b.buildingType.Value == "FlashShifter.StardewValleyExpandedCP_PremiumBarn")
                {
                    Point tileLoc = new(b.tileX.Value + 2, b.tileY.Value + 2);
                    var l = new LightSource($"SVE_PremiumBarnLight_{b.tileX.Value}_{b.tileY.Value}_1", 4, tileLoc.ToVector2() * Game1.tileSize, 1f, Color.Black, LightSource.LightContext.None);
                    Game1.currentLightSources.Add(l.Id, l);

                    tileLoc = new(b.tileX.Value + 8, b.tileY.Value + 2);
                    l = new LightSource($"SVE_PremiumBarnLight_{b.tileX.Value}_{b.tileY.Value}_2",4, tileLoc.ToVector2() * Game1.tileSize, 1f, Color.Black, LightSource.LightContext.None);
                    Game1.currentLightSources.Add(l.Id, l);
                }
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
            if (dc4?.Position == new Vector2(84, 40) * Game1.tileSize)
            {
                dc4.Position += new Vector2(6, 0) * Game1.tileSize;
            }
            var dc5 = __instance.getCharacterFromName("derby_contestent5");
            if (dc5?.Position == new Vector2(88, 49) * Game1.tileSize)
            {
                dc5.Position += new Vector2(1, 2) * Game1.tileSize;
            }
            var dc9 = __instance.getCharacterFromName("derby_contestent9");
            if (dc9?.Position == new Vector2(82, 51) * Game1.tileSize)
            {
                dc9.Position += new Vector2(1, -1) * Game1.tileSize;
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            //Removes Morris from the game when community center completion = 'true'
            if ((Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.eventsSeen.Contains("191393")) && !this.Helper.ModRegistry.IsLoaded("Yoshimax.MarryMorris"))
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
                    __instance.moveContents(9, 86, 7, 89, null);
                    __instance.moveContents(21, 89, 22, 89, null);
                    __instance.moveContents(63, 63, 55, 68, null);
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
                    Vector2 key = new Vector2((float)x, (float)y);
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

        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.IsGrowthBlocked))]
        public static class FruitTreeAllowGrowthInLocationsPatch
        {
            public static void Postfix(GameLocation environment, ref bool __result)
            {
                if (environment.NameOrUniqueName == "Custom_Highlands" || environment.NameOrUniqueName == "Custom_FerngillRepublicFrontier")
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Slingshot), nameof(Slingshot.GetAmmoDamage))]
        public static class SlingshotGalaxyDamageBuff
        {
            public static void Postfix(Slingshot __instance, ref int __result)
            {
                if (__instance.ItemId == "34")
                {
                    __result = (int)Math.Ceiling(__result * 1.33f);
                }
            }
        }

        [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.getCategoryName))]
        public static class WeaponCategoryPatch
        {
            public static void Postfix(MeleeWeapon __instance, ref string __result)
            {
                if (__instance.ItemId == "FlashShifter.StardewValleyExpandedCP_Heavy_Shield")
                {
                    __result = I18n.ShieldWithLevel(__instance.getItemLevel());
                }
                else if (__instance.ItemId == "FlashShifter.StardewValleyExpandedCP_Monster_Splitter")
                {
                    __result = I18n.GreatswordWithLevel(__instance.getItemLevel());
                }
                else if (__instance.ItemId == "FlashShifter.StardewValleyExpandedCP_Diamond_Wand")
                {
                    __result = I18n.WandWithLevel(__instance.getItemLevel());
                }
            }
        }

        [HarmonyPatch(typeof(Tree), nameof(Tree.performToolAction))]
        public static class TreeDisallowChoppingCustomInTownPatch
        {
            public static bool Prefix(Tree __instance, Tool t, Vector2 tileLocation, ref bool __result)
            {
                if (t is Axe && __instance.Location is Town && tileLocation.X < 100f && !__instance.isTemporaryGreenRainTree.Value)
                {
                    int pathsIndex = __instance.Location.getTileIndexAt((int)tileLocation.X, (int)tileLocation.Y, "Paths");
                    if (pathsIndex == 34)
                    {
                        __instance.Location.playSound("axchop", tileLocation);
                        __instance.shake(tileLocation, doEvenIfStillShaking: true);
                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\1_6_Strings:TownTreeWarning"));
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Building), nameof(Building.doesTileHaveProperty))]
        public static class BuildingDeluxeBarnDoorCursorPatch
        {
            public static void Postfix(Building __instance, int tile_x, int tile_y, string property_name, string layer_name, ref string property_value, ref bool __result)
            {
                if (__instance.buildingType.Value == "FlashShifter.StardewValleyExpandedCP_PremiumBarn" && __instance.daysOfConstructionLeft.Value <= 0)
                {
                    var interior = __instance.GetIndoors();
                    if (tile_x == __instance.tileX.Value + __instance.humanDoor.X + 8 &&
                        tile_y == __instance.tileY.Value + __instance.humanDoor.Y &&
                        interior != null)
                    {
                        if (property_name == "Action")
                        {
                            property_value = "meow";
                            __result = true;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Building), nameof(Building.doAction))]
        public static class BuildingDeluxeBarnDoorPatch
        {
            public static void Postfix(Building __instance, Vector2 tileLocation, Farmer who, ref bool __result)
            {
                if (who.ActiveObject != null && who.ActiveObject.IsFloorPathItem() && who.currentLocation != null && !who.currentLocation.terrainFeatures.ContainsKey(tileLocation))
                {
                    return;
                }

                if (__instance.buildingType.Value == "FlashShifter.StardewValleyExpandedCP_PremiumBarn" && __instance.daysOfConstructionLeft.Value <= 0)
                {
                    var interior = __instance.GetIndoors();
                    if (tileLocation.X == __instance.tileX.Value + __instance.humanDoor.X + 8 &&
                        tileLocation.Y == __instance.tileY.Value + __instance.humanDoor.Y &&
                        interior != null)
                    {
                        if (who.mount != null)
                        {
                            Game1.showRedMessage(Game1.content.LoadString("Strings\\Buildings:DismountBeforeEntering"));
                            __result = false;
                            return;
                        }
                        if (who.team.demolishLock.IsLocked())
                        {
                            Game1.showRedMessage(Game1.content.LoadString("Strings\\Buildings:CantEnter"));
                            __result = false;
                            return;
                        }
                        if (__instance.OnUseHumanDoor(who))
                        {
                            who.currentLocation.playSound("doorClose", tileLocation);
                            bool isStructure = __instance.indoors.Value != null;
                            Game1.warpFarmer(interior.NameOrUniqueName, interior.warps[1].X, interior.warps[1].Y - 1, Game1.player.FacingDirection, isStructure);
                        }

                        __result = true;
                        return;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Building), nameof(Building.updateInteriorWarps))]
        public static class BuildingDeluxeBarnWarpPatch
        {
            public static void Postfix(Building __instance, GameLocation interior)
            {
                if (__instance.buildingType.Value != "FlashShifter.StardewValleyExpandedCP_PremiumBarn")
                    return;
                if (interior == null || interior.warps.Count == 0)
                    return;

                var w = interior.warps[1];
                interior.warps[1] = new(w.X, w.Y, w.TargetName, w.TargetX + 8, w.TargetY, w.flipFarmer.Value, w.npcOnly.Value);
            }
        }
        [HarmonyPatch(typeof(Utility), "_HasBuildingOrUpgrade")]
        public static class UtilityHasCoopBarnPatch
        {
            public static void Postfix(GameLocation location, string buildingId, ref bool __result)
            {
                string toCheck = null;
                if (buildingId == "Coop" || buildingId == "Deluxe Coop" || buildingId == "Big Coop")
                {
                    toCheck = "FlashShifter.StardewValleyExpandedCP_PremiumCoop";
                }
                else if (buildingId == "Barn" || buildingId == "Deluxe Barn" || buildingId == "Big Barn")
                {
                    toCheck = "FlashShifter.StardewValleyExpandedCP_PremiumBarn";
                }

                if (!__result && toCheck != null)
                {
                    if (location.getNumberBuildingsConstructed(toCheck) > 0)
                    {
                        __result = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.OutputMachine))]
        public static class ObjectWineryFasterPatch
        {
            public static void Postfix(StardewValley.Object __instance, GameLocation location, bool probe )
            {
                if (probe)
                    return;
                if (__instance.QualifiedItemId != "(BC)163")
                    return;
                if (location.Name != "FlashShifter.StardewValleyExpandedCP_Winery")
                    return;

                //__instance.MinutesUntilReady = Math.Max( 1, (int)((__instance.MinutesUntilReady / 10) * 0.85f) ) * 10;
                if (__instance is Cask cask)
                    cask.agingRate.Value /= 0.85f;
            }
        }


        [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.performUseAction))]
        public static class ObjectSunWindTotemPatch
        {
            public static bool Prefix(StardewValley.Object __instance, GameLocation location, ref bool __result)
            {
                if (__instance.QualifiedItemId != "(O)FlashShifter.StardewValleyExpandedCP_Sun_Totem" &&
                     __instance.QualifiedItemId != "(O)FlashShifter.StardewValleyExpandedCP_Wind_Totem")
                {
                    return true;
                }

                if (!Game1.player.canMove || __instance.isTemporarilyInvisible)
                {
                    __result = false;
                    return false;
                }
                bool normal_gameplay = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value && !Game1.player.onBridge.Value;
                if (normal_gameplay)
                {
                    Game1.playSound("warrior");
                    switch (__instance.QualifiedItemId)
                    {
                        case "(O)FlashShifter.StardewValleyExpandedCP_Sun_Totem":
                            __result = weatherTotem(__instance, Game1.player, "Sun", I18n.SunTotemMessage());
                            break;
                        case "(O)FlashShifter.StardewValleyExpandedCP_Wind_Totem":
                            __result = weatherTotem(__instance, Game1.player, "Wind", I18n.WindTotemMessage());
                            break;

                    }
                    return false;
                }

                __result = false;
                return false;
            }

            private static bool weatherTotem(StardewValley.Object obj, Farmer who, string weather, string msg)
            {
                GameLocation location = who.currentLocation;
                string contextId = location.GetLocationContextId();
                LocationContextData context = location.GetLocationContext();
                if (!context.AllowRainTotem)
                {
                    Game1.showRedMessageUsingLoadString("Strings\\UI:Item_CantBeUsedHere");
                    return false;
                }
                if (obj.ItemId == "FlashShifter.StardewValleyExpandedCP_Wind_Totem" && Game1.season == Season.Winter)
                {
                    Game1.showRedMessage(I18n.CantUseNow());
                    return false;
                }
                if (context.RainTotemAffectsContext != null)
                {
                    contextId = context.RainTotemAffectsContext;
                }
                bool applied = false;
                if (contextId == "Default")
                {
                    if (!Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.season))
                    {
                        Game1.netWorldState.Value.WeatherForTomorrow = (Game1.weatherForTomorrow = weather);
                        applied = true;
                    }
                }
                else
                {
                    location.GetWeather().WeatherForTomorrow = weather;
                    applied = true;
                }
                if (applied)
                {
                    Game1.pauseThenMessage(2000, msg);
                }
                Game1.screenGlow = false;
                //location.playSound("thunder");
                who.canMove = false;
                Game1.screenGlowOnce(Color.SlateBlue, hold: false);
                Game1.player.faceDirection(2);
                Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[1]
                {
                new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false, Farmer.canMoveNow, behaviorAtEndOfFrame: true)
                });
                /*
                for (int i = 0; i < 6; i++)
                {
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 2f, 0.01f, 0f, 0f)
                    {
                        motion = new Vector2((float)Game1.random.Next(-10, 11) / 10f, -2f),
                        delayBeforeAnimationStart = i * 200
                    });
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f)
                    {
                        motion = new Vector2((float)Game1.random.Next(-30, -10) / 10f, -1f),
                        delayBeforeAnimationStart = 100 + i * 200
                    });
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f)
                    {
                        motion = new Vector2((float)Game1.random.Next(10, 30) / 10f, -1f),
                        delayBeforeAnimationStart = 200 + i * 200
                    });
                }
                */
                TemporaryAnimatedSprite sprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
                {
                    motion = new Vector2(0f, -7f),
                    acceleration = new Vector2(0f, 0.1f),
                    scaleChange = 0.015f,
                    alpha = 1f,
                    alphaFade = 0.0075f,
                    shakeIntensity = 1f,
                    initialPosition = Game1.player.Position + new Vector2(0f, -96f),
                    xPeriodic = true,
                    xPeriodicLoopTime = 1000f,
                    xPeriodicRange = 4f,
                    layerDepth = 1f
                };
                sprite.CopyAppearanceFromItemId(obj.QualifiedItemId);
                Game1.Multiplayer.broadcastSprites(location, sprite);
                DelayedAction.playSoundAfterDelay("rainsound", 2000);

                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Furniture), nameof(Furniture.checkForAction))]
    public static class FurnitureCatalogue2Patch
    {
        public static void Postfix(Furniture __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
        {
            if (__instance.QualifiedItemId == "(F)FlashShifter.StardewValleyExpandedCP_Furniture_Catalogue_2")
            {
                if (!justCheckingForActivity)
                {
                    Utility.TryOpenShopMenu("FlashShifter.StardewValleyExpandedCP_FurnitureCatalogue2", __instance.Location);
                }
            }
        }
    }


    [HarmonyPatch(typeof(Furniture), nameof(Furniture.getDescription))]
    public static class FurnitureDescriptionExtensionPatch
    {
        public static void Postfix(Furniture __instance, ref string __result)
        {
            if (__instance.ItemId == "FlashShifter.StardewValleyExpandedCP_Furniture_Catalogue_2")
            {
                __result = Game1.parseText(I18n.FurnitureCatalogue2Description(), Game1.smallFont, 320);
            }
        }
    }


    [HarmonyPatch(typeof(Building), nameof(Building.InitializeIndoor))]
    public static class BuildingAutoGrabberFix
    {
        public static void Postfix(Building __instance, BuildingData data, bool forConstruction, bool forUpgrade)
        {
            if (!forConstruction)
                return;
            if (__instance.buildingType.Value != "FlashShifter.StardewValleyExpandedCP_PremiumCoop" &&
                 __instance.buildingType.Value != "FlashShifter.StardewValleyExpandedCP_PremiumBarn")
                return;

            foreach (var obj in __instance.indoors.Value.Objects.Values)
            {
                if (obj.QualifiedItemId == "(BC)165" && obj.heldObject.Value == null)
                {
                    obj.heldObject.Value = new Chest();
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameLocation), "resetSharedState")]
    public static class GameLocationFixDoorsWhenSleepingInSameLocationPatch
    {
        public static void Postfix(GameLocation __instance)
        {
            __instance.interiorDoors.ResetSharedState();
        }
    }
}