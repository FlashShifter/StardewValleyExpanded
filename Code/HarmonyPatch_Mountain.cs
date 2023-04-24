using System;

using Microsoft.Xna.Framework;

using StardewValley.Locations;

using HarmonyLib;
using StardewModdingAPI;

namespace StardewValleyExpanded
{
    public static class HarmonyPatch_Mountain
    {
        private const string BOULDER_POSITION = "boulderPosition";
        private const string LANDSLICE_POSITION = "landSlideRect";

        public static bool Applied { get; private set; }

        static HarmonyPatch_Mountain()
        {
            HarmonyPatch_Mountain.Applied = false;
        }

        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            if (HarmonyPatch_Mountain.Applied)
            {
                return;
            }

            monitor.LogOnce($"Applying Harmony Patch: {nameof(HarmonyPatch_Mountain)}");

            var parameters = new Type[] { typeof(string), typeof(string) };
            var original = AccessTools.Constructor(typeof(Mountain), parameters);
            var postfix = new HarmonyMethod(typeof(HarmonyPatch_Mountain), nameof(Postfix));

            harmony.Patch(original, null, postfix);

            HarmonyPatch_Mountain.Applied = true;
        }

        public static void Postfix(Mountain __instance)
        {
            var boulder = AccessTools.Field(typeof(Mountain), HarmonyPatch_Mountain.BOULDER_POSITION);
            var landslide = AccessTools.Field(typeof(Mountain), HarmonyPatch_Mountain.LANDSLICE_POSITION);

            var boulderPosition = new Vector2(48f, 11f) * 64f - new Vector2(4f, 3f) * 4f;
            var landslidePosition = new Rectangle(46 * 64, 256, 192, 320);

            boulder.SetValue(__instance, boulderPosition);
            landslide.SetValue(__instance, landslidePosition);
        }
    }
}
