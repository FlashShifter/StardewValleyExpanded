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
using StardewValley.Tools;

namespace StardewValleyExpanded
{
    /// <summary>Implements a set of custom "Action" tile property behaviors.</summary>
    /// <remarks>
    /// Additional credits:
    /// * Adds backup implementations for certain TMXLoader features due to maintenance issues; TMXLoader is by Platonymous
    ///     ** This implements support for a basic imitation of TMXL's "Action Lock" to cover SVE's specific use cases
    /// </remarks>
    public static class ActionProperties
    {
		/*****            *****/
		/***** Setup Code *****/
		/*****            *****/

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

                GameLocation.RegisterTileAction("SVE_Lock", SveLockAction);

                Applied = true;
            }
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        private static bool SveLockAction(GameLocation __instance, string[] fields, Farmer who, Point tileLocation)
        {

            if (fields.Length > 2 && int.TryParse(fields[1], out int amount)) //if 2 parameters exist ("SVE_Lock <ObjectAmount> <ObjectID>")
            {
                string id = fields[2];
                //get the success/failure/default tile properties
                string successText = __instance.doesTileHavePropertyNoNull(tileLocation.X, tileLocation.Y, "Success", "Buildings");
                string failureText = __instance.doesTileHavePropertyNoNull(tileLocation.X, tileLocation.Y, "Failure", "Buildings");
                string defaultText = __instance.doesTileHavePropertyNoNull(tileLocation.X, tileLocation.Y, "Default", "Buildings");

                if (who.ActiveObject != null && Utility.IsNormalObjectAtParentSheetIndex(who.ActiveObject, id) && who.ActiveObject.Stack >= amount) //if success (player is holding up the correct object & amount)
                {
                    Item result = null;
                    try
                    {
                        if (successText.StartsWith('T')) //if this needs special handling to create a tool
                        {
                            string[] successSplit = successText.Split(' ');
                            switch (successSplit[1].ToLower())
                            {
                                //TODO: add handlers for other tool subclasses if needed; this is currently limited to SVE's actual usage
                                case "slingshot":
                                    result = new Slingshot(successSplit[2]); //create a slingshot with field 2 as its ID
                                    break;
                            }
                        }
                        else
                        {
                            result = Utility.getItemFromStandardTextDescription(successText, who); //try to get the item described by the success text
                        }
                    }
                    catch (Exception)
                    {
                        Monitor.LogOnce($"{nameof(ActionProperties)}: Action SVE_Lock cannot parse the \"Success\" tile property at {__instance.Name} ({tileLocation.X},{tileLocation.Y}).\nPlease use this format: \"<ObjectType> <ID> <Amount>\". Example: \"O 206 1\"", LogLevel.Debug);
                        return true;
                    }
                    if (result != null) //if the item was created
                    {
                        Monitor.VerboseLog($"{nameof(ActionProperties)}: SVE_Lock Success. Removing {amount} of item {id} from player {who.Name} and adding {result.Stack} of item {result.ParentSheetIndex}.");
                        while (amount > 0 && who.ActiveObject != null) //subtract the necessary amount of the held object
                        {
                            who.reduceActiveItemByOne();
                            amount--;
                        }
                        who.addItemByMenuIfNecessary(result); //give the result object to the player

                        if (fields.Length > 3) //if the optional "sound effect name" field exists
                        {
                            try { Game1.soundBank.GetCue(fields[3]); } //check whether this sound exists (case-sensitive; throws an error if the sound doesn't exist)
                            catch
                            {
                                Monitor.LogOnce($"{nameof(ActionProperties)}: Tried to play a sound effect with an invalid name.\nSound name: \"{fields[3]}\". Tile: {tileLocation.X},{tileLocation.Y}. Location: {__instance.Name}.", LogLevel.Debug);
                                return true;
                            }
                            Game1.playSound(fields[3]); //if the sound exists, play it
                        }
                    }
                }
                else if (who.ActiveObject is StardewValley.Object) //if failure (player is holding up an object, but it has the wrong ID or insufficient amount)
                {
                    Monitor.VerboseLog($"{nameof(ActionProperties)}: SVE_Lock Failure (player is holding incorrect/insufficient item). Displaying failure message if provided.");
                    if (!string.IsNullOrWhiteSpace(failureText))
                        Game1.drawDialogueNoTyping(failureText);
                }
                else //if default (player isn't holding up an object)
                {
                    Monitor.VerboseLog($"{nameof(ActionProperties)}: SVE_Lock Default (player is not holding a relevant item). Displaying default message if provided.");
                    if (!string.IsNullOrWhiteSpace(defaultText))
                        Game1.drawDialogueNoTyping(defaultText);
                }
            }
            else //not enough fields or they weren't readable integers
            {
                Monitor.LogOnce($"{nameof(ActionProperties)}: Cannot parse Action SVE_Lock at {__instance.Name} ({tileLocation.X},{tileLocation.Y}).\nPlease use this format: \"SVE_Lock <ObjectAmount> <ObjectID>\"", LogLevel.Debug);
            }
            return true;
        }
	}
}
