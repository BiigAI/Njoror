using System;
using System.Collections.Generic;
using Njoror.Configuration;
using UnityEngine;

namespace Njoror.Logic
{
    public static class OceanWindEngine
    {
        private static readonly System.Random _rng = new System.Random();

        /// <summary>
        /// Evaluates whether the calculated wind angle creates a dead headwind against active sailing ships.
        /// If so, shifts the wind angle into a favorable crosswind/tailwind according to the configured mitigation percentage.
        /// </summary>
        public static Vector3 AdjustWindForSailing(Vector3 baseWindDir, float baseWindAngle)
        {
            if (!ModConfig.EnableFairWinds.Value)
                return baseWindDir;

            if (Player.s_players == null || Player.s_players.Count == 0)
                return baseWindDir;

            // Check if any player is actively piloting/sitting in a ship
            Ship? activeShip = null;
            foreach (var p in Player.s_players)
            {
                if (p != null && !p.IsDead())
                {
                    Ship standingShip = p.GetStandingOnShip();
                    if (standingShip != null)
                    {
                        activeShip = standingShip;
                        break;
                    }
                }
            }

            if (activeShip == null)
                return baseWindDir;

            // Calculate angle between ship heading forward and wind direction
            Vector3 shipForward = activeShip.transform.forward;
            shipForward.y = 0;
            shipForward.Normalize();

            Vector3 windNormalized = baseWindDir;
            windNormalized.y = 0;
            windNormalized.Normalize();

            // Dot product = 1 (tailwind), -1 (headwind), 0 (crosswind)
            float dot = Vector3.Dot(shipForward, windNormalized);

            // If ocean mode is forced tailwind
            if (ModConfig.AlwaysTailwindInOcean.Value)
            {
                Heightmap.Biome currentBiome = WorldGenerator.instance != null 
                    ? WorldGenerator.instance.GetBiome(activeShip.transform.position.x, activeShip.transform.position.z) 
                    : Heightmap.Biome.Ocean;

                if (currentBiome == Heightmap.Biome.Ocean)
                {
                    return shipForward;
                }
            }

            // Headwind detection: dot < -0.7 means within 45 degrees of direct headwind
            if (dot < -0.6f)
            {
                double roll = _rng.NextDouble() * 100.0;
                if (roll < ModConfig.HeadwindMitigationPercent.Value)
                {
                    // Deflect wind into a 65-degree crosswind (tackable and fast)
                    float deflectSign = _rng.Next(2) == 0 ? 1f : -1f;
                    Quaternion rotation = Quaternion.Euler(0f, deflectSign * 80f, 0f);
                    Vector3 deflectedWind = rotation * shipForward;
                    deflectedWind.y = 0;
                    return deflectedWind.normalized;
                }
            }

            return baseWindDir;
        }

        public static float AdjustWindIntensity(float baseIntensity)
        {
            if (!ModConfig.EnableFairWinds.Value)
                return baseIntensity;

            float minMulti = ModConfig.MinimumWindSpeedMultiplier.Value;
            return Mathf.Max(baseIntensity * minMulti, 0.25f);
        }

        public static void ApplyEnvironmentWeights(List<EnvEntry> environments)
        {
            if (!ModConfig.EnableWeatherTuning.Value || environments == null)
                return;

            foreach (var entry in environments)
            {
                if (entry == null || entry.m_env == null) continue;

                string name = entry.m_env.m_name.ToLower();

                if (name.Contains("thunder") || name.Contains("storm"))
                {
                    entry.m_weight *= ModConfig.StormFrequencyMultiplier.Value;
                }
                else if (name.Contains("rain") || name.Contains("fog"))
                {
                    entry.m_weight *= ModConfig.RainFrequencyMultiplier.Value;
                }
                else if (name.Contains("clear") || name.Contains("sun"))
                {
                    entry.m_weight *= ModConfig.ClearWeatherFrequencyMultiplier.Value;
                }
            }
        }
    }
}
