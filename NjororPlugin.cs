using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using Njoror.Configuration;
using Njoror.Logic;

namespace Njoror
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class NjororPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.bigai.njoror_fairwinds";
        public const string PluginName = "Njoror_FairWinds";
        public const string PluginVersion = "1.0.0";

        public static NjororPlugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;

        public static void LogDebug(string message)
        {
            if (ModConfig.EnableDebugLogging.Value)
                Log.LogInfo(message);
        }

        private Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            try
            {
                // 1. Initialize configuration
                ModConfig.Initialize(Config);

                // Client instances report their actual active-ship heading; the server validates and consumes it.
                SailingStateNetwork.Initialize();

                // 2. Apply Harmony patches
                _harmony = new Harmony(PluginGUID);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                int patchedMethodCount = 0;
                foreach (MethodBase patchedMethod in _harmony.GetPatchedMethods())
                {
                    patchedMethodCount++;
                    LogDebug($"[{PluginName}][Diag] Bound Harmony target: {patchedMethod.DeclaringType?.FullName}::{patchedMethod.Name}");
                }

                LogDebug($"[{PluginName}] All Harmony patches applied successfully ({patchedMethodCount} target(s)).");
                LogDebug($"[{PluginName}] Fair winds, ocean storm modulation, and serpent controls active.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[{PluginName}] Failed to initialize: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            LogDebug($"[{PluginName}] Unloaded.");
        }
    }
}
