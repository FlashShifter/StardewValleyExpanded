using System;
using StardewModdingAPI;
using StardewValley;
using HarmonyLib;
using System.Collections.Generic;
using StardewValley.Locations;
using System.Reflection.Emit;
using System.Reflection;

namespace StardewValleyExpanded
{
    /// <summary>Modifies the "arrive on bus" logic when warping to the desert, allowing Sandy's shop to be moved without provoking the bus.</summary>
    public static class HarmonyPatch_FixDesertBusWarp
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
                Monitor = monitor; //store monitor

                Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_FixDesertBusWarp)}\": transpiling SDV method \"Desert.resetLocalState()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Desert), "resetLocalState", new Type[] { }),
                    transpiler: new HarmonyMethod(typeof(HarmonyPatch_FixDesertBusWarp), nameof(Desert_resetLocalState))
                );

                Applied = true;
            }
        }

        /// <summary>Modify the desert's range of tiles where the player will arrive via bus.</summary>
        /// <remarks>
        /// Old C#:
        ///     if (Game1.player.getTileY() > 40 || Game1.player.getTileY() < 10)
        ///     
        /// New C#:
        ///     if (Game1.player.getTileY() > 40 || Game1.player.getTileY() < 20)
        ///     
        /// Old CIL:
        /// 	IL_004c: callvirt instance int32 StardewValley.Character::getTileY()
        ///     IL_0051: ldc.i4.s 10
	    ///     IL_0053: bge IL_013a
        ///     
        ///New CIL:
        /// 	IL_004c: callvirt instance int32 StardewValley.Character::getTileY()
        ///     IL_0051: ldc.i4.s 20
	    ///     IL_0053: bge IL_013a
        /// </remarks>
        /// <param name="instructions">The original method's CIL code.</param>
        private static IEnumerable<CodeInstruction> Desert_resetLocalState(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                List<CodeInstruction> patched = new List<CodeInstruction>(instructions); //make a copy of the instructions to modify

                MethodInfo getTileY = AccessTools.Method(typeof(Character), nameof(Character.TilePoint)); //get info for the "Character.getTileY()" method

                for (int x = patched.Count - 1; x >= 3; x--) //for each instruction (looping backward)
                {
                    // X coordinate
                    if (patched[x].opcode == OpCodes.Bne_Un
                        && patched[x-1].operand is sbyte numberX && numberX == 16
                        && patched[x-3].operand is MethodInfo methodX && methodX.Equals(getTileY))
                    {
                        patched[x - 1] = new CodeInstruction(patched[x - 1].opcode, (sbyte)18);
                    }
                    // Y coordinate
                    else if (patched[x].opcode == OpCodes.Bne_Un
                        && patched[x - 1].operand is sbyte numberY && numberY == 24
                        && patched[x - 3].operand is MethodInfo methodY && methodY.Equals(getTileY))
                    {
                        patched[x - 1] = new CodeInstruction(patched[x - 1].opcode, (sbyte)27);
                    }
                }

                return patched; //return the patched instructions
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_FixDesertBusWarp)}\" has encountered an error. Transpiler \"{nameof(Desert_resetLocalState)}\" will not be applied. Full error message:\n{ex.ToString()}", LogLevel.Error);
                return instructions; //return the original instructions
            }
        }
    }
}
