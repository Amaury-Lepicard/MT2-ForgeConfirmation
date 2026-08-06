# ForgeSmartToggle

A Monster Train 2 mod that adds a third **Hybrid** state to the in-battle forge
toggle, so cheap swarm units stop eating your Forge Points.

The toggle now cycles **Off → On → Hybrid → Off**. Hybrid behaves exactly like
On, except that playing a unit whose subtype is on the skip list spends no Forge
Points. Hybrid is marked by tinting the toggle orange and pinning an orange
**H** badge to its corner; the tooltip gains a "(Hybrid)" suffix and lists the
subtypes being skipped.

## Configuration

`BepInEx/config/ForgeSmartToggle.cfg`

| Key | Default | Meaning |
| --- | --- | --- |
| `Hybrid.Enabled` | `true` | Whether the Hybrid state exists at all. When disabled, the toggle stays plain Off → On → Off. |
| `Hybrid.SkippedSubtypes` | `Morsel, Imp, Whelp` | Comma-separated unit subtypes that don't consume Forge Points in Hybrid. Matched case-insensitively as a substring of both the subtype key and its localized name. Leave it empty and Hybrid skips nothing (the tooltip says so). |

## Install

Use a mod manager (r2modman / Thunderstore Mod Manager) and install
`SpecialCircumstances-ForgeSmartToggle`. Dependencies are pulled in
automatically:

- BepInEx 5.4.2100
- Trainworks Reloaded 0.7.2
- Conductor 0.4.1

Manual install: drop `ForgeSmartToggle.dll` into `BepInEx/plugins/`.

## Notes

Only units you play yourself are exempted, and only by the subtypes of the
character they spawn — units summoned by a card's effects, and spell cards, are
unaffected either way.

The third state lives entirely mod-side — the game's own forge toggle stays
`true` while in Hybrid, so save data and replays are unaffected. Anything that
sets the toggle directly (new run, replay, undo) falls back to plain On/Off.

## Build

```sh
dotnet build
```

The output DLL is copied to `../../mt2-plugins/ForgeSmartToggle` after every
build (`PluginDeployDir` in [ForgeSmartToggle.csproj](ForgeSmartToggle/ForgeSmartToggle.csproj)) —
point that at your `BepInEx/plugins` folder to test in-game directly.

Harmony patches live in [ForgeSmartToggle/patches/](ForgeSmartToggle/patches/);
the forge logic is [HybridForgePatch.cs](ForgeSmartToggle/patches/HybridForgePatch.cs).

Publishing to Thunderstore is driven by [thunderstore.toml](thunderstore.toml)
via `tcli`; the GitHub workflows in [.github/workflows/](.github/workflows/) build and validate the package.

## License

See [LICENSE](LICENSE).
