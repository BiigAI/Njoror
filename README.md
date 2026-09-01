# Njoror_FairWinds

> **Njoror_FairWinds is a lightweight mod designed to improve sailing with fair winds, ocean weather modulation, and sea serpent encounter controls.**

### About the Name: *Njörðr*
In Norse mythology, **Njörðr** (*Njord*) is the revered Vanir god of the wind, the sea, and prosperous seafaring voyages, prayed to by Vikings for favorable winds and calm waters. In this mod, *Njörðr* watches over your fleet—deflecting frustrating headwinds into fair sailing breezes, balancing oceanic storms, and regulating sea serpents.

---

## Features
- **Fair Winds & Headwind Mitigation**: Intelligently deflects dead-ahead headwinds into tackable crosswinds or broad reaches while sailing.
- **Minimum Wind Speed Scaling**: Prevents dead calms from stalling voyages by guaranteeing a steady baseline sailing breeze.
- **Ocean Weather Tuning**: Dynamically modulates the frequency and duration of thunderstorms, rain, and clear skies.
- **Sea Serpent Controls**: Customizes encounter rates, spawn timers, and daytime serpent availability.

---

### Installation Type
- **Location:** Must be installed on both the Server and the Client.
- **Enforcement:** Client versions must match the server version.

### Manual Install
1. Ensure BepInEx and Jotunn are installed.
2. Extract the downloaded `.zip` archive.
3. Copy `Njoror_FairWinds.dll` into your `Valheim/BepInEx/plugins/` folder.
4. Launch the game once to generate the default configuration file.

---

## Configuration
The configuration file is automatically created at `BepInEx/config/com.bigai.njoror_fairwinds.cfg` after running the game once.

| Section | Setting | Default | Description |
| :--- | :--- | :--- | :--- |
| `1 - Fair Winds` | `EnableFairWinds` | `true` | Enable fair winds while sailing aboard a ship. |
| `1 - Fair Winds` | `HeadwindMitigationPercent` | `60.0` | Chance (%) to deflect a direct dead-ahead headwind into a tackable crosswind. |
| `1 - Fair Winds` | `MinimumWindSpeedMultiplier` | `1.0` | Minimum wind velocity scaling factor (>1.0 ensures a steady minimum breeze). |
| `1 - Fair Winds` | `AlwaysTailwindInOcean` | `false` | If enabled, ships sailing in the Ocean biome strictly receive tailwinds or broad reaches. |
| `1 - Fair Winds` | `CheckDeflectOnWindChange` | `true` | Evaluate fair-wind deflection only when Valheim selects a new wind target. |
| `1 - Fair Winds` | `CheckDeflectTimeSeconds` | `0` | When wind-change checks are disabled, re-evaluate fair winds at this interval (0 to disable). |
| `2 - Weather & Storms` | `EnableWeatherTuning` | `true` | Enable dynamic modulation of ocean storms and atmospheric weather conditions. |
| `2 - Weather & Storms` | `StormFrequencyMultiplier` | `1.0` | Multiplier for the frequency and duration of ThunderStorms (1.0 = Vanilla). |
| `2 - Weather & Storms` | `RainFrequencyMultiplier` | `1.0` | Multiplier for regular rain and overcast conditions (1.0 = Vanilla). |
| `2 - Weather & Storms` | `ClearWeatherFrequencyMultiplier` | `1.0` | Multiplier for clear, sunny weather frequency (>1.0 for more clear days). |
| `3 - Sea Serpents` | `EnableSerpentTuning` | `true` | Enable custom encounter rate tuning for Ocean Sea Serpents. |
| `3 - Sea Serpents` | `DaytimeSerpentSpawnChance` | `0.0` | Base chance (%) for a Sea Serpent to spawn during clear/calm Day conditions. |
| `3 - Sea Serpents` | `NighttimeSerpentSpawnChance` | `5.0` | Base chance (%) for a Sea Serpent to spawn during Night hours. |
| `3 - Sea Serpents` | `SerpentSpawnIntervalSeconds` | `1000.0` | Time interval (seconds) between Ocean spawner serpent spawn checks. |
| `3 - Sea Serpents` | `AllowCalmWeatherDaySerpents` | `false` | If true, Sea Serpents can spawn during daytime even when it is not storming. |
| `4 - Diagnostics` | `EnableDebugLogging` | `false` | Enable detailed diagnostic logs in the BepInEx console. |

---

## Controls & Commands
- **Keybinds:** None.
- **Admin Commands:** None.

---

## Compatibility & Safe Removal
- **Multiplayer:** Must be installed on both server and clients with Jotunn.
- **Save Integrity:** Safe to add or remove mid-playthrough without affecting existing world or character saves.

### AI Disclosure 

I made this mod using AI. Most of the code in this mod was AI generated. If you have an issue with this, I completely understand and urge you to not use this mod. This mod ("Njoror_FairWinds") is meant as a lightweight mod for small servers that don't need all the bells and whistles of a more complex mod.
