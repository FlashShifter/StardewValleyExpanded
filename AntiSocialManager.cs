using Harmony;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SuperAardvark.AntiSocial
{
    /// <summary>
    /// This class can be copied into any mod to provide ad-hoc AntiSocial functionality.  Just call AntiSocialManager.DoSetupIfNecessary in your mod's Entry method.
    /// </summary>
    public class AntiSocialManager : IAssetLoader
    {
        public const string AssetName = "Data/AntiSocialNPCs";
        public const string OriginModId = "SuperAardvark.AntiSocial";

        private static Mod modInstance;
        private static bool adHoc = false;

        public static AntiSocialManager Instance { get; private set; }

        /// <summary>
        /// Checks for the AntiSocial stand-alone mod before running setup.
        /// </summary>
        /// <param name="modInstance">A reference to your Mod class.</param>
        public static void DoSetupIfNecessary(Mod modInstance)
        {
            if (modInstance.ModManifest.UniqueID.Equals(OriginModId))
            {
                modInstance.Monitor.Log("AntiSocial Mod performing stand-alone setup.", LogLevel.Info);
                adHoc = false;
                DoSetup(modInstance);
            }
            else if (modInstance.Helper.ModRegistry.IsLoaded(OriginModId))
            {
                modInstance.Monitor.Log("AntiSocial Mod loaded.  Skipping ad hoc setup.", LogLevel.Info);
            }
            else if (AntiSocialManager.modInstance != null)
            {
                modInstance.Monitor.Log("AntiSocial setup was already completed.", LogLevel.Info);
            }
            else
            {
                modInstance.Monitor.Log("AntiSocial Mod not loaded.  Performing ad hoc setup.", LogLevel.Info);
                adHoc = true;
                DoSetup(modInstance);
            }
        }

        /// <summary>
        /// Sets up AntiSocial.
        /// </summary>
        /// <param name="modInstance"></param>
        private static void DoSetup(Mod modInstance)
        {
            if (Instance != null)
            {
                modInstance.Monitor.Log($"AntiSocial setup was already completed by {AntiSocialManager.modInstance.ModManifest.Name} ({AntiSocialManager.modInstance.ModManifest.UniqueID}).", LogLevel.Warn);
                return;
            }

            Instance = new AntiSocialManager();
            AntiSocialManager.modInstance = modInstance;
            modInstance.Helper.Content.AssetLoaders.Add(Instance);

            HarmonyInstance harmonyInstance = HarmonyInstance.Create(OriginModId);
            MethodInfo methodInfo = AccessTools.Method(typeof(NPC), "get_CanSocialize");
            harmonyInstance.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(AntiSocialManager), "get_CanSocialize_Postfix"), (HarmonyMethod)null);
        }

        public static bool get_CanSocialize_Postfix(
            bool originalReturnValue,
            NPC __instance)
        {
            try
            {
                if (originalReturnValue && Game1.content.Load<Dictionary<string, string>>(AssetName).ContainsKey(__instance.Name))
                {
                    Log($"Overriding CanSocialize for {__instance.Name}", LogLevel.Info);
                    return false;
                }
                else
                {
                    return originalReturnValue;
                }
            }
            catch (Exception ex)
            {
                Log($"Error in get_CanSocialize postfix patch: {ex}", LogLevel.Error);
                return originalReturnValue;
            }
        }

        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals(AntiSocialManager.AssetName);
        }

        public T Load<T>(IAssetInfo asset)
        {
            return (T)(object)new Dictionary<string, string>();
        }

        private static void Log(String message, LogLevel level = LogLevel.Trace)
        {
            modInstance.Monitor.Log((adHoc ? "[AntiSocial] " + message : message), level);
        }
    }
}
