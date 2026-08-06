# ForgeSmartToggle

BepInEx/Harmony mod for Monster Train 2. Adds a third **Hybrid** state to the
in-battle forge toggle (cycles Off → On → Hybrid → Off): behaves like On, but
skips Forge Point cost for unit subtypes on a configurable list. See
[README.md](README.md) for the full feature description and config keys.

## Layout

- [ForgeSmartToggle/Plugin.cs](ForgeSmartToggle/Plugin.cs) — BepInEx entry point, calls `HybridForgePatch.Init` then `harmony.PatchAll()`.
- [ForgeSmartToggle/patches/HybridForgePatch.cs](ForgeSmartToggle/patches/HybridForgePatch.cs) — all the mod's logic, in one file: config binding plus five nested Harmony patches.
  - The Hybrid state is a single mod-side `static bool`, never persisted: `SaveManager.ToggleForgeActive` is prefixed to insert Hybrid into the cycle (returning `false` so the game's own bool stays `true`), and `SaveManager.SetForgeToggleActive` is prefixed to drop back out of it whenever anything sets the toggle directly (new run, replay, undo).
  - `PlayerManager.OnCardPlayedPreEffectsFired` is prefixed to swallow the forge cost for skipped subtypes. Only `fromDirectPlay` calls are considered, and matching runs against `CardState.GetSpawnCharacterData().GetSubtypes()`, so spells and effect-summoned units are untouched.
  - `ForgePointsUI.SetStateForgingActive` / `SetTooltip` are patched for the badge, tint and tooltip. `SetStateForgingActive` only refreshes the tooltip when the underlying bool flips — On → Hybrid doesn't — so the postfix calls the private `RefreshTooltip` itself via `Traverse`.
  - `Matches` and its `SelfCheck` are pure and run at `Init`; extend the self-check when touching the matching rules.
- Harmony patches belong in `ForgeSmartToggle/patches/` (one file per concern) per [patches/README.md](ForgeSmartToggle/patches/README.md).

## Workspace layout

This repo sits alongside the other MT2 mod repos in the parent workspace
folder, which also holds three shared folders. Paths below are relative to
this repo's root; the `.csproj` sits one level deeper, so it spells the same
targets `../../mt2-plugins/...`.

- `../mt2-game` — symlink to the Steam install. The game's own assemblies are
  in `MonsterTrain2_Data/Managed/` (`Assembly-CSharp.dll` is the game code);
  the BepInEx loader, its config, and `LogOutput.log` are under `BepInEx/`.
- `../mt2-plugins` — symlink to `mt2-game/BepInEx/plugins`, the folder the game
  actually loads mods from. Each mod's `PluginDeployDir` targets it, so
  `dotnet build` deploys into the live install with no separate copy step.
- `../mt2-decompiled` — ILSpy output for the game's assemblies
  (`Assembly-CSharp/`, `Assembly-CSharp-firstpass/`, `CommandSystem/`).
  Read-only reference material; never built. See below.

## Build

```sh
set -a; . ./.env; set +a   # exports GITHUB_USER / GH_AUTH_TOKEN
dotnet build
```

`nuget.config` pulls `TrainworksReloaded.Base` and `Conductor` from a private
GitHub Packages feed, expanding `%GITHUB_USER%`/`%GH_AUTH_TOKEN%` from the
**process environment** — `dotnet` does not read `.env` itself, hence the
`set -a` line. Without those vars, restore fails with `401 Unauthorized` /
`NU1301`. There is no offline fallback: both packages exist only on that feed,
so `--source ~/.nuget/packages` helps only once an authenticated restore has
already cached them.

Build copies the DLL to `../../mt2-plugins/ForgeSmartToggle` (see
`PluginDeployDir` in
[ForgeSmartToggle.csproj](ForgeSmartToggle/ForgeSmartToggle.csproj)), which
symlinks into the game's `BepInEx/plugins` — build alone is enough to deploy
for in-game testing.

## Decompiled game code

Monster Train 2 decompiled source lives at `../mt2-decompiled`. Use it
to look up game types (e.g. `ForgePointsUI`) instead of guessing signatures —
Harmony patches target these classes directly and get silently skipped if a
patched member doesn't exist.
