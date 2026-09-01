using System;
using System.Collections.Generic;
using Njoror.Configuration;
using UnityEngine;

namespace Njoror.Logic
{
    public static class OceanWindEngine
    {
        private static readonly System.Random _rng = new System.Random();
        private static Vector3 _timedWindDirection;
        private static bool _hasTimedWindDirection;
        private static float _nextTimedCheckAt;

        /// <summary>
        /// Evaluates whether the calculated wind angle creates a dead headwind against active sailing ships.
        /// If so, shifts the wind angle into a favorable crosswind/tailwind according to the configured mitigation percentage.
        /// </summary>
        public static Vector3 AdjustWindForSailing(Vector3 baseWindDir)
        {
            NjororPlugin.LogDebug("[Njoror][Diag] AdjustWindForSailing entered.");
            if (!ModConfig.EnableFairWinds.Value)
            {
                NjororPlugin.LogDebug("[Njoror][Diag] Fair winds skipped: disabled by configuration.");
                return baseWindDir;
            }

            if (!SailingStateNetwork.TryGetActiveSailingState(out Vector3 shipForward, out bool activeShipInOcean))
            {
                NjororPlugin.LogDebug("[Njoror][Diag] Fair winds skipped: no fresh client sailing-state report.");
                return baseWindDir;
            }

            // Calculate angle between ship heading forward and wind direction
            Vector3 windNormalized = baseWindDir;
            windNormalized.y = 0;
            windNormalized.Normalize();

            // Dot product = 1 (tailwind), -1 (headwind), 0 (crosswind)
            float dot = Vector3.Dot(shipForward, windNormalized);

            // If ocean mode is forced tailwind
            if (ModConfig.AlwaysTailwindInOcean.Value)
            {
                if (activeShipInOcean)
                {
                    NjororPlugin.LogDebug("[Njoror][Diag] Ocean tailwind applied.");
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
                    NjororPlugin.Log.LogInfo($"[Njoror] Headwind deflected; roll={roll:F2}.");
                    return deflectedWind.normalized;
                }

                NjororPlugin.LogDebug($"[Njoror][Diag] Headwind retained; roll={roll:F2}.");
            }

            NjororPlugin.LogDebug("[Njoror][Diag] Fair winds retained the base direction.");
            return baseWindDir;
        }

        public static float AdjustWindIntensity(float baseIntensity)
        {
            if (!ModConfig.EnableFairWinds.Value)
                return baseIntensity;

            float minMulti = ModConfig.MinimumWindSpeedMultiplier.Value;
            float adjustedIntensity = Mathf.Max(baseIntensity * minMulti, 0.25f);
            return adjustedIntensity;
        }

        public static bool TryGetTimedWindOverride(Vector3 baseWindDirection, out Vector3 windDirection, out bool decisionMade)
        {
            windDirection = baseWindDirection;
            decisionMade = false;

            if (ModConfig.CheckDeflectOnWindChange.Value || ModConfig.CheckDeflectTimeSeconds.Value <= 0)
            {
                _hasTimedWindDirection = false;
                return false;
            }

            if (Time.unscaledTime >= _nextTimedCheckAt)
            {
                _nextTimedCheckAt = Time.unscaledTime + ModConfig.CheckDeflectTimeSeconds.Value;
                _timedWindDirection = AdjustWindForSailing(baseWindDirection);
                _hasTimedWindDirection = true;
                decisionMade = true;
                NjororPlugin.LogDebug($"[Njoror][Diag] Timed fair-wind check completed; next check in {ModConfig.CheckDeflectTimeSeconds.Value}s.");
            }

            if (!_hasTimedWindDirection)
                return false;

            windDirection = _timedWindDirection;
            return true;
        }

        public static void ApplyEnvironmentWeights(List<EnvEntry> environments)
        {
            NjororPlugin.LogDebug("[Njoror][Diag] ApplyEnvironmentWeights entered.");
            if (!ModConfig.EnableWeatherTuning.Value)
            {
                NjororPlugin.LogDebug("[Njoror][Diag] Environment tuning skipped: disabled by configuration.");
                return;
            }

            if (environments == null)
            {
                NjororPlugin.LogDebug("[Njoror][Diag] Environment tuning skipped: environment list was null.");
                return;
            }

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

            NjororPlugin.LogDebug($"[Njoror][Diag] Environment tuning processed {environments.Count} entry(s).");
        }
    }
}
