/***
 *      Contents originally written by shekurika for Ridgeside Village (RSV).
 *      Later updates made by moe/cl4r3.
 *      Log utility format by moe/cl4r3.
 *      Modified by Esca for use with Stardew Valley Expanded (SVE).
 *      
 *      Original repo: https://github.com/Rafseazz/Ridgeside-Village-Mod
 *      Original file: https://github.com/Rafseazz/Ridgeside-Village-Mod/blob/4ef742e9e4b6a42438cc79bb11fd8505d1d39a9c/Ridgeside%20SMAPI%20Component%202.0/RidgesideVillage/InstallationChecker.cs
 ***/

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace RidgesideVillage
{
    internal static class Log
    {
        internal static IMonitor Monitor { private get; set; } = null;
        internal static void Error(string msg) => Monitor.Log(msg, LogLevel.Error);
        internal static void Alert(string msg) => Monitor.Log(msg, LogLevel.Alert);
        internal static void Warn(string msg) => Monitor.Log(msg, LogLevel.Warn);
        internal static void Info(string msg) => Monitor.Log(msg, LogLevel.Info);
        internal static void Debug(string msg) => Monitor.Log(msg, LogLevel.Debug);
        internal static void Trace(string msg) => Monitor.Log(msg, LogLevel.Trace);
        internal static void Verbose(string msg) => Monitor.VerboseLog(msg);
    }

    public class Dependency
    {
        public string url;
        public string author;
        public string name;
        public string uniqueID;
        public bool required;
        public bool component;
#nullable enable
        public string? minVersion;
        public string? parents;
#nullable disable
    }

    internal class InstallationChecker
    {
        IModHelper helper;

        static string BOLDLINE = new string('#', 75);
        static string THINLINE = new string('-', 75);
        static string BULLET = "\t \x1A ";
        static string INDENT = "\t" + new string(' ', 5);

        bool isInstalledCorrectly = true;
        bool hasAllDependencies = true;
        List<Dependency> missing_dependencies = new List<Dependency>();
        List<Dependency> outdated_dependencies = new List<Dependency>();
        List<Dependency> missing_parents = new List<Dependency>();

        /// <summary>Performs an installation check with no conditions while displaying SVE's added messages.</summary>
        public static void AutoCheck(IModHelper Helper, IMonitor Monitor)
        {
            Monitor.Log(Helper.Translation.Get("installationchecker.start"), LogLevel.Info);
            Monitor.Log(Helper.Translation.Get("installationchecker.credits"), LogLevel.Info);
            if (new RidgesideVillage.InstallationChecker().checkInstallation(Helper, Monitor)) //run a check; if it succeeds...
                Monitor.Log(Helper.Translation.Get("installationchecker.success"), LogLevel.Info);
        }

        public bool checkInstallation(IModHelper Helper, IMonitor Monitor)
        {
            helper = Helper;
            Log.Monitor = Monitor;
            var dependencies = helper.Data.ReadJsonFile<Dictionary<string, Dependency>>(PathUtilities.NormalizePath("assets/Dependencies.json"));

            Log.Trace($"Number of dependencies to check: {dependencies.Values.Count}");
            foreach (var dependency in dependencies.Values)
            {
                Log.Trace($"InstallationChecker checking {dependency.name}...");
                if (dependency.name != "SMAPI" && !helper.ModRegistry.IsLoaded(dependency.uniqueID))
                {
                    if (dependency.required)
                    {
                        Log.Trace($"{dependency.name} is missing.");
                        if (dependency.parents == null)  // no parent dependencies
                            missing_dependencies.Add(dependency);
                        else if (dependency.parents != null && !TheseModsLoaded(dependency.parents)) // not loaded bc missing parent dependencies
                            missing_parents.Add(dependency);
                        else // has parent dependencies but they're loaded
                            missing_dependencies.Add(dependency);
                        continue;
                    }
                    else
                    {
                        Log.Trace($"{dependency.name} is missing but not required.");
                        continue;
                    }
                }
                else
                {
                    if (dependency.minVersion != null)
                    {
                        ISemanticVersion localVersion = null;
                        if (dependency.name == "SMAPI")
                            localVersion = Constants.ApiVersion;
                        else
                            localVersion = helper.ModRegistry.Get(dependency.uniqueID)?.Manifest.Version;

                        if (localVersion == null)
                            continue;

                        if (localVersion.IsOlderThan(dependency.minVersion))
                        {
                            Log.Trace($"{dependency.name}: Local version ({localVersion.ToString()}) is older than required version ({dependency.minVersion}).");
                            outdated_dependencies.Add(dependency);
                            continue;
                        }
                    }
                }

                Log.Trace($"{dependency.name} is loaded and up to date.");
            }

            Log.Trace($"Number of missing mods: {missing_dependencies.Count}");
            Log.Trace($"Number of out of date mods: {outdated_dependencies.Count}");

            if (outdated_dependencies.Any() || missing_dependencies.Any())
                hasAllDependencies = false;

            if (missing_dependencies.Concat(outdated_dependencies).Any(dependency => dependency.component)) //if any component mod is missing or outdated
                isInstalledCorrectly = false;

            if (!isInstalledCorrectly || !hasAllDependencies)
                helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            return isInstalledCorrectly && hasAllDependencies;
        }

        public bool TheseModsLoaded(string mods)
        {
            string[] reqs = mods.Split(",");
            foreach(string req in reqs)
            {
                if (missing_dependencies.FindAll(d => d.name == req.Trim()).Any() || outdated_dependencies.FindAll(d => d.name == req.Trim()).Any())
                {
                    return false;
                }
            }
            return true;
        }


        [EventPriority(EventPriority.Low)]
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            for(int i=0; i<3; i++)
            {
                Log.Error(BOLDLINE);
            }

            Log.Error("");
            Log.Error(helper.Translation.Get("installationchecker.installation.incorrect"));

            if (missing_parents.Any())
            {
                Log.Error("");
                Log.Error(THINLINE);
                Log.Error("");
                Log.Error(helper.Translation.Get("installationchecker.orphaned.mods"));
                Log.Error(helper.Translation.Get("installationchecker.orphaned.mods.cont"));
                Log.Error("");
                foreach (var dependency in missing_parents)
                {
                    Log.Error(BULLET + helper.Translation.Get("installationchecker.mod.info", new { modName = dependency.name, author = dependency.author }));
                    Log.Error(INDENT + helper.Translation.Get("installationchecker.mod.parents", new { parents = dependency.parents }));
                }
            }
            if (missing_dependencies.Any())
            {
                Log.Error("");
                Log.Error(THINLINE);
                Log.Error("");
                Log.Error(helper.Translation.Get("installationchecker.missing.mods"));
                Log.Error(helper.Translation.Get("installationchecker.missing.mods.cont"));
                Log.Error("");
                foreach (var dependency in missing_dependencies)
                {
                    Log.Error(BULLET + helper.Translation.Get("installationchecker.mod.info", new { modName = dependency.name, author = dependency.author}));
                    Log.Error(INDENT + dependency.url);
                }
            }
            if (outdated_dependencies.Any())
            {
                Log.Error("");
                Log.Error(THINLINE);
                Log.Error("");
                Log.Error(helper.Translation.Get("installationchecker.outdated.mods"));
                Log.Error(helper.Translation.Get("installationchecker.outdated.mods.cont"));
                Log.Error("");
                foreach (var dependency in outdated_dependencies)
                {
                    if (dependency.name == "SMAPI")
                        Log.Error(BULLET + helper.Translation.Get("installationchecker.smapi.outdated"));
                    else
                        Log.Error(BULLET + helper.Translation.Get("installationchecker.mod.info", new { modName = dependency.name, author = dependency.author}));
                    Log.Error(INDENT + dependency.url);
                }
            }
            if (!isInstalledCorrectly)
            {
                Log.Error("");
                Log.Error(THINLINE);
                Log.Error("");
                Log.Error(helper.Translation.Get("installationchecker.component.missing"));
            }
            Log.Error("");
            Log.Error(THINLINE);
            Log.Error("");
            Log.Error(helper.Translation.Get("installationchecker.help.message"));
            Log.Error("");

            for (int i = 0; i < 3; i++)
            {
                Log.Error(BOLDLINE);
            }
        }

        
    }

}
