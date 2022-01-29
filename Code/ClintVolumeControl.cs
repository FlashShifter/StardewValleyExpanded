using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using System;

namespace StardewValleyExpanded
{
    /// <summary>Adjusts the volume of the ambient furnace noise outside Clint's blacksmith shop.</summary>
    public static class ClintVolumeControl
    {

        /*****            *****/
        /***** Setup Code *****/
        /*****            *****/

        /// <summary>True if this fix is currently enabled.</summary>
        private static bool Enabled { get; set; } = false;
        /// <summary>The SMAPI helper to use for events and other API access.</summary>
        private static IModHelper Helper { get; set; } = null;
        /// <summary>The monitor to use for log messages.</summary>
        private static IMonitor Monitor { get; set; } = null;

        /// <summary>Initializes and enables this class.</summary>
        /// <param name="helper">The <see cref="IModHelper"/> provided by SMAPI. Used for events and other API access.</param>
        /// <param name="monitor">The <see cref="IMonitor"/> provided by SMAPI. Used for log messages.</param>
        public static void Enable(IModHelper helper, IMonitor monitor)
        {
            if (!Enabled) //if not already enabled
            {
                //store SMAPI tools
                Helper = helper;
                Monitor = monitor;

                //replace ambient engine sound with a custom wrapper
                Monitor.Log($"Replacing sound cue \"{nameof(AmbientLocationSounds)}.engine\" with a wrapper for volume control.", LogLevel.Trace);
                var engineField = Helper.Reflection.GetField<ICue>(typeof(AmbientLocationSounds), "engine", true); //reflect the field
                ICue engine = engineField.GetValue(); //get the cue
                engineField.SetValue(new CueWrapper(engine)); //create a wrapper and replace the original with it

                Enabled = true;
            }
        }

        /*****              *****/
        /***** Mod Settings *****/
        /*****              *****/

        /// <summary>Modifies the volume of the "engine" sound outside Clint's blacksmith shop.</summary>
        /// <param name="originalVolume">The original volume to set, from 0 to 100. Modified by the player's ambient volume setting and distance from the sound source.</param>
        /// <returns>The modified volume to set instead of the original, from 0 to 100.</returns>
        public static float VolumeModifier(float originalVolume)
        {
            float newVolume = originalVolume - ((ShortestDistanceForEngine / 32f) * 1.5f);
            return newVolume;
        }

        /*****               *****/
        /***** Internal Code *****/
        /*****               *****/

        private static IReflectedField<float[]> shortestDistanceForCue;

        /// <summary>The number of actual pixels between the player and the nearest engine noise source tile. 64 per tile in a direct line.</summary>
        private static float ShortestDistanceForEngine
        {
            get
            {
                if (shortestDistanceForCue == null) //if this field hasn't been reflected yet
                    shortestDistanceForCue = Helper.Reflection.GetField<float[]>(typeof(AmbientLocationSounds), "shortestDistanceForCue", true); //reflect it

                return shortestDistanceForCue.GetValue()[AmbientLocationSounds.sound_engine]; //get the distance to the nearest engine sound
            }
        }

        /// <summary>A sound cue wrapper that modifies volume values. Otherwise behaves like a normal <see cref="ICue"/>.</summary>
        public class CueWrapper : ICue
        {
            /***** Modified Code *****/

            /// <summary>The "original" cue this class will contain.</summary>
            protected ICue WrappedCue { get; set; }

            public CueWrapper(ICue cue)
            {
                WrappedCue = cue;
            }

            public void SetVariable(string var, int val)
            {
                if (var.Equals("Volume", StringComparison.OrdinalIgnoreCase) && Game1.player?.currentLocation is Town) //if volume is being set AND the local player is in town
                {
                    val = Convert.ToInt32(Utility.Clamp(VolumeModifier(val), 0, 100)); //modify the volume, clamp it between 0 and 100, and convert it to the nearest integer
                }

                WrappedCue.SetVariable(var, val);
            }

            public void SetVariable(string var, float val)
            {
                if (var.Equals("Volume", StringComparison.OrdinalIgnoreCase) && Game1.player?.currentLocation is Town) //if volume is being set AND the local player is in town
                {
                    val = Utility.Clamp(VolumeModifier(val), 0, 100); //modify the volume and clamp it between 0 and 100
                }

                WrappedCue.SetVariable(var, val);
            }

            /***** Unmodified Code *****/

            public bool IsStopped => WrappedCue.IsStopped;

            public bool IsStopping => WrappedCue.IsStopping;

            public bool IsPlaying => WrappedCue.IsPlaying;

            public bool IsPaused => WrappedCue.IsPaused;

            public string Name => WrappedCue.Name;

            public float Pitch { get => WrappedCue.Pitch; set => WrappedCue.Pitch = value; }
            public float Volume { get => WrappedCue.Volume; set => WrappedCue.Volume = value; }

            public bool IsPitchBeingControlledByRPC => WrappedCue.IsPitchBeingControlledByRPC;

            public void Play()
            {
                WrappedCue.Play();
            }

            public void Pause()
            {
                WrappedCue.Pause();
            }

            public void Resume()
            {
                WrappedCue.Resume();
            }

            public void Stop(AudioStopOptions options)
            {
                WrappedCue.Stop(options);
            }           

            public float GetVariable(string var)
            {
                return WrappedCue.GetVariable(var);
            }

            public void Dispose()
            {
                WrappedCue.Dispose();
            }
        }
    }
}
