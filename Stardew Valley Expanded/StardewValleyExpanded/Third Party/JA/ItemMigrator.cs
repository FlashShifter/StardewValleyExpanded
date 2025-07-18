using Netcode;
using Newtonsoft.Json;
using SpaceShared;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SObject = StardewValley.Object;

namespace JsonAssets
{
    public class ItemMigrator
    {
        // int-ID-string to new ID
        private Dictionary<string, string> OldObjectIds = new();
        private Dictionary<string, string> OldCropIds = new();
        private Dictionary<string, string> OldFruitTreeIds = new();
        private Dictionary<string, string> OldBigCraftableIds = new();
        private Dictionary<string, string> OldHatIds = new();
        private Dictionary<string, string> OldWeaponIds = new();
        private Dictionary<string, string> OldClothingIds = new();
        private Dictionary<string, string> OldBootsIds = new();

        private IReflectionHelper Reflection;

        public ItemMigrator(string[] mappings, IReflectionHelper reflection)
        {
            Reflection = reflection;

            Dictionary<string, string> toNewObjects = new();
            Dictionary<string, string> toNewCrops = new();
            Dictionary<string, string> toNewFruitTrees = new();
            Dictionary<string, string> toNewBigCraftables = new();
            Dictionary<string, string> toNewHats = new();
            Dictionary<string, string> toNewWeapons = new();
            Dictionary<string, string> toNewClothing = new();
            Dictionary<string, string> toNewBoots = new();
            foreach (var str in mappings)
            {
                string[] toks = str.Split(':', '=');
                if (toks.Length != 3)
                    continue;

                Dictionary<string, string> dict = null;
                switch (toks[0])
                {
                    case "Object": dict = toNewObjects; break;
                    case "Crop": dict = toNewCrops; break;
                    case "FruitTree": dict = toNewFruitTrees; break;
                    case "BigCraftable": dict = toNewBigCraftables; break;
                    case "Hat": dict = toNewHats; break;
                    case "Weapon": dict = toNewWeapons; break;
                    case "Clothing": dict = toNewClothing; break;
                    case "Boots": dict = toNewBoots; break;
                }
                dict?.Add(toks[1], toks[2]);
            }

            IDictionary<TKey, TValue> LoadDictionary<TKey, TValue>(string filename)
            {
                string path = Path.Combine(Constants.CurrentSavePath, "JsonAssets", filename);
                return File.Exists(path)
                    ? JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(File.ReadAllText(path))
                    : new Dictionary<TKey, TValue>();
            }
            var oldObjectIds = (LoadDictionary<string, int>("ids-objects.json") ?? new Dictionary<string, int>());
            var oldCropIds = (LoadDictionary<string, int>("ids-crops.json") ?? new Dictionary<string, int>());
            var oldFruitTreeIds = (LoadDictionary<string, int>("ids-fruittrees.json") ?? new Dictionary<string, int>());
            var oldBigCraftableIds = (LoadDictionary<string, int>("ids-big-craftables.json") ?? new Dictionary<string, int>());
            var oldHatIds = (LoadDictionary<string, int>("ids-hats.json") ?? new Dictionary<string, int>());
            var oldWeaponIds = (LoadDictionary<string, int>("ids-weapons.json") ?? new Dictionary<string, int>());
            var oldClothingIds = (LoadDictionary<string, int>("ids-clothing.json") ?? new Dictionary<string, int>());
            var oldBootsIds = (LoadDictionary<string, int>("ids-boots.json") ?? new Dictionary<string, int>());

            foreach ((string jaName, string strId) in toNewObjects)
            {
                if (oldObjectIds.TryGetValue(jaName, out var id))
                    this.OldObjectIds.Add(id.ToString(), strId);
            }
            foreach ((string jaName, string strId) in toNewCrops)
            {
                if (oldCropIds.TryGetValue(jaName, out var id))
                    this.OldCropIds.Add(id.ToString(), strId);
            }
            foreach ((string jaName, string strId) in toNewFruitTrees)
            {
                if (oldFruitTreeIds.TryGetValue(jaName, out var id))
                    this.OldFruitTreeIds.Add(id.ToString(), strId);
            }
            foreach ((string jaName, string strId) in toNewBigCraftables)
            {
                if (oldBigCraftableIds.TryGetValue(jaName, out var id))
                    this.OldBigCraftableIds.Add(id.ToString(), strId);
            }
            foreach ((string jaName, string strId) in toNewHats)
            {
                if (oldHatIds.TryGetValue(jaName, out var id))
                    this.OldHatIds.Add(id.ToString(), strId);
            }
            foreach ((string jaName, string strId) in toNewWeapons)
            {
                if (oldWeaponIds.TryGetValue(jaName, out var id))
                    this.OldWeaponIds.Add(id.ToString(), strId);
            }
            foreach ((string jaName, string strId) in toNewClothing)
            {
                if (oldClothingIds.TryGetValue(jaName, out var id))
                    this.OldClothingIds.Add(id.ToString(), strId);
            }
            foreach ((string jaName, string strId) in toNewBoots)
            {
                if (oldBootsIds.TryGetValue(jaName, out var id))
                    this.OldBootsIds.Add(id.ToString(), strId);
            }
        }

        public void Migrate()
        {
            SpaceUtility.iterateAllItems(this.FixItem);
            SpaceUtility.iterateAllTerrainFeatures(this.FixTerrainFeature);
            foreach (var loc in Game1.locations)
            {
                foreach (var building in loc.buildings)
                    FixBuilding(building);
            }

            this.FixIdDict(Game1.player.basicShipped, removeUnshippable: true);
            this.FixIdDict(Game1.player.mineralsFound);
            this.FixIdDict(Game1.player.recipesCooked);
            this.FixIdDict2(Game1.player.archaeologyFound);
            this.FixIdDict2(Game1.player.fishCaught);
        }

        internal Item FixItem(Item item)
        {
            switch (item)
            {
                case Hat hat:
                    if (hat.obsolete_which.HasValue && this.OldHatIds.ContainsKey(hat.obsolete_which.Value.ToString()))
                        hat.ItemId = this.OldHatIds[hat.obsolete_which.Value.ToString()];
                    break;

                case MeleeWeapon weapon:
                    if (this.OldWeaponIds.ContainsKey(weapon.ItemId))
                        weapon.ItemId = this.OldWeaponIds[weapon.ItemId];
                    if (weapon.appearance.Value != null && this.OldWeaponIds.ContainsKey(weapon.appearance.Value))
                        weapon.appearance.Value = this.OldWeaponIds[weapon.appearance.Value];
                    break;

                case Ring ring:
                    if (this.OldObjectIds.ContainsKey(ring.ItemId))
                        ring.ItemId = this.OldObjectIds[ring.ItemId];

                    if (ring is CombinedRing combinedRing)
                    {
                        for (int i = combinedRing.combinedRings.Count - 1; i >= 0; i--)
                        {
                            combinedRing.combinedRings[i] = FixItem(combinedRing.combinedRings[i]) as Ring;
                        }
                    }
                    break;

                case Clothing clothing:
                    if (this.OldClothingIds.ContainsKey(clothing.ItemId))
                        clothing.ItemId = this.OldClothingIds[clothing.ItemId];
                    break;

                case Boots boots:
                    if (this.OldBootsIds.ContainsKey(boots.ItemId))
                        boots.ItemId = this.OldBootsIds[boots.ItemId];
                    // TODO: what to do about tailored boots...
                    break;

                case SObject obj:
                    if (obj is Chest chest)
                    {
                        if (this.OldBigCraftableIds.ContainsKey(chest.ItemId))
                            chest.ItemId = this.OldBigCraftableIds[chest.ItemId];
                        else
                            chest.startingLidFrame.Value = chest.ParentSheetIndex + 1;
                        this.FixItemList(chest.Items);
                    }
                    else if (obj is IndoorPot pot)
                    {
                        if (pot.hoeDirt.Value != null && pot.hoeDirt.Value.crop != null)
                            this.FixCrop(pot.hoeDirt.Value.crop);
                    }
                    else if (obj is Fence fence)
                    {
                        // TODO: Do this once custom fences are in
                    }
                    else if (obj.GetType() == typeof(SObject) || obj.GetType() == typeof(Cask))
                    {
                        if (!obj.bigCraftable.Value)
                        {
                            /*
                            if (this.FixId(this.OldObjectIds, this.ObjectIds, obj.preservedParentSheetIndex, this.VanillaObjectIds))
                                obj.preservedParentSheetIndex.Value = -1;
                            */
                            if (this.OldObjectIds.ContainsKey(obj.ItemId))
                                obj.ItemId = this.OldObjectIds[obj.ItemId];
                        }
                        else
                        {
                            if (this.OldBigCraftableIds.ContainsKey(obj.ItemId))
                                obj.ItemId = this.OldBigCraftableIds[obj.ItemId];
                        }
                    }

                    if (obj.heldObject.Value != null)
                    {
                        if (this.OldObjectIds.ContainsKey(obj.heldObject.Value.ItemId))
                            obj.heldObject.Value.ItemId = this.OldObjectIds[obj.heldObject.Value.ItemId].Replace(' ', '_');

                        if (obj.heldObject.Value is Chest innerChest)
                            this.FixItemList(innerChest.Items);
                    }
                    break;
            }

            item?.ResetParentSheetIndex();
            return item;
        }
        private void FixBuilding(Building building)
        {
            if (building is null)
                return;

            switch (building)
            {
                default:
                    foreach (var chest in building.buildingChests.ToList())
                        this.FixItemList(chest.Items);
                    break;

                case FishPond pond:
                    if (pond.fishType.Value == "-1")
                    {
                        Reflection.GetField<SObject>(pond, "_fishObject").SetValue(null);
                        break;
                    }

                    if (pond.fishType.Value != null && this.OldObjectIds.ContainsKey(pond.fishType.Value))
                        pond.fishType.Value = this.OldObjectIds[pond.fishType.Value];
                    pond.sign.Value = FixItem(pond.sign.Value) as SObject;
                    pond.output.Value = FixItem(pond.output.Value);
                    pond.neededItem.Value = FixItem(pond.neededItem.Value) as SObject;
                    break;
            }
        }
        private void FixCrop(Crop crop)
        {
            if (crop is null || crop.indexOfHarvest.Value == null)
                return;

            if (this.OldObjectIds.ContainsKey(crop.indexOfHarvest.Value))
                crop.indexOfHarvest.Value = this.OldObjectIds[crop.indexOfHarvest.Value];
            if (crop.netSeedIndex.Value != null && this.OldObjectIds.ContainsKey(crop.netSeedIndex.Value))
                crop.netSeedIndex.Value = this.OldObjectIds[crop.netSeedIndex.Value];
            if (crop.netSeedIndex.Value == null)
            {
                foreach ( var data in Game1.cropData )
                {
                    if (data.Value.HarvestItemId == crop.indexOfHarvest.Value)
                    {
                        crop.netSeedIndex.Value = data.Key;
                        break;
                    }
                }
            }

            if (this.OldCropIds.ContainsKey(crop.rowInSpriteSheet.Value.ToString()))
            {
                if (Crop.TryGetData(crop.netSeedIndex.Value, out var data))
                {
                    crop.rowInSpriteSheet.Value = data.SpriteIndex;
                    crop.overrideTexturePath.Value = data.GetCustomTextureName("TileSheets\\crops");
                }
            }
        }
        private TerrainFeature FixTerrainFeature(TerrainFeature feature)
        {
            switch (feature)
            {
                case HoeDirt dirt:
                    this.FixCrop(dirt.crop);
                    break;

                case FruitTree ftree:
                    {
                        if (this.OldFruitTreeIds.ContainsKey(ftree.treeId.Value))
                        {
                            ftree.treeId.Value = this.OldFruitTreeIds[ftree.treeId.Value];
                        }
                    }
                    break;

                    // TODO: How to do in 1.6?
                    /*
                case ResourceClump rclump:
                    if ( this.OldObjectIds.ContainsKey( rclump.parentSheetIndex.Value.ToString() ) )
                    {
                        rclump.ItemId = this.OldObjectIds[rclump.parentSheetIndex.Value.ToString()].FixIdJA();
                        rclump.parentSheetIndex.Value = 1720;
                    }
                    */

                    break;
            }

            return feature;
        }
        internal void FixItemList(IList<Item> items)
        {
            if (items is null)
                return;

            for (int i = 0; i < items.Count; ++i)
            {
                items[i] = FixItem(items[i]);
                var item = items[i];
                if (item == null)
                    continue;
            }
        }

        private void FixIdDict(NetStringDictionary<int, NetInt> dict, bool removeUnshippable = false)
        {
            var toRemove = new List<string>();
            var toAdd = new Dictionary<string, int>();
            foreach (string entry in dict.Keys)
            {
                if (this.OldObjectIds.ContainsKey(entry))
                {
                    toRemove.Add(entry);
                    toAdd.Add(this.OldObjectIds[entry], dict[entry]);
                }
            }
            foreach (string entry in toRemove)
                dict.Remove(entry);
            foreach (var entry in toAdd)
            {
                if (dict.ContainsKey(entry.Key))
                {
                    continue;
                    //Log.Error("Dict already has value for " + entry.Key + "!");
                }
                dict.Add(entry.Key, entry.Value);
            }
        }

        private void FixIdDict2(NetStringIntArrayDictionary dict)
        {
            var toRemove = new List<string>();
            var toAdd = new Dictionary<string, int[]>();
            foreach (string entry in dict.Keys)
            {
                if (this.OldObjectIds.ContainsKey(entry))
                {
                    toRemove.Add(entry);
                    toAdd.Add(this.OldObjectIds[entry], dict[entry]);
                }
            }
            foreach (string entry in toRemove)
                dict.Remove(entry);
            foreach (var entry in toAdd)
                dict.Add(entry.Key, entry.Value);
        }
    }
}
