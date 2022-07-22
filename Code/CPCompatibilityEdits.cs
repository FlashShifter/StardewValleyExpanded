using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewValleyExpanded
{
    /// <summary>Fixes compatibility issues by editing specific textures after Content Patcher and most other mods.</summary>
    public static class CPCompatibilityEdits
    {

        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

        /// <summary>True if this class's features are currently enabled.</summary>
        public static bool Enabled { get; private set; } = false;
        /// <summary>The SMAPI helper instance to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor instance to use for log messages. Null if not provided.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Enables this class's features by setting up SMAPI events.</summary>
        /// <param name="helper">A SMAPI helper instance, used to set up events.</param>
        public static void Enable(IModHelper helper, IMonitor monitor)
        {
            if (!Enabled && helper != null && monitor != null) //if not already enabled AND valid tools were provided
            {
                Helper = helper; //store the helper
                Monitor = monitor; //store the monitor

                helper.Events.Content.AssetRequested += Content_AssetRequested;

                Enabled = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>A set of assets to replace. See the <see cref="AssetToReplace"/> class fields for information.</summary>
        public static List<AssetToReplace> AssetsToReplace = new()
        {
            new AssetToReplace()
            {
                AssetName = "Characters/Lance",
                ReplacementFilePath = "assets/Lance.png",
                ModsRequired = new() { "Poltergeister.SeasonalCuteSpritesSVE" },
                ModsToAvoid = new() { "Nom0ri.LannahFemLance" }
            },

            new AssetToReplace()
            {
                AssetName = "Characters/Scarlett",
                ReplacementFilePath = "assets/Scarlett.png",
                ModsRequired = new() { "Poltergeister.SeasonalCuteSpritesSVE" }
            }
        };

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        /// <summary>The information necessary to replace a single asset.</summary>
        public class AssetToReplace
        {
            /// <summary>The in-game asset to replace, e.g. "Characters/Abigail".</summary>
            public string AssetName { get; set; }
            /// <summary>The address of the replacement file within this C# mod's folder, e.g. "assets/replacement.png".</summary>
            public string ReplacementFilePath { get; set; }
            /// <summary>(Optional) A list of mod IDs to require. If any of these mods are NOT loaded, the asset will NOT be replaced.</summary>
            public List<string> ModsRequired { get; set; } = null;
            /// <summary>(Optional) A list of mod IDs to avoid. If any of these mods are loaded, the asset will NOT be replaced.</summary>
            public List<string> ModsToAvoid { get; set; } = null;
        }

        /// <summary>Loads/edits assets as necessary when requested through the content system.</summary>
        private static void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            try
            {
                AssetToReplace removeForThisSession = null;
                foreach (var replacement in AssetsToReplace)
                {
                    if (e.DataType == typeof(Texture2D) && e.NameWithoutLocale.IsEquivalentTo(replacement.AssetName, true)) //if this asset is a texture and has a replacement
                    {
                        if (replacement.ModsRequired?.All(mod => Helper.ModRegistry.IsLoaded(mod)) != false //if all required mods are loaded (or no mods are required)
                        && replacement.ModsToAvoid?.All(mod => !Helper.ModRegistry.IsLoaded(mod)) != false) //and all incompatible mods are NOT loaded (or no mods are incompatible) 
                        {
                            e.Edit(asset => asset.ReplaceWith(Helper.ModContent.Load<Texture2D>(replacement.ReplacementFilePath)), AssetEditPriority.Late); //after most other mods have applied edits, load the replacement and overwrite the asset
                            if (Monitor.IsVerbose)
                                Monitor.LogOnce($"{typeof(CPCompatibilityEdits)}: Replacing asset \"{replacement.AssetName}\" with local file \"{replacement.ReplacementFilePath}\".", LogLevel.Trace);
                        }
                        else //if the loaded mods do NOT match the requirements
                        {
                            removeForThisSession = replacement;
                            if (Monitor.IsVerbose)
                                Monitor.LogOnce($"{typeof(CPCompatibilityEdits)}: Skipping replacement for asset \"{replacement.AssetName}\": A required mod was not found.", LogLevel.Trace);
                        }

                        break; //stop checking this asset for replacements
                    }
                }

                if (removeForThisSession != null) //if this replacement should no longer be checked (e.g. due to missing mods)
                    AssetsToReplace.Remove(removeForThisSession); //remove it
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"{typeof(CPCompatibilityEdits)}: Error while trying to replace an asset with a local version. Compatibility edits for certain mods might not be applied. Full error message: \n{ex}", LogLevel.Error);
            }
        }
    }
}
