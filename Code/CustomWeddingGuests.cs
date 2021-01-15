using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewValleyExpanded
{
    class CustomWeddingGuests : IAssetLoader
    {
        private const string AssetName = "Data/CustomWeddingGuestPositions";
        private static Mod modInstance;

        public CustomWeddingGuests(Mod modInstance)
        {
            CustomWeddingGuests.modInstance = modInstance;
        }

        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals(AssetName);
        }

        public T Load<T>(IAssetInfo asset)
        {
            return (T)(object)new Dictionary<string, string>();
        }

        public static string getCelebrationPositionsForDatables_Postfix(string originalValue, List<string> people_to_exclude)
        {
            try
            {
                Dictionary<string, string> locations = Game1.content.Load<Dictionary<string, string>>(AssetName);
                Log($"Setting custom wedding positions for {locations.Count} custom NPCs", LogLevel.Trace);
                return locations.Where(kvp => !people_to_exclude.Contains(kvp.Key)).
                                 Aggregate(originalValue, (locationString, kvp) => locationString + kvp.Key + " " + kvp.Value + " ");
            }
            catch (Exception ex)
            {
                Log($"Error setting wedding positions for custom NPCs: {ex}", LogLevel.Error);
                return originalValue;
            }
        }

        private static void Log(string message, LogLevel level = LogLevel.Trace)
        {
            if (CustomWeddingGuests.modInstance != null)
            {
                CustomWeddingGuests.modInstance.Monitor.Log(message, level);
            }
        }
    }
}
