using System;
using System.Collections.Generic;
using HarmonyLib;
using Njoror.Configuration;
using Njoror.Logic;
using UnityEngine;

namespace Njoror.Patches
{
    // 1. Apply already-authoritative wind state on clients and support timed server checks.
    [HarmonyPatch(typeof(EnvMan), "UpdateWind")]
    public static class EnvManWindPatch
    {
        [HarmonyPostfix]
        public static void Postfix(EnvMan __instance, ref Vector4 ___m_wind)
        {
            if (__instance == null)
                return;

            try
            {
                Vector3 windDirection = new Vector3(___m_wind.x, ___m_wind.y, ___m_wind.z);
                if (ZNet.instance != null && ZNet.instance.IsServer())
                {
                    if (OceanWindEngine.TryGetTimedWindOverride(windDirection, out Vector3 timedDirection, out bool decisionMade))
                        ___m_wind = new Vector4(timedDirection.x, timedDirection.y, timedDirection.z, ___m_wind.w);

                    if (decisionMade)
                        SailingStateNetwork.BroadcastAuthoritativeWind(new Vector3(___m_wind.x, ___m_wind.y, ___m_wind.z), ___m_wind.w);
                }
                else if (SailingStateNetwork.TryGetAuthoritativeWind(out Vector3 authoritativeDirection, out float authoritativeIntensity))
                {
                    ___m_wind = new Vector4(
                        authoritativeDirection.x,
                        authoritativeDirection.y,
                        authoritativeDirection.z,
                        authoritativeIntensity);
                }
            }
            catch (Exception ex)
            {
                NjororPlugin.Log.LogError($"[Njoror] Error during wind adjustment: {ex}");
            }
        }
    }

    // 2. A new target wind is selected only once per Valheim wind cycle.
    [HarmonyPatch(typeof(EnvMan), "SetTargetWind")]
    public static class EnvManTargetWindPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref Vector3 dir, ref float intensity)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || !ModConfig.CheckDeflectOnWindChange.Value)
                return;

            try
            {
                dir = OceanWindEngine.AdjustWindForSailing(dir);
                intensity = OceanWindEngine.AdjustWindIntensity(intensity);
                SailingStateNetwork.BroadcastAuthoritativeWind(dir, intensity);
                NjororPlugin.LogDebug($"[Njoror][Diag] Wind-cycle fair-wind check completed: direction={dir}, intensity={intensity:F3}.");
            }
            catch (Exception ex)
            {
                NjororPlugin.Log.LogError($"[Njoror] Error during wind-cycle adjustment: {ex}");
            }
        }
    }

    // 3. Hook EnvMan environment selection to tune storm/clear weather frequencies
    [HarmonyPatch(typeof(EnvMan), "GetAvailableEnvironments")]
    public static class EnvManWeatherPatch
    {
        [HarmonyPostfix]
        public static void Postfix(List<EnvEntry> __result)
        {
            NjororPlugin.LogDebug("[Njoror][Diag] EnvMan.GetAvailableEnvironments postfix entered.");
            if (__result == null)
            {
                NjororPlugin.LogDebug("[Njoror][Diag] Environment tuning skipped: result was null.");
                return;
            }

            try
            {
                OceanWindEngine.ApplyEnvironmentWeights(__result);
            }
            catch (Exception ex)
            {
                NjororPlugin.Log.LogError($"[Njoror] Error during weather weight adjustment: {ex}");
            }
        }
    }

    // 4. Hook SpawnSystem to configure Serpent encounter rates
    [HarmonyPatch(typeof(SpawnSystem), "Awake")]
    public static class SpawnSystemSerpentPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SpawnSystem __instance)
        {
            NjororPlugin.LogDebug("[Njoror][Diag] SpawnSystem.Awake postfix entered.");
            if (__instance == null)
            {
                NjororPlugin.LogDebug("[Njoror][Diag] Serpent tuning skipped: SpawnSystem instance was null.");
                return;
            }

            if (__instance.m_spawnLists == null)
            {
                NjororPlugin.LogDebug("[Njoror][Diag] Serpent tuning skipped: spawn list was null.");
                return;
            }

            try
            {
                if (!ModConfig.EnableSerpentTuning.Value)
                {
                    NjororPlugin.LogDebug("[Njoror][Diag] Serpent tuning skipped: disabled by configuration.");
                    return;
                }

                foreach (var list in __instance.m_spawnLists)
                {
                    if (list == null || list.m_spawners == null) continue;

                    foreach (var spawner in list.m_spawners)
                    {
                        if (spawner != null && spawner.m_prefab != null && spawner.m_prefab.name.IndexOf("Serpent", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            spawner.m_spawnInterval = ModConfig.SerpentSpawnIntervalSeconds.Value;
                            spawner.m_spawnChance = ModConfig.NighttimeSerpentSpawnChance.Value;
                            spawner.m_spawnAtDay = ModConfig.DaytimeSerpentSpawnChance.Value > 0 || ModConfig.AllowCalmWeatherDaySerpents.Value;

                            NjororPlugin.LogDebug($"[Njoror][Diag] Configured Serpent spawner: Interval={spawner.m_spawnInterval}s, NightChance={spawner.m_spawnChance}%, DayChance={ModConfig.DaytimeSerpentSpawnChance.Value}%");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NjororPlugin.Log.LogError($"[Njoror] Error configuring Serpent spawners: {ex}");
            }
        }
    }
}
