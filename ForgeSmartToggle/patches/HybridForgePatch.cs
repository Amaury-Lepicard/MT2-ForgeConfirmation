using System.Collections;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeSmartToggle.Patches
{
    // Adds a third "Hybrid" state to the in-battle forge toggle. The button now cycles
    // Off -> On -> Hybrid -> Off. Hybrid forges as normal except for the cheap swarm units
    // listed in BepInEx/config/ForgeSmartToggle.cfg.
    internal static class HybridForgePatch
    {
        private static ConfigEntry<bool> enabled = null!;
        private static ConfigEntry<string> skippedSubtypes = null!;

        // The third state lives entirely mod-side: the game's own bool toggle stays "on"
        // while we're in Hybrid, so save data / replays keep working unchanged.
        private static bool hybrid;

        private static Color? originalTint;

        private static TextMeshProUGUI? hybridBadge;

        private static readonly Color HybridTint = new(1f, 0.69f, 0.13f);

        public static void Init(ConfigFile config)
        {
            enabled = config.Bind("Hybrid", "Enabled", true,
                "Enable the Hybrid forge state. When disabled, the toggle stays plain Off -> On -> Off.");

            skippedSubtypes = config.Bind("Hybrid", "SkippedSubtypes", "Morsel, Imp, Whelp",
                "Comma-separated unit subtypes that do NOT consume Forge Points while the forge " +
                "toggle is in its Hybrid state. Matched case-insensitively as a substring of both " +
                "the subtype key and its localized name.");

            SelfCheck();
        }

        internal static bool Matches(string subtypeKey, string localizedName, string configuredList)
        {
            foreach (var entry in configuredList.Split(','))
            {
                var needle = entry.Trim();
                if (needle.Length == 0)
                {
                    continue;
                }

                if (subtypeKey.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0
                    || localizedName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SelfCheck()
        {
            const string list = "Morsel, Imp, Whelp";
            if (!Matches("SubtypeData_Morsel", "Morsel", list)
                || !Matches("SubtypeData_Imp", "Imp", list)
                || !Matches("whatever", "Whelp", list)
                || Matches("SubtypeData_Dragon", "Dragon", list)
                || Matches("SubtypeData_Imp", "Imp", " , "))
            {
                throw new InvalidOperationException("HybridForgePatch.Matches self-check failed");
            }
        }

        private static bool IsSkipped(CardState card)
        {
            var subtypes = card.GetSpawnCharacterData()?.GetSubtypes();
            if (subtypes == null)
            {
                return false;
            }

            foreach (var subtype in subtypes)
            {
                if (Matches(subtype.Key ?? string.Empty, subtype.LocalizedName ?? string.Empty, skippedSubtypes.Value))
                {
                    return true;
                }
            }

            return false;
        }

        // Off -> On -> Hybrid -> Off.
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.ToggleForgeActive))]
        private static class Cycle
        {
            private static bool Prefix(SaveManager __instance)
            {
                if (!enabled.Value)
                {
                    hybrid = false;
                    return true;
                }

                if (__instance.IsForgeToggleActive() && !hybrid)
                {
                    hybrid = true;
                    return false; // stay "active", only the mod-side state changes
                }

                hybrid = false;
                return true;
            }
        }

        // A new run, a replay or an undo sets the toggle directly - fall back to plain On/Off.
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SetForgeToggleActive))]
        private static class ClearOnDirectSet
        {
            private static void Prefix(SaveManager __instance, bool isActive)
            {
                if (__instance.IsForgeToggleActive() != isActive)
                {
                    hybrid = false;
                }
            }
        }

        // This method only ever applies forge points, so skipping it wholesale is safe.
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.OnCardPlayedPreEffectsFired))]
        private static class SkipForgeOnPlay
        {
            private static bool Prefix(CardState cardState, bool fromDirectPlay, ref IEnumerator __result)
            {
                if (!hybrid || !fromDirectPlay || cardState == null || !IsSkipped(cardState))
                {
                    return true;
                }

                Plugin.Logger.LogDebug($"Hybrid forge: skipping '{cardState.GetTitle()}'");
                __result = Nothing();
                return false;
            }

            private static IEnumerator Nothing()
            {
                yield break;
            }
        }

        private static string SkippedList()
        {
            var names = skippedSubtypes.Value.Split(',').Select(entry => entry.Trim()).Where(entry => entry.Length > 0);
            return string.Join(", ", names);
        }

        // An "H" pinned to the corner of the toggle, cloned from the forge point counter so it
        // picks up the game's font and material for free.
        private static void RefreshBadge(ForgePointsUI ui, GameObject toggle)
        {
            if (hybridBadge == null)
            {
                var label = Traverse.Create(ui).Field<TextMeshProUGUI>("countLabel").Value;
                if (label == null)
                {
                    return; // ponytail: no font to borrow, the tint below is still a marker
                }

                hybridBadge = UnityEngine.Object.Instantiate(label, toggle.transform);
                hybridBadge.name = "HybridBadge";
                hybridBadge.text = "H";
                hybridBadge.fontSize = label.fontSize * 0.65f;
                hybridBadge.color = HybridTint;
                hybridBadge.alignment = TextAlignmentOptions.Center;
                hybridBadge.raycastTarget = false;

                var rect = hybridBadge.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f); // bottom-right of the toggle
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(28f, 28f);
                rect.localScale = Vector3.one;
            }

            hybridBadge.gameObject.SetActive(hybrid);
        }

        // Hybrid has to look different from On, otherwise the extra state is invisible.
        [HarmonyPatch(typeof(ForgePointsUI), nameof(ForgePointsUI.SetStateForgingActive))]
        private static class MarkToggle
        {
            private static void Postfix(ForgePointsUI __instance)
            {
                var toggle = Traverse.Create(__instance).Field<GameObject>("forgingActiveToggle").Value;
                if (toggle == null)
                {
                    return;
                }

                RefreshBadge(__instance, toggle);

                var graphic = toggle.GetComponentInChildren<Graphic>(true);
                if (graphic != null)
                {
                    originalTint ??= graphic.color;
                    graphic.color = hybrid ? HybridTint : originalTint.Value;
                }

                // SetStateForgingActive only refreshes the tooltip when the bool flips, and
                // On -> Hybrid doesn't flip it.
                Traverse.Create(__instance).Method("RefreshTooltip").GetValue();
            }
        }

        [HarmonyPatch(typeof(ForgePointsUI), "SetTooltip")]
        private static class TooltipNote
        {
            private static void Prefix(ref string title, ref string body)
            {
                if (!hybrid)
                {
                    return;
                }

                title += " (Hybrid)";

                var list = SkippedList();
                body += list.Length > 0
                    ? $"\n\n<b>Hybrid forging</b> (the <color=#FFB021>H</color> on the button): Forge Points are spent as usual, " +
                      $"except on these units, which are ignored:\n<color=#FFB021>{list}</color>"
                    : "\n\n<b>Hybrid forging</b>: no units are ignored - the ignore list in " +
                      "BepInEx/config/ForgeSmartToggle.cfg is empty.";
            }
        }
    }
}
