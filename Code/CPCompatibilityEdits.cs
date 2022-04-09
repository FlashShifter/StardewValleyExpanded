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
    /// <summary>Edits specific assets after Content Patcher and most other mods in order to fix compatibility issues.</summary>
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

                helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
                helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;

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
                ReplacementFilePath = "Assets/Lance.png",
                ModsRequired = new(){ "Poltergeister.SeasonalCuteSpritesSVE" }
            },

            new AssetToReplace()
            {
                AssetName = "Characters/Scarlett",
                ReplacementFilePath = "Assets/Scarlett.png",
                ModsRequired = new() { "Poltergeister.SeasonalCuteSpritesSVE" }
            }
        };

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        private static void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ticksUntilInitialization = 5; //begin counting down to initalization
            Helper.Events.GameLoop.GameLaunched -= GameLoop_GameLaunched; //disable this event
        }

        private static int? ticksUntilInitialization = null;
        private static void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (ticksUntilInitialization.HasValue) //if the initialization countdown has begun
            {
                if (ticksUntilInitialization.Value <= 0) //if the countdown has finished
                {
                    var editor = new CPCompatibilityEditor(); //create the editor
                    Helper.Content.AssetEditors.Add(editor); //add it to SMAPI's editor list

                    ticksUntilInitialization = null; //clear value
                    Helper.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked; //disable this event
                }
                else //if the countdown has NOT finished
                {
                    ticksUntilInitialization--;
                }
            }
            //if the countdown has not started yet, do nothing
        }

        /// <summary>Edits assets based on data in <see cref="CPCompatibilityEdits"/>.</summary>
        public class CPCompatibilityEditor : IAssetEditor
        {
            public bool CanEdit<T>(IAssetInfo asset)
            {
                foreach (var replacement in AssetsToReplace)
                {
                    if (asset.AssetNameEquals(replacement.AssetName))
                        return true;
                }
                return false;
            }

            public void Edit<T>(IAssetData asset)
            {
                try
                {
                    AssetToReplace removeForThisSession = null;
                    foreach (var replacement in AssetsToReplace)
                    {
                        if (asset.AssetNameEquals(replacement.AssetName))
                        {
                            if (replacement.ModsRequired?.All(mod => Helper.ModRegistry.IsLoaded(mod)) != false) //if all required mods are loaded (or no mods are required)
                            {
                                asset.ReplaceWith(Helper.Content.Load<T>(replacement.ReplacementFilePath, ContentSource.ModFolder)); //replace this asset with the specified local file
                                if (Monitor.IsVerbose)
                                    Monitor.LogOnce($"{typeof(CPCompatibilityEdits)}: Replacing asset \"{replacement.AssetName}\" with local file \"{replacement.ReplacementFilePath}\".", LogLevel.Trace);
                            }
                            else //if a required mod is not loaded
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

        /// <summary>The information necessary to replace a single asset.</summary>
        public class AssetToReplace
        {
            /// <summary>The in-game asset to replace, e.g. "Characters/Abigail".</summary>
            public string AssetName { get; set; }
            /// <summary>The address of the replacement file within this C# mod's folder, e.g. "assets/replacement.png".</summary>
            public string ReplacementFilePath { get; set; }
            /// <summary>(Optional) A list of mod IDs. If any of these mods are missing, the asset will NOT be replaced.</summary>
            public List<string> ModsRequired { get; set; } = null;
        }
    }
}
