using System;
using StardewModdingAPI;
using StardewValley;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.Locations;
using StardewValley.Monsters;
using System.Diagnostics;
using StardewValley.Objects;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Events;
using StardewValley.Characters;
using xTile.Dimensions;
using Netcode;
using StardewValley.Network;
using System.Reflection.Emit;
using System.Reflection;
using xTile.ObjectModel;

namespace StardewValleyExpanded
{
    /// <summary></summary>
    public static class HarmonyPatch_MovieTheaterNPCs
    {
        /// <summary>True if this patch is currently applied.</summary>
        public static bool Applied { get; private set; } = false;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided to this mod by SMAPI. Used for log messages.</param>
        public static void ApplyPatch(Harmony harmony, IMonitor monitor)
        {
            if (!Applied && monitor != null) //if NOT already applied
            {
                //store helper and monitor
                Monitor = monitor;

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_MovieTheaterNPCs)}\": postfixing SDV method \"MovieTheater.checkAction(Location, Rectangle, Farmer)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(MovieTheater), nameof(MovieTheater.checkAction), new[] { typeof(Location), typeof(xTile.Dimensions.Rectangle), typeof(Farmer) }),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_MovieTheaterNPCs), nameof(MovieTheater_checkAction))
                );

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_MovieTheaterNPCs)}\": transpiling SDV method \"Utility.CheckForCharacterAtTile(Vector2, Farmer)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Utility), nameof(Utility.checkForCharacterInteractionAtTile), new[] { typeof(Vector2), typeof(Farmer) }),
                    transpiler: new HarmonyMethod(typeof(HarmonyPatch_MovieTheaterNPCs), nameof(Utility_CheckForCharacterAtTile))
                );

                Applied = true;
            }
        }

        /// <summary>Allows generic interactions with NPCs in <see cref="MovieTheater"/> under certain conditions.</summary>
        /// <param name="__instance">The <see cref="MovieTheater"/> instance.</param>
        /// <param name="tileLocation">The tile targeted by this action.</param>
        /// <param name="viewport">The viewport of the player performing this action.</param>
        /// <param name="who">The player performing this action.</param>
        /// <param name="____playerInvitedPatrons">The list of NPCs invited by the player. Non-public field for this <see cref="MovieTheater"/> instance.</param>
        /// <param name="____characterGroupLookup">The list of NPC patrons currently present. Non-public field for this <see cref="MovieTheater"/> instance.</param>
        /// <param name="__result">The result of the original method.</param>
        private static void MovieTheater_checkAction(MovieTheater __instance,
            Location tileLocation,
            xTile.Dimensions.Rectangle viewport,
            Farmer who,
            NetStringDictionary<int, NetInt> ____playerInvitedPatrons,
            NetStringDictionary<bool, NetBool> ____characterGroupLookup,
            ref bool __result)
        {
            try
            {
                if (__result == true) //if the original method return true
                {
                    PropertyValue action = null;
                    __instance.map.GetLayer("Buildings").PickTile(new Location(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size)?.Properties.TryGetValue("Action", out action); //get this tile's Action property if it exists
                    if (action == null) //if this tile does NOT have an Action property
                    {
                        Microsoft.Xna.Framework.Rectangle tileRect = new Microsoft.Xna.Framework.Rectangle(tileLocation.X * 64, tileLocation.Y * 64, 64, 64); //get this tile's pixel area
                        foreach (NPC npc in __instance.characters) //for each NPC in this location
                        {
                            if (npc.isVillager() && npc.GetBoundingBox().Intersects(tileRect)) //if this NPC is a villager and targeted by this action
                            {
                                string npcName = npc.Name;
                                if (____playerInvitedPatrons.ContainsKey(npcName) == false && ____characterGroupLookup.ContainsKey(npcName) == false) //if this NPC is NOT here as a patron (i.e. does not have patron-specific dialogue or behavior)
                                {
                                    __result = npc.checkAction(who, __instance); //check action on this NPC (i.e. talk/give gifts/etc) and override the result
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_MovieTheaterNPCs)}\" has encountered an error. Postfix \"{nameof(MovieTheater_checkAction)}\" might malfunction or revert to default behavior. Full error message: \n{ex.ToString()}", LogLevel.Error);
                return; //run the original method
            }
        }

        /// <summary>Disables code specific to the <see cref="MovieTheater"/> in the target method, minimizing odd cursor behavior while NPC interactions are enabled.</summary>
        private static IEnumerable<CodeInstruction> Utility_CheckForCharacterAtTile(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                List<CodeInstruction> patched = new List<CodeInstruction>(instructions); //make a copy of the instructions to modify

                for (int x = patched.Count - 1; x >= 2; x--) //for each instruction (looping backward, stopping at 2)
                {
                    if ((patched[x].opcode == OpCodes.Brfalse_S || patched[x].opcode == OpCodes.Brfalse) //if this code is a "branch if false"
                        && patched[x-1].opcode == OpCodes.Isinst && patched[x-1].operand.Equals(typeof(MovieTheater)) //AND the previous instruction checks for a theater instance
                        && patched[x-2].opcode == OpCodes.Call) //AND the previous code is a call (i.e. getting a gamelocation instance)
                    {
                        patched[x-1] = new CodeInstruction(OpCodes.Ldc_I4_0); //replace the "Isinst" with a code that outputs 0 (false)
                        patched.RemoveAt(x-2); //remove the call
                    }
                }

                return patched; //return the patched instructions
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_MovieTheaterNPCs)}\" has encountered an error. Transpiler \"{nameof(Utility_CheckForCharacterAtTile)}\" will not be applied. Full error message:\n{ex.ToString()}", LogLevel.Error);
                return instructions; //return the original instructions
            }
        }
    }
}
