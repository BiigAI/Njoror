using System;
using System.Collections.Generic;
using HarmonyLib;
using Njoror.Configuration;
using Njoror.Logic;
using UnityEngine;

namespace Njoror.Patches
{
    // 1. Hook EnvMan wind calculations to inject server-side fair winds
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateWind))]
    public static class EnvManWindPatch
    {
        [HarmonyPostfix]
        public static void Postfix(EnvMan __instance, ref Vector3 ___m_dir, ref float ___m_windIntensity)
        {
            if (__instance == null) return;

            try
            {
                ___m_dir = OceanWindEngine.AdjustWindForSailing(___m_dir, __instance.m_windAngle);
                ___m_windIntensity = OceanWindEngine.AdjustWindIntensity(___m_windIntensity);
            }
            catch (Exception ex)
            {
                NjororPlugin.Log.LogError($"[Njörðr] Error during wind adjustment: {ex}");
            }
        }
    }

    // 2. Hook EnvMan environment selection to tune storm/clear weather frequencies
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetAvailableEnvironments))]
    public static class EnvManWeatherPatch
    {
        [HarmonyPostfix]
        public static void Postfix(List<EnvEntry> __result)
        {
            if (__result == null) return;

            try
            {
                OceanWindEngine.ApplyEnvironmentWeights(__result);
            }
            catch (Exception ex)
            {
                NjororPlugin.Log.LogError($"[Njörðr] Error during weather weight adjustment: {ex}");
            }
        }
    }

    // 3. Hook SpawnSystem to configure Serpent encounter rates
    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake))]
    public static class SpawnSystemSerpentPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SpawnSystem __instance)
        {
            if (__instance == null || __instance.m_spawnLists == null) return;

            try
            {
                if (!ModConfig.EnableSerpentTuning.Value) return;

                foreach (var list in __instance.m_spawnLists)
                {
                    if (list == null || list.m_spawners == null) continue;

                    foreach (var spawner in list.m_spawners)
                    {
                        if (spawner != null && spawner.m_prefab != null && spawner.m_prefab.name.IndexOf("Serpent", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            spawner.m_spawnInterval = ModConfig.SerpentSpawnIntervalSeconds.Value;
                            spawner.m_chance = ModConfig.NighttimeSerpentSpawnChance.Value;
                            spawner.m_spawnAtDay = ModConfig.DaytimeSerpentSpawnChance.Value > 0 || ModConfig.AllowCalmWeatherDaySerpents.Value;

                            NjororPlugin.Log.LogInfo($"[Njörðr] 🐍 Configured Serpent spawner: Interval={spawner.m_spawnInterval}s, NightChance={spawner.m_chance}%, DayChance={ModConfig.DaytimeSerpentSpawnChance.Value}%");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NjororPlugin.Log.LogError($"[Njörðr] Error configuring Serpent spawners: {ex}");
            }
        }
    }
}
