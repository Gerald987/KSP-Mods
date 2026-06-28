# KSP Mods

Custom Kerbal Space Program mods for improved flight operations.

## Mods

### V1 Callout Mod (`V1CalloutMod.dll`)
Provides audible and visual V1, VR, and V2 takeoff callouts:
- **Audible tones** — different frequencies for V1 (880Hz), VR (660Hz), V2 (440Hz)
- **On-screen display** — bold text + live speed and V-speed info
- **Auto-detects takeoff** — arms when throttle is applied on the runway
- **Calculates V-speeds** from aircraft mass, TWR, and runway altitude
- **5 weight categories** — ultralight through superheavy

### ILS HUD Mod (`ILSHudMod.dll`)
Instrument Landing System heads-up display for approach guidance:
- **Crosshair display** — localizer (horizontal) and glideslope (vertical) deviation indicators
- **7 runways** — KSC 09/27/18/36, Island 09/27, Desert 09
- **Color-coded** — green/yellow/red alignment status
- **Press `]` key** to toggle the HUD on/off in flight

## Installation

1. Copy the `.dll` files from `build/` into your KSP `GameData/` folder
2. Or build from source (see below)

## Building from Source

Requires .NET Framework C# compiler (`csc.exe`):

```bash
csc.exe /target:library /out:build/V1CalloutMod.dll /reference:"KSP_x64_Data/Managed/Assembly-CSharp.dll" /reference:"KSP_x64_Data/Managed/UnityEngine.dll" /reference:"KSP_x64_Data/Managed/UnityEngine.CoreModule.dll" /reference:"KSP_x64_Data/Managed/UnityEngine.IMGUIModule.dll" /reference:"KSP_x64_Data/Managed/UnityEngine.AudioModule.dll" /reference:"KSP_x64_Data/Managed/UnityEngine.TextRenderingModule.dll" /reference:"KSP_x64_Data/Managed/UnityEngine.UI.dll" /reference:"KSP_x64_Data/Managed/UnityEngine.InputLegacyModule.dll" sources/V1CalloutMod/V1Callout.cs
```

## Usage

Launch KSP and start a flight. V1 callouts arm automatically. Press `]` to open the ILS HUD.
