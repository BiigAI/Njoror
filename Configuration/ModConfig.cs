using System;
using BepInEx.Configuration;

namespace Njoror.Configuration
{
    public static class ModConfig
    {
        // ── Section 1: Fair Winds & Headwind Mitigation ────────────────────────
        public static ConfigEntry<bool> EnableFairWinds { get; private set; } = null!;
        public static ConfigEntry<float> HeadwindMitigationPercent { get; private set; } = null!;
        public static ConfigEntry<float> MinimumWindSpeedMultiplier { get; private set; } = null!;
        public static ConfigEntry<bool> AlwaysTailwindInOcean { get; private set; } = null!;

        // ── Section 2: Ocean Weather & Storms ──────────────────────────────────
        public static ConfigEntry<bool> EnableWeatherTuning { get; private set; } = null!;
        public static ConfigEntry<float> StormFrequencyMultiplier { get; private set; } = null!;
        public static ConfigEntry<float> RainFrequencyMultiplier { get; private set; } = null!;
        public static ConfigEntry<float> ClearWeatherFrequencyMultiplier { get; private set; } = null!;

        // ── Section 3: Sea Serpent Encounters ──────────────────────────────────
        public static ConfigEntry<bool> EnableSerpentTuning { get; private set; } = null!;
        public static ConfigEntry<float> DaytimeSerpentSpawnChance { get; private set; } = null!;
        public static ConfigEntry<float> NighttimeSerpentSpawnChance { get; private set; } = null!;
        public static ConfigEntry<float> SerpentSpawnIntervalSeconds { get; private set; } = null!;
        public static ConfigEntry<bool> AllowCalmWeatherDaySerpents { get; private set; } = null!;

        public static void Initialize(ConfigFile config)
        {
            // ── Section 1: Fair Winds ──────────────────────────────────────────
            EnableFairWinds = config.Bind(
                "1 - Fair Winds",
                "EnableFairWinds",
                true,
                "Enable server-authoritative fair winds algorithm to bias against strict headwinds while sailing."
            );

            HeadwindMitigationPercent = config.Bind(
                "1 - Fair Winds",
                "HeadwindMitigationPercent",
                60.0f,
                new ConfigDescription(
                    "Chance % to deflect a direct dead-ahead headwind into a tackable crosswind/quarter wind when sailing.",
                    new AcceptableValueRange<float>(0f, 100f)
                )
            );

            MinimumWindSpeedMultiplier = config.Bind(
                "1 - Fair Winds",
                "MinimumWindSpeedMultiplier",
                1.0f,
                new ConfigDescription(
                    "Minimum wind velocity scaling factor (1.0 = Vanilla, >1.0 ensures ships always have at least moderate breeze).",
                    new AcceptableValueRange<float>(0.5f, 2.5f)
                )
            );

            AlwaysTailwindInOcean = config.Bind(
                "1 - Fair Winds",
                "AlwaysTailwindInOcean",
                false,
                "If enabled, players sailing in the deep Ocean biome will strictly receive tailwinds or broad reaches."
            );

            // ── Section 2: Weather & Storms ────────────────────────────────────
            EnableWeatherTuning = config.Bind(
                "2 - Weather & Storms",
                "EnableWeatherTuning",
                true,
                "Enable dynamic modulation of ocean storms and atmospheric weather conditions."
            );

            StormFrequencyMultiplier = config.Bind(
                "2 - Weather & Storms",
                "StormFrequencyMultiplier",
                1.0f,
                new ConfigDescription(
                    "Multiplier for the frequency and duration of ThunderStorms (1.0 = Vanilla, 0.5 = Calmer, 2.0 = Violent).",
                    new AcceptableValueRange<float>(0.1f, 3.0f)
                )
            );

            RainFrequencyMultiplier = config.Bind(
                "2 - Weather & Storms",
                "RainFrequencyMultiplier",
                1.0f,
                new ConfigDescription(
                    "Multiplier for regular rain and overcast conditions (1.0 = Vanilla).",
                    new AcceptableValueRange<float>(0.1f, 3.0f)
                )
            );

            ClearWeatherFrequencyMultiplier = config.Bind(
                "2 - Weather & Storms",
                "ClearWeatherFrequencyMultiplier",
                1.0f,
                new ConfigDescription(
                    "Multiplier for clear, sunny weather frequency (1.0 = Vanilla, >1.0 for more clear sailing days).",
                    new AcceptableValueRange<float>(0.5f, 3.0f)
                )
            );

            // ── Section 3: Sea Serpents ────────────────────────────────────────
            EnableSerpentTuning = config.Bind(
                "3 - Sea Serpents",
                "EnableSerpentTuning",
                true,
                "Enable custom encounter rate tuning for Ocean Sea Serpents."
            );

            DaytimeSerpentSpawnChance = config.Bind(
                "3 - Sea Serpents",
                "DaytimeSerpentSpawnChance",
                0.0f,
                new ConfigDescription(
                    "Base chance % for a Sea Serpent to spawn during clear/calm Day conditions. (Vanilla default is 0.0% / storms only).",
                    new AcceptableValueRange<float>(0f, 30f)
                )
            );

            NighttimeSerpentSpawnChance = config.Bind(
                "3 - Sea Serpents",
                "NighttimeSerpentSpawnChance",
                5.0f,
                new ConfigDescription(
                    "Base chance % for a Sea Serpent to spawn during Night hours. (Vanilla default is 5.0%).",
                    new AcceptableValueRange<float>(1f, 50f)
                )
            );

            SerpentSpawnIntervalSeconds = config.Bind(
                "3 - Sea Serpents",
                "SerpentSpawnIntervalSeconds",
                1000.0f,
                new ConfigDescription(
                    "Time interval (seconds) between Ocean spawner serpent spawn checks. (Vanilla default is 1000.0s).",
                    new AcceptableValueRange<float>(100f, 2000f)
                )
            );

            AllowCalmWeatherDaySerpents = config.Bind(
                "3 - Sea Serpents",
                "AllowCalmWeatherDaySerpents",
                false,
                "If true, Sea Serpents can spawn during daytime even when it is not storming or raining."
            );
        }
    }
}
