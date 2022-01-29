using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace StardewValleyExpanded
{
    /// <summary>Removes crops from specific locations that formerly used TMXLoader's crop layer feature.</summary>
    public static class RemoveCropLayerCrops
    {
        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

        /// <summary>True if this fix is currently enabled.</summary>
        private static bool Enabled { get; set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Initialize and enable this class.</summary>
        /// <param name="helper">The SMAPI helper instance to use for events and other API access.</param>
        /// <param name="monitor">The monitor instance to use for log messages.</param>
        public static void Enable(IModHelper helper, IMonitor monitor)
        {
            if (!Enabled && helper != null && monitor != null) //if NOT already enabled
            {
                Helper = helper; //store helper
                Monitor = monitor; //store monitor

                //initialize fields
                ModDataKey = $"{Helper.ModRegistry.ModID}/{nameof(RemoveCropLayerCrops)}"; //create a unique key for this class's mod data

                //enable SMAPI events
                Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded_PrepareToCheckCrops;
                Helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked_CheckCrops;

                Enabled = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>A list of location names to check for crop removal.</summary>
        /// <remarks>
        /// All crops will be removed from these locations the first time the host player starts/loads each save.
        /// WARNING: It may be unsafe to add farmable locations to this list; it will remove all crops from the location, not just those added by TMXL crop layers.
        /// </remarks>
        public static List<string> LocationsToCheck { get; set; } = new List<string>()
        {
            "Custom_SpriteSpring2",
            "Custom_JunimoWoods",
            "Custom_SpriteSpringCave"
        };

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/
        
        /// <summary>The key to use when reading/writing mod data for this class.</summary>
        private static string ModDataKey { get; set; }
        /// <summary>If not null, crops should be checked at this time value.</summary>
        private static double? CheckCropsAtThisTime { get; set; } = null;

        /// <summary>When a game is started/loaded by the host, set a time at which crop layers should be checked.</summary>
        private static void GameLoop_SaveLoaded_PrepareToCheckCrops(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return; //do nothing

            CheckCropsAtThisTime = Game1.currentGameTime.TotalGameTime.TotalSeconds + 1; //check crops 1 second after the current time
        }

        /// <summary>When the "check crops" timer is set and matches the current time, check crop layers and clear the timer.</summary>
        private static void GameLoop_OneSecondUpdateTicked_CheckCrops(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsMainPlayer || CheckCropsAtThisTime == null || CheckCropsAtThisTime < Game1.currentGameTime.TotalGameTime.TotalSeconds) //if not ready yet, not the host player, or it's not time to check yet
                return; //do nothing

            CheckCropsAtThisTime = null; //clear the timer

            Monitor.VerboseLog("Cleaning up crops from obsolete TMXL crop layers...");
            foreach (string name in LocationsToCheck)
            {
                GameLocation location = Game1.getLocationFromName(name);
                if (location == null)
                {
                    Monitor.LogOnce($"Failed to find location while cleaning up obsolete TMXL crop layers: \"{name}\"", LogLevel.Trace);
                    continue; //skip this location
                }

                if (location.modData.TryGetValue(ModDataKey, out string data) && !string.IsNullOrWhiteSpace(data))
                {
                    Monitor.VerboseLog($"Already cleaned location \"{name}\". Skipping cleanup.");
                    continue;
                }

                RemoveCrops(location);
                location.modData[ModDataKey] = "true"; //mark this location as clean for future checks
            }
            Monitor.VerboseLog($"Crop layer cleanup complete.");
        }

        /// <summary>Removes all crops from the given location.</summary>
        /// <param name="location">The location to check.</param>
        private static void RemoveCrops(GameLocation location)
        {
            int removalCount = 0;
            for (int i = location.terrainFeatures.Count() - 1; i >= 0; i--) //loop backward through each terrain feature at this location
            {
                if (location.terrainFeatures.Pairs.ElementAt(i).Value is HoeDirt dirt && dirt.crop != null) //if this feature is a crop
                {
                    location.terrainFeatures.Remove(location.terrainFeatures.Pairs.ElementAt(i).Key); //remove it
                    removalCount++;
                }
            }
            Monitor.VerboseLog($"Removed {removalCount} crop(s) from {location.NameOrUniqueName}.");
        }
    }
}
