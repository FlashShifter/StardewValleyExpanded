using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;

using StardewValley.Events;
using StardewValley.Locations;

using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;

namespace StardewValleyExpanded
{
    /// <summary>
    /// Harmony Patch for the <see cref="Mountain"/> location within Stardew Valley. Adjusts entity
    /// locations and corresponding events.
    /// 
    /// Changes made when <see cref="Apply(Harmony, IMonitor)"/> is called:
    /// Corrects the positioning of the Glimmering Boulder and the Landslide entity.
    /// Corrects the viewport and actor locations for the World Changing Event: Glimmering Boulder.
    /// </summary>
    public static class HarmonyPatch_Mountain
    {
        private const string BOULDER_POSITION = "boulderPosition";
        private const string LANDSLICE_POSITION = "landSlideRect";

        private static IMonitor monitor;

        private static bool patchedCommunityRoute;
        private static bool patchedJojaRoute;

        /// <summary>Gets a value indicating if the Harmony Patch was applied.</summary>
        public static bool Applied { get; private set; }

        /// <summary>
        /// Constructor for the static class <see cref="HarmonyPatch_Mountain"/>
        /// </summary>
        static HarmonyPatch_Mountain()
        {
            HarmonyPatch_Mountain.Applied = false;
            HarmonyPatch_Mountain.patchedCommunityRoute = false;
            HarmonyPatch_Mountain.patchedJojaRoute = false;
        }

        /// <summary>
        /// Applies the Harmony Patch corresponding to the <see cref="Mountain"/> location within
        /// Stardew Valley and its event(s).
        /// </summary>
        /// <param name="harmony">The <see cref="Harmony"/> instance.</param>
        /// <param name="monitor">Implementation of SMAPI's monitor and logging system.</param>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            if (HarmonyPatch_Mountain.Applied)
            {
                return;
            }

            HarmonyPatch_Mountain.monitor = monitor;

            monitor.LogOnce($"Applying Harmony Patch: {nameof(HarmonyPatch_Mountain)}");

            var parameters = new Type[] { typeof(string), typeof(string) };
            var original = AccessTools.Constructor(typeof(Mountain), parameters);
            var postfix = new HarmonyMethod(typeof(HarmonyPatch_Mountain), nameof(Postfix));

            harmony.Patch(original, null, postfix);

            var method = AccessTools.Method(typeof(WorldChangeEvent), "resetForPlayerEntry");
            var prefix = new HarmonyMethod(typeof(HarmonyPatch_Mountain), nameof(Prefix));
            var transpile = new HarmonyMethod(typeof(HarmonyPatch_Mountain), nameof(Transpile));
            harmony.Patch(method, prefix, null, transpile);

            HarmonyPatch_Mountain.Applied = true;
        }

        private static void Prefix(WorldChangeEvent __instance, ref Point targetTile)
        {
            if (__instance.whichEvent.Value == 8 || __instance.whichEvent.Value == 9)
                targetTile = new Point(48, 11);
        }

        /// <summary>
        /// Alters the bytecode of for WorldChangeEvent.setUp, correcting the viewport and actor positioning
        /// for the Glimmering Boulder event.
        /// </summary>
        /// <param name="instructions">The bytecode instructions of WorldChangeEvent.setUp.</param>
        /// <returns>The bytecode instructions with the harmony patch applied.</returns>
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var patched = new List<CodeInstruction>(instructions);

                for (int i = 0; i < patched.Count; i++)
                {
                    if (!HarmonyPatch_Mountain.patchedCommunityRoute)
                    {
                        HarmonyPatch_Mountain.PatternMatchCommunityCenterEvent(patched, i);
                    }

                    if (!HarmonyPatch_Mountain.patchedJojaRoute)
                    {
                        HarmonyPatch_Mountain.PatternMatchJojaEvent(patched, i);
                    }
                }

                // If we couldn't find the community center route, warn the user. Things will only be visually off.
                if (!HarmonyPatch_Mountain.patchedCommunityRoute)
                {
                    HarmonyPatch_Mountain.monitor.Log($"World Changing Event: Glimmering Boulder (Community) could not be patched.", LogLevel.Warn);
                    HarmonyPatch_Mountain.monitor.Log($"The code sequence could was not found.", LogLevel.Warn);
                    HarmonyPatch_Mountain.monitor.Log($"The event will still function, but will look visually incorrect.", LogLevel.Warn);
                }

                // If we couldn't find the joja route, warn the user. Things will only be visually off.
                if (!HarmonyPatch_Mountain.patchedJojaRoute)
                {
                    HarmonyPatch_Mountain.monitor.Log($"World Changing Event: Glimmering Boulder (Joja) could not be patched.", LogLevel.Warn);
                    HarmonyPatch_Mountain.monitor.Log($"The code sequence could was not found.", LogLevel.Warn);
                    HarmonyPatch_Mountain.monitor.Log($"The event will still function, but will look visually incorrect.", LogLevel.Warn);
                }

                return patched;
            }
            catch (Exception ex)
            {
                HarmonyPatch_Mountain.monitor.Log($"Harmony patch {nameof(HarmonyPatch_Mountain)}. Transpile has encounter an error.", LogLevel.Warn);
                HarmonyPatch_Mountain.monitor.Log(ex.ToString(), LogLevel.Warn);
                HarmonyPatch_Mountain.monitor.Log($"The World Changing Event; Glimmering Boulder will still function, but it will look visually incorrect.", LogLevel.Warn);

                return instructions;
            }
        }

        /// <summary>
        /// Modifies values within the newly created <see cref="Mountain"/> instance right after allocation,
        /// correcting the positioning of the Glimmering Boulder and the Landslide entity.
        /// </summary>
        /// <param name="__instance">The newly created <see cref="Mountain"/> instance.</param>
        private static void Postfix(Mountain __instance)
        {
            var boulder = AccessTools.Field(typeof(Mountain), HarmonyPatch_Mountain.BOULDER_POSITION);
            var landslide = AccessTools.Field(typeof(Mountain), HarmonyPatch_Mountain.LANDSLICE_POSITION);

            var boulderPosition = new Vector2(48f, 11f) * 64f - new Vector2(4f, 3f) * 4f;
            var landslidePosition = new Rectangle(46 * 64, 256, 192, 320);

            boulder.SetValue(__instance, boulderPosition);
            landslide.SetValue(__instance, landslidePosition);
        }

        private static Label ccLabel;
        private static void PatternMatchCommunityCenterEvent(List<CodeInstruction> instructions, int i)
        {
            if (instructions[i].opcode == OpCodes.Switch)
            {
                ccLabel = (instructions[i].operand as Label[])[9];
                return;
            }
            if (ccLabel == default(Label))
                return;
            if (!instructions[i].labels.Contains(ccLabel))
                return;

            for (int j = i; j < instructions.Count; ++j)
            {
                if (instructions[j].opcode == OpCodes.Br)
                    break;
                if (instructions[j].opcode == OpCodes.Ldc_R4)
                {
                    if ((float)instructions[j].operand == 368f)
                        instructions[j].operand = 560f;
                }
            }

            HarmonyPatch_Mountain.patchedCommunityRoute = true;
        }

        private static Label jojaLabel;
        private static void PatternMatchJojaEvent(List<CodeInstruction> instructions, int i)
        {
            if (instructions[i].opcode == OpCodes.Switch)
            {
                jojaLabel = (instructions[i].operand as Label[])[8];
                return;
            }
            if (jojaLabel == default(Label))
                return;
            if (!instructions[i].labels.Contains(jojaLabel))
                return;

            // Remove Orange guy
            instructions.RemoveRange(i, 25);
            instructions[i].labels.Add(jojaLabel);

            // Drill Guy relocated
            for (int j = i; j < instructions.Count; ++j)
            {
                if (instructions[j].opcode == OpCodes.Br)
                    break;
                if (instructions[j].opcode == OpCodes.Ldc_R4)
                {
                    if ((float)instructions[j].operand == 3040f)
                        instructions[j].operand = 3104f;
                    else if ((float)instructions[j].operand == 160f)
                    {
                        instructions[j].operand = 640f;
                        break;
                    }
                }
            }

            // Tools relocated
            for (int j = i; j < instructions.Count; ++j)
            {
                if (instructions[j].opcode == OpCodes.Br)
                    break;
                if (instructions[j].opcode == OpCodes.Ldc_R4)
                {
                    if ((float)instructions[j].operand == 2816f)
                        instructions[j].operand = 3008f;
                    else if ((float)instructions[j].operand == 368f)
                    {
                        instructions[j].operand = 496f;
                        break;
                    }
                }
            }

            // Morris relocated
            for (int j = i; j < instructions.Count; ++j)
            {
                if (instructions[j].opcode == OpCodes.Br)
                    break;
                if (instructions[j].opcode == OpCodes.Ldc_R4)
                {
                    if ((float)instructions[j].operand == 3200f)
                        instructions[j].operand = 3092f;
                    else if ((float)instructions[j].operand == 368f)
                    {
                        instructions[j].operand = 496f;
                        break;
                    }
                }
            }

            HarmonyPatch_Mountain.patchedJojaRoute = true;
        }

        private static bool TryGetInstruction(List<CodeInstruction> instructions, int index, int offset, out CodeInstruction value)
        {
            value = null;

            if (index + offset >= instructions.Count)
            {
                return false;
            }

            value = instructions[index + offset];

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckOpCodeAndStrValue(List<CodeInstruction> instructions, int index, int offset, string value)
        {
            if (index + offset >= instructions.Count)
            {
                return false;
            }

            var cmd = instructions[index + offset];

            if (cmd.opcode != OpCodes.Ldstr || cmd.operand is not string str || str != value)
            {
                return false;
            }

            return true;
        }
    }
}
