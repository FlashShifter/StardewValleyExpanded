using System;
using StardewModdingAPI;
using StardewValley;
using HarmonyLib;
using System.Collections.Generic;
using StardewValley.Locations;
using Netcode;
using System.Reflection.Emit;
using System.Reflection;

namespace StardewValleyExpanded
{
    /// <summary>Replaces community shortcut warps at the <see cref="Beach"/> and <see cref="Forest"/> with TouchAction tile property equivalents. Prevents any related custom NPC pathing errors.</summary>
    /// <remarks>If the mod TMXLoader is available, this will use "LoadMap" tile properties instead of "MagicWarp". This removes unnecessary magic effects and longer warp delays.</remarks>
    public static class HarmonyPatch_FixCommunityShortcuts
    {
        /// <summary>True if this patch is currently applied.</summary>
        public static bool Applied { get; private set; } = false;

        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Applies this Harmony patch to the game.</summary>
        /// <param name="harmony">The <see cref="Harmony"/> created with this mod's ID.</param>
        /// <param name="helper">The <see cref="IModHelper"/> provided to this mod by SMAPI. Used for events and other API access.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided to this mod by SMAPI. Used for log messages.</param>
        public static void ApplyPatch(Harmony harmony, IModHelper helper, IMonitor monitor)
        {
            if (!Applied && helper != null && monitor != null) //if NOT already applied AND valid tools were provided
            {
                Helper = helper; //store helper
                Monitor = monitor; //store monitor

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FixCommunityShortcuts)}\": transpiling SDV method \"Forest.showCommunityUpgradeShortcuts()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Forest), "showCommunityUpgradeShortcuts"),
                    transpiler: new HarmonyMethod(typeof(HarmonyPatch_FixCommunityShortcuts), nameof(ReplaceNewWarpsWithTouchActions))
                );

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FixCommunityShortcuts)}\": transpiling SDV method \"Beach.showCommunityUpgradeShortcuts(GameLocation, bool)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Beach), "showCommunityUpgradeShortcuts"),
                    transpiler: new HarmonyMethod(typeof(HarmonyPatch_FixCommunityShortcuts), nameof(ReplaceNewWarpsWithTouchActions))
                );

                Applied = true;
            }
        }


        /****************/
        /* Mod Settings */
        /****************/


        /// <summary>If true, this patch will remove the shortcuts, but will NOT add TouchAction replacements. Preferable for redesigned maps.</summary>
        public static bool RemoveWarpsWithoutReplacing { get; set; } = true;


        /***********************/
        /* Internal code below */
        /***********************/


        /// <summary>Replaces any "warps.Add(new Warp)" calls with a method that creates TouchAction warps instead.</summary>
        /// <param name="instructions">The original method's CIL code.</param>
        private static IEnumerable<CodeInstruction> ReplaceNewWarpsWithTouchActions(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                List<CodeInstruction> patched = new List<CodeInstruction>(instructions); //make a copy of the instructions to modify

                MethodInfo replacementMethod = AccessTools.Method(typeof(HarmonyPatch_FixCommunityShortcuts), nameof(HarmonyPatch_FixCommunityShortcuts.AddWarpAsTouchAction));

                for (int x = patched.Count - 1; x >= 1; x--) //for each instruction (looping backward, skipping the first)
                {
                    if (patched[x-1].opcode == OpCodes.Newobj //if the previous instruction creates a new object
                        && patched[x-1].operand is ConstructorInfo conInfo && conInfo.DeclaringType == typeof(Warp) //and the new object is a warp
                        && patched[x].opcode == OpCodes.Callvirt //and the current instruction is a virtual method call
                        && patched[x].operand is MethodInfo methodInfo && methodInfo.DeclaringType?.IsAssignableFrom(typeof(NetObjectList<Warp>)) == true && methodInfo.Name == "Add") //and the method seems to be GameLocation.warps.Add
                    {
                        patched[x] = new CodeInstruction(OpCodes.Call, replacementMethod); //replace the call with the modified version
                        patched.Insert(x, new CodeInstruction(OpCodes.Ldarg_0, null)); //insert a "load this GameLocation instance" instruction before the call
                    }
                }

                return patched; //return the patched instructions
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FixCommunityShortcuts)}\" has encountered an error. Transpiler \"{nameof(ReplaceNewWarpsWithTouchActions)}\" will not be applied. Full error message:\n{ex.ToString()}", LogLevel.Error);
                return instructions; //return the original instructions
            }
        }

        /// <summary>Creates a TouchAction warp at the specified location and tile (clamped to the nearest map edge). Prevents NPC use of the warp.</summary>
        /// <param name="warpList">The list of warps on which this method was called, e.g. <see cref="GameLocation.warps"/>. Not actually used by this method, which is implemented this way for transpiler convenience.</param>
        /// <param name="warp">The warp to be added as a TouchAction.</param>
        /// <param name="location">The location of the warp.</param>
        private static void AddWarpAsTouchAction(this List<Warp> warpList, Warp warp, GameLocation location)
        {
            try
            {
                if (RemoveWarpsWithoutReplacing) //if the warps should be removed completely
                    return; //do nothing

                if (warp != null && location != null) //if valid warp and locations were provided
                {
                    //adjust the warp tile to be within the back layer's boundaries
                    var backLayer = location.Map.GetLayer("Back");
                    int x = Utility.Clamp(warp.X, 0, (backLayer.DisplayWidth / Game1.tileSize) - 1);
                    int y = Utility.Clamp(warp.Y, 0, (backLayer.DisplayHeight / Game1.tileSize) - 1);

                    string warpType = Helper.ModRegistry.IsLoaded("Platonymous.TMXLoader") ? "LoadMap" : "MagicWarp"; //use LoadMap if TMXL exists to implement it; otherwise, use MagicWarp
                    string warpString = $"{warpType} {warp.TargetName} {warp.TargetX} {warp.TargetY}"; //create the map property value
                    location.setTileProperty(x, y, "Back", "TouchAction", warpString); //set the tile's TouchAction property

                    Monitor.LogOnce($"Community shortcut NPC fix: Setting tile property at {location.Name} {x},{y} to \"TouchAction\", \"{warpString}\".", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FixCommunityShortcuts)}\" has encountered an error in method \"{nameof(AddWarpAsTouchAction)}\". Full error message:\n{ex.ToString()}", LogLevel.Error);
            }
        }
    }
}
