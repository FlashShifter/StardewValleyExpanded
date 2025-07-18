using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;

namespace SpaceShared
{
    internal class SpaceUtility
    {
        public static void iterateAllTerrainFeatures(Func<TerrainFeature, TerrainFeature> action)
        {
            foreach (GameLocation location in Game1.locations)
            {
                SpaceUtility._recursiveIterateLocation(location, action);
            }
        }

        protected static void _recursiveIterateLocation(GameLocation l, Func<TerrainFeature, TerrainFeature> action)
        {
            foreach (Building b in l.buildings)
            {
                if (b.indoors.Value != null)
                {
                    SpaceUtility._recursiveIterateLocation(b.indoors.Value, action);
                }
            }

            foreach (var key in l.objects.Keys)
            {
                var obj = l.objects[key];
                if (obj is IndoorPot pot)
                {
                    pot.hoeDirt.Value = (HoeDirt)action(pot.hoeDirt.Value);
                }
            }

            var toRemove = new List<Vector2>();
            foreach (var key in l.terrainFeatures.Keys)
            {
                var ret = action(l.terrainFeatures[key]);
                if (ret == null)
                    toRemove.Add(key);
                else if (l.terrainFeatures[key] != ret)
                    l.terrainFeatures[key] = ret;
            }
            foreach (var r in toRemove)
                l.terrainFeatures.Remove(r);

            for (int i = l.resourceClumps.Count - 1; i >= 0; --i)
            {
                var ret = (ResourceClump)action(l.resourceClumps[i]);
                if (ret == null)
                    l.resourceClumps.RemoveAt(i);
                else
                    l.resourceClumps[i] = ret;
            }
        }

        public static void iterateAllItems(Func<Item, Item> action)
        {
            foreach (GameLocation location in Game1.locations)
            {
                SpaceUtility._recursiveIterateLocation(location, action);
            }
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                IList<Item> list = farmer.Items;
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i] = SpaceUtility._recursiveIterateItem(list[i], action);
                }
                //farmer.Items = list;
                farmer.shirtItem.Value = (Clothing)SpaceUtility._recursiveIterateItem(farmer.shirtItem.Value, action);
                farmer.pantsItem.Value = (Clothing)SpaceUtility._recursiveIterateItem(farmer.pantsItem.Value, action);
                farmer.boots.Value = (Boots)SpaceUtility._recursiveIterateItem(farmer.boots.Value, action);
                farmer.hat.Value = (Hat)SpaceUtility._recursiveIterateItem(farmer.hat.Value, action);
                farmer.leftRing.Value = (Ring)SpaceUtility._recursiveIterateItem(farmer.leftRing.Value, action);
                farmer.rightRing.Value = (Ring)SpaceUtility._recursiveIterateItem(farmer.rightRing.Value, action);
                list = farmer.itemsLostLastDeath;
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    list[i] = SpaceUtility._recursiveIterateItem(list[i], action);
                    if (list[i] == null)
                        list.RemoveAt(i);
                }
                //farmer.itemsLostLastDeath.CopyFrom( list );
            }
            IList<Item> list2 = Game1.player.team.returnedDonations;
            for (int i = list2.Count - 1; i >= 0; --i)
            {
                if (list2[i] != null)
                {
                    list2[i] = action(list2[i]);
                    if (list2[i] == null)
                        list2.RemoveAt(i);
                }
            }
            //Game1.player.team.returnedDonations.Set( list2 );
            IEnumerable<Inventory> list3 = Game1.player.team.globalInventories.Values;
            foreach (var inv in list3)
            {
                for (int i = inv.Count - 1; i >= 0; --i)
                {
                    if (inv[i] != null)
                    {
                        inv[i] = action(inv[i]);
                        if (inv[i] == null)
                            inv.RemoveAt(i);
                    }
                }
            }
            //Game1.player.team.junimoChest.CopyFrom( list2 );
            foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
            {
                list2 = specialOrder.donatedItems;
                for (int i = list2.Count - 1; i >= 0; --i)
                {
                    if (list2[i] != null)
                    {
                        list2[i] = action(list2[i]);
                        if (list2[i] == null)
                            list2.RemoveAt(i);
                    }
                }
                //specialOrder.donatedItems.CopyFrom( list2 );
            }
        }

        protected static void _recursiveIterateLocation(GameLocation l, Func<Item, Item> action)
        {
            if (l == null)
            {
                return;
            }
            {
                IList<Furniture> list = l.furniture;
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    // this one acts funny when returning null
                    var v = (Furniture)SpaceUtility._recursiveIterateItem(list[i], action);
                    if (v == null)
                        list.RemoveAt(i);
                    else
                        list[i] = v;
                }

            }
            if (l is IslandFarmHouse)
            {
                IList<Item> list = (l as IslandFarmHouse).fridge.Value.Items;
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i] != null)
                    {
                        list[i] = SpaceUtility._recursiveIterateItem(list[i], action);
                    }
                }
                (l as IslandFarmHouse).fridge.Value.clearNulls();
            }
            if (l is FarmHouse)
            {
                IList<Item> list = (l as FarmHouse).fridge.Value.Items;
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i] != null)
                    {
                        list[i] = SpaceUtility._recursiveIterateItem(list[i], action);
                    }
                    (l as FarmHouse).fridge.Value.clearNulls();
                }
            }
            foreach (NPC character in l.characters)
            {
                if (character is Child && (character as Child).hat.Value != null)
                {
                    (character as Child).hat.Value = (Hat)SpaceUtility._recursiveIterateItem((character as Child).hat.Value, action);
                }
                if (character is Horse && (character as Horse).hat.Value != null)
                {
                    (character as Horse).hat.Value = (Hat)SpaceUtility._recursiveIterateItem((character as Horse).hat.Value, action);
                }
            }
            foreach (Building b in l.buildings)
            {
                if (b.indoors.Value != null)
                {
                    SpaceUtility._recursiveIterateLocation(b.indoors.Value, action);
                }
                if (b is JunimoHut)
                {
                    IList<Item> list = (b as JunimoHut).GetOutputChest().Items;
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (list[i] != null)
                        {
                            list[i] = SpaceUtility._recursiveIterateItem(list[i], action);
                        }
                    }
                }
                else
                {
                    foreach (var chest in b.buildingChests)
                    {
                        IList<Item> list = chest.Items;
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (list[i] != null)
                            {
                                list[i] = SpaceUtility._recursiveIterateItem(list[i], action);
                            }
                        }
                    }
                }
            }
            var toRemove = new List<Vector2>();
            foreach (var key in l.objects.Keys)
            {
                var ret = (StardewValley.Object)SpaceUtility._recursiveIterateItem(l.objects[key], action);
                if (ret == null)
                    toRemove.Add(key);
                else
                    l.objects[key] = ret;
            }
            foreach (var r in toRemove)
                l.objects.Remove(r);

            var toRemove2 = new List<Debris>();
            foreach (Debris d in l.debris)
            {
                if (d.item != null)
                {
                    d.item = SpaceUtility._recursiveIterateItem(d.item, action);
                    if (d.item == null)
                        toRemove2.Add(d);
                }
            }
            foreach (var r in toRemove2)
                l.debris.Remove(r);
        }

        private static Item _recursiveIterateItem(Item i, Func<Item, Item> action)
        {
            if (i == null)
            {
                return null;
            }
            if (i is StardewValley.Object)
            {
                StardewValley.Object o = i as StardewValley.Object;
                if (o is StorageFurniture)
                {
                    IList<Item> list = (o as StorageFurniture).heldItems;
                    for (int ii = 0; ii < list.Count; ++ii)
                    {
                        if (list[ii] != null)
                        {
                            list[ii] = SpaceUtility._recursiveIterateItem(list[ii], action);
                        }
                    }
                }
                if (o is Chest)
                {
                    IList<Item> list = (o as Chest).Items;
                    for (int ii = 0; ii < list.Count; ++ii)
                    {
                        if (list[ii] != null)
                        {
                            list[ii] = SpaceUtility._recursiveIterateItem(list[ii], action);
                        }
                    }
                    (o as Chest).clearNulls();
                }
                if (o.heldObject.Value != null)
                {
                    o.heldObject.Value = (StardewValley.Object)SpaceUtility._recursiveIterateItem(o.heldObject.Value, action);
                }
            }
            if (i is Tool t)
            {
                IList<StardewValley.Object> list = t.attachments;
                for (int ii = 0; ii < list.Count; ++ii)
                {
                    if (list[ii] != null)
                    {
                        list[ii] = (StardewValley.Object)SpaceUtility._recursiveIterateItem(list[ii], action);
                    }
                }
            }
            return action(i);
        }
    }
}
