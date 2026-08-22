using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Njoror.Configuration;

namespace Njoror
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NjororPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.bigai.njoror";
        public const string PluginName = "Njoror";
        public const string PluginVersion = "1.0.0";

        public static NjororPlugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;

        private Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            try
            {
                Log.LogInfo($"══════════════════════════════════════════");
                Log.LogInfo($"  {PluginName} v{PluginVersion} loading...");
                Log.LogInfo($"══════════════════════════════════════════");

                // 1. Initialize configuration
                ModConfig.Initialize(Config);

                // 2. Apply Harmony patches
                _harmony = new Harmony(PluginGUID);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                Log.LogInfo($"[{PluginName}] All Harmony patches applied successfully.");
                Log.LogInfo($"[{PluginName}] Fair winds, ocean storm modulation, and serpent controls active.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[{PluginName}] Failed to initialize: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Log.LogInfo($"[{PluginName}] Unloaded.");
        }
    }
}
