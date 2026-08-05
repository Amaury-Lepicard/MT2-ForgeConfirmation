# ForgeSmartToggle

BepInEx/Harmony mod for Monster Train 2. Adds a third **Hybrid** state to the
in-battle forge toggle (cycles Off → On → Hybrid → Off): behaves like On, but
skips Forge Point cost for unit subtypes on a configurable list. See
[README.md](README.md) for the full feature description and config keys.

## Layout

- [ForgeSmartToggle/Plugin.cs](ForgeSmartToggle/Plugin.cs) — BepInEx entry point, calls `HybridForgePatch.Init` then `harmony.PatchAll()`.
- [ForgeSmartToggle/patches/HybridForgePatch.cs](ForgeSmartToggle/patches/HybridForgePatch.cs) — all the mod's logic: Harmony patches on `ForgePointsUI`, config binding, the Hybrid badge/tint.
- Harmony patches belong in `ForgeSmartToggle/patches/` (one file per concern) per [patches/README.md](ForgeSmartToggle/patches/README.md).

## Build

```sh
dotnet build
```

Copies the DLL to `../../mt2-plugins/ForgeSmartToggle` (see `PluginDeployDir`
in [ForgeSmartToggle.csproj](ForgeSmartToggle/ForgeSmartToggle.csproj)), which
symlinks to the game's `BepInEx/plugins` — build alone is enough to deploy for
in-game testing.

## Decompiled game code

Monster Train 2 decompiled source lives at `~/MT2Mods/mt2-decompiled`. Use it
to look up game types (e.g. `ForgePointsUI`) instead of guessing signatures —
Harmony patches target these classes directly and get silently skipped if a
patched member doesn't exist.
