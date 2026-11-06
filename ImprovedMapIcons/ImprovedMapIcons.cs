using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ImprovedMapIcons
{
    /// <summary>
    /// Two things the map legend is missing.
    ///
    /// The question-mark pins - Hildir's three quest sites, and the locations an Ashlands vegvisir
    /// reveals - are the only pins the game gives no toggle for. They can be crossed off or
    /// deleted, but not hidden, so a map you want to read keeps them whether you like it or not.
    /// This adds the button the legend never had.
    ///
    /// And the icon toggles do not keep. Minimap.Awake fills the visibility array with true and
    /// nothing ever writes it to disk, so every icon is back on at the start of every session
    /// however you left it. This sets the ones you want at load.
    ///
    /// Everything here is client-side. It reads and writes the local player's own map UI, nothing
    /// is sent anywhere, and nobody else sees a difference - so it works on a server without the
    /// server or anyone else having it installed.
    /// </summary>
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInProcess("valheim.exe")]
    public class ImprovedMapIcons : BaseUnityPlugin
    {
        // Module info
        private const string modGUID = "Pip.ImprovedMapIcons";
        private const string modName = "Pip's Improved Map Icons";
        private const string modVersion = "1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);

        // Config file entries
        private static ConfigEntry<bool> QuestPinToggle;
        private static ConfigEntry<string> QuestPinTypes;
        private static ConfigEntry<float> QuestPinLegendNudge;
        private static ConfigEntry<string> DefaultVisiblePins;
        private static ConfigEntry<bool> VerboseLogging;

        private void Awake()
        {
            Game.isModded = true;

            QuestPinToggle = Config.Bind("Quest icon", "AddQuestIcon", true,
                "Add a question-mark button to the map legend, below the boss icon, so quest pins can be shown and hidden like the death and boss pins. "
                + "Right-click toggles them. Left-click toggles on a double-tap and does nothing on a single press, the same as the two buttons above it.");
            QuestPinTypes = Config.Bind("Quest icon", "QuestPinTypes", "Hildir1,Hildir2,Hildir3",
                "Which pin types the question-mark button governs. Comma separated names or numbers. The default is the three Hildir types, and is almost certainly what you want.");
            QuestPinLegendNudge = Config.Bind("Quest icon", "LegendNudge", 0f, new ConfigDescription(
                "Extra pixels to lift the top group of map icons by. Positive moves it up, negative down. Only needed if the spacing does not come out right on your UI scale.",
                new AcceptableValueRange<float>(-200f, 200f)));
            DefaultVisiblePins = Config.Bind("Startup", "DefaultVisiblePins", "Icon1,Icon4",
                "Which map icons start switched on. Everything else starts off. Valheim switches them all back on every time it loads a world, so without this the same handful has to be switched off by hand at the start of every session. "
                + "Comma separated names or numbers: Icon0 campfire, Icon1 house, Icon2 hammer, Icon3 plain dot, Icon4 portal, plus Death, Boss, and Hildir1, Hildir2 and Hildir3 for the question marks. "
                + "Use the names - Icon4 is 6, and 4 is Death. "
                + "Beds, other players, pings, shouts and event markers are never touched. Leave this empty to let the game start the way it always has.");
            VerboseLogging = Config.Bind("Logging", "VerboseLogging", false,
                "Log every map icon switched on or off, with its name, number and sprite. The quickest way to find the name a setting wants: open the map and click an icon.");

            RebuildDerivedConfig();
            // A config manager edits settings live, and both lists are parsed once rather than per
            // frame - so the caches have to be rebuilt when one changes, not just at startup
            Config.SettingChanged += delegate { RebuildDerivedConfig(); };

            harmony.PatchAll();
        }

        private void Update()
        {
            UpdateQuestPinToggle();
            ApplyDefaultPinVisibility();
        }

        /// <summary>
        /// Re-derives everything parsed out of a config string. Dropping the button and clearing
        /// the applied-to marker makes a settings change take effect on the next frame rather than
        /// at the next world load.
        /// </summary>
        private static void RebuildDerivedConfig()
        {
            _questPinTypes = ParsePinTypes(QuestPinTypes.Value, "QuestPinTypes");
            _defaultVisiblePins = ParsePinTypes(DefaultVisiblePins.Value, "DefaultVisiblePins");
            _defaultPinsMap = null;
            _questIconTableLogged = false;
            _questLegendLogged = false;
            DropQuestPinToggle();
        }

        // Non-public in the shipped assembly even though a publicized reference declares it
        // "public bool[]", so it is reached through a handle rather than named directly
        private static readonly FieldInfo VisibleIconTypesField =
            AccessTools.Field(typeof(Minimap), "m_visibleIconTypes");

        // ---------------- Quest pin toggle ----------------
        // The map legend's two groups are both built the same way. Each button is a GameObject
        // carrying the icon Image, with the "selected" highlight as a child - which is what the
        // public m_selectedIconDeath and m_selectedIconBoss fields point at, and why vanilla's own
        // ToggleIconFilter recolours selectedIcon.Value.transform.parent rather than the Image it
        // holds. That gives a reliable way in without knowing a single object name: the highlight's
        // parent is the button, and the button's parent is the group.
        //
        // So the button here is a clone of the boss button, dropped into the same group one sibling
        // further down. Cloning rather than building a RectTransform from scratch is what makes it
        // match: the sprite anchoring, the hover tint, the highlight child and whatever styling the
        // prefab carries all come along, and they keep matching if Iron Gate restyles the legend.
        //
        // What does not come along is behaviour. The clone arrives wired to the boss icon's
        // handlers, so every delegate is cleared before anything is attached: a copy made for
        // display has to have its behaviour stripped or it quietly goes on acting as the original. The delegates are cleared by reflecting over
        // UIInputHandler's Action fields rather than naming them, so a field added later is cleared
        // too instead of being left live on a button that looks inert.
        //
        // The type is not registered in Minimap.m_selectedIcons. Vanilla iterates that dictionary
        // for gamepad D-pad cycling and for SelectIcon, and a quest pin cannot be placed by hand -
        // adding it there would put a non-placeable icon into the selection cycle. The cost is that
        // vanilla's recolour loop never touches this button, so its tint is maintained here.
        private static readonly FieldInfo PinUpdateRequiredField =
            AccessTools.Field(typeof(Minimap), "m_pinUpdateRequired");

        // Clearing the handlers by reflection rather than by name: every one of these is public in
        // the shipped assembly today, but naming eleven fields to clear is eleven chances to miss
        // one that gets added later, and a missed one leaves the boss icon's behaviour attached.
        private static readonly FieldInfo[] InputHandlerActionFields = typeof(UIInputHandler)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.FieldType == typeof(Action<UIInputHandler>))
            .ToArray();

        private static List<Minimap.PinType> _questPinTypes = new List<Minimap.PinType>();
        private static Minimap _questToggleMap;
        private static GameObject _questToggleButton;
        private static Image _questToggleImage;
        private static bool _questIconTableLogged;
        private static bool _questLegendLogged;

        /// <summary>
        /// Reads one of the configured type lists. Accepts names or numbers, because the two
        /// spellings answer different questions - a name is readable, a number is what someone
        /// reading the enum will have - and names are the safer of the two, since the numbers are
        /// not in legend order and Icon4 is 6 while 4 is Death.
        /// </summary>
        private static List<Minimap.PinType> ParsePinTypes(string raw, string setting)
        {
            var types = new List<Minimap.PinType>();
            foreach (string token in (raw ?? string.Empty).Split(','))
            {
                string name = token.Trim();
                if (name.Length == 0)
                {
                    continue;
                }
                Minimap.PinType parsed;
                if (int.TryParse(name, out int index))
                {
                    if (!Enum.IsDefined(typeof(Minimap.PinType), index))
                    {
                        LogWarn($"{setting}: {index} is not a Minimap.PinType, ignoring it");
                        continue;
                    }
                    parsed = (Minimap.PinType)index;
                }
                else
                {
                    try
                    {
                        parsed = (Minimap.PinType)Enum.Parse(typeof(Minimap.PinType), name, true);
                    }
                    catch (ArgumentException)
                    {
                        LogWarn($"{setting}: '{name}' is not a Minimap.PinType, ignoring it");
                        continue;
                    }
                }
                if (!types.Contains(parsed))
                {
                    types.Add(parsed);
                }
            }
            return types;
        }

        private static void DropQuestPinToggle()
        {
            if (_questToggleButton != null)
            {
                UnityEngine.Object.Destroy(_questToggleButton);
            }
            _questToggleButton = null;
            _questToggleImage = null;
            _questToggleMap = null;
        }

        private static void UpdateQuestPinToggle()
        {
            Minimap map = Minimap.instance;
            if (map == null)
            {
                // The map object is rebuilt per world, so the handles have to go with it
                _questToggleButton = null;
                _questToggleImage = null;
                _questToggleMap = null;
                return;
            }
            if (QuestPinToggle == null || !QuestPinToggle.Value || _questPinTypes.Count == 0)
            {
                if (_questToggleButton != null)
                {
                    DropQuestPinToggle();
                }
                return;
            }
            // The Unity null check matters on the second half: the button is destroyed with the map
            if (ReferenceEquals(map, _questToggleMap) && _questToggleButton != null)
            {
                return;
            }
            _questToggleMap = map;
            BuildQuestPinToggle(map);
        }

        private static void BuildQuestPinToggle(Minimap map)
        {
            LogPinIconTable(map);

            Image anchor = map.m_selectedIconBoss != null ? map.m_selectedIconBoss : map.m_selectedIconDeath;
            Transform model = anchor != null ? anchor.transform.parent : null;
            Transform group = model != null ? model.parent : null;
            if (group == null)
            {
                LogWarn("Quest pin toggle: the map legend has no boss or death button to copy, "
                    + "so no button was added");
                return;
            }

            LogLegendHierarchy(group);

            // The distance between the two existing buttons is the one measurement everything else
            // is built on - where the new button goes, and how much taller the panel has to be.
            RectTransform deathRect = map.m_selectedIconDeath != null
                ? map.m_selectedIconDeath.transform.parent as RectTransform : null;
            RectTransform bossRect = map.m_selectedIconBoss != null
                ? map.m_selectedIconBoss.transform.parent as RectTransform : null;
            Vector2 stepVector = deathRect != null && bossRect != null && deathRect != bossRect
                ? bossRect.anchoredPosition - deathRect.anchoredPosition
                : Vector2.zero;

            GameObject clone = UnityEngine.Object.Instantiate(model.gameObject, group, false);
            clone.name = "PipsModQuestPinToggle";
            clone.transform.SetSiblingIndex(model.GetSiblingIndex() + 1);
            _questToggleButton = clone;

            // A layout group places the clone itself. Without one the positions are authored, and
            // the clone would land exactly on top of the button it was copied from - so the step
            // between the two existing buttons is applied once more.
            bool autoLayout = group.GetComponent<LayoutGroup>() != null;
            if (!autoLayout)
            {
                RectTransform cloneRect = clone.transform as RectTransform;
                RectTransform modelRect = model as RectTransform;
                if (cloneRect != null && modelRect != null && stepVector != Vector2.zero)
                {
                    cloneRect.anchoredPosition = modelRect.anchoredPosition + stepVector;
                }
                else
                {
                    LogWarn("Quest pin toggle: the legend has no layout group and the spacing "
                        + "could not be measured, so the new button may sit on top of another");
                }
            }

            StripClonedBehaviour(clone);
            ResizeLegendPanel(group, Mathf.Abs(stepVector.y), autoLayout);

            // The highlight child marks the icon you are about to place. Nothing can place a quest
            // pin, so the clone's copy of it is switched off rather than left to light up.
            Transform highlight = anchor != null ? clone.transform.Find(anchor.name) : null;
            Image highlightImage = highlight != null ? highlight.GetComponent<Image>() : null;
            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }

            _questToggleImage = clone.GetComponent<Image>();
            Sprite icon = FindPinSprite(map);
            if (_questToggleImage == null)
            {
                LogWarn("Quest pin toggle: the cloned button has no Image, so it still shows the "
                    + "boss icon and cannot be tinted");
            }
            else if (icon == null)
            {
                LogWarn("Quest pin toggle: none of the types in QuestPinTypes has a sprite in "
                    + "Minimap.m_icons, so the button still shows the boss icon");
            }
            else
            {
                _questToggleImage.sprite = icon;
            }

            foreach (UITooltip tip in clone.GetComponentsInChildren<UITooltip>(true))
            {
                tip.m_topic = "Quest markers";
                tip.m_text = "Right-click to show or hide quest markers";
            }

            // Wired through MouseClick, because that is the component the legend's own buttons use
            // and the clone already carries one. UIInputHandler is the fallback for a legend built
            // some other way.
            bool wired = false;
            foreach (MouseClick click in clone.GetComponentsInChildren<MouseClick>(true))
            {
                click.m_rightClick.AddListener(ToggleQuestPinFilter);
                click.m_leftClick.AddListener(ToggleQuestPinFilterOnDoubleTap);
                wired = true;
            }
            if (!wired)
            {
                UIInputHandler input = clone.GetComponentInChildren<UIInputHandler>(true)
                    ?? clone.AddComponent<UIInputHandler>();
                input.m_onRightClick = delegate { ToggleQuestPinFilter(); };
                input.m_onLeftClick = delegate { ToggleQuestPinFilterOnDoubleTap(); };
            }

            SyncQuestToggleTint(VisibleIconTypesField?.GetValue(map) as bool[]);
            LogInfo("Quest pin toggle added to the map legend for "
                + string.Join(", ", _questPinTypes.Select(t => t.ToString()).ToArray()));
        }

        /// <summary>
        /// Clears every handler the clone inherited from the boss button.
        ///
        /// The first attempt at this called RemoveAllListeners and did not work: the legend wires
        /// its clicks through MouseClick, whose three UnityEvents are filled in the prefab
        /// inspector, and RemoveAllListeners drops only the listeners added at runtime - a
        /// persistent one survives it untouched. So the new button toggled the boss pins alongside
        /// its own, whichever state either was in. Replacing each event with a fresh instance is
        /// what actually detaches the inherited wiring, and the same goes for Button.onClick.
        /// </summary>
        private static void StripClonedBehaviour(GameObject clone)
        {
            foreach (MouseClick click in clone.GetComponentsInChildren<MouseClick>(true))
            {
                click.m_leftClick = new UnityEvent();
                click.m_middleClick = new UnityEvent();
                click.m_rightClick = new UnityEvent();
            }
            foreach (Button button in clone.GetComponentsInChildren<Button>(true))
            {
                button.onClick = new Button.ButtonClickedEvent();
            }
            foreach (UIInputHandler handler in clone.GetComponentsInChildren<UIInputHandler>(true))
            {
                foreach (FieldInfo field in InputHandlerActionFields)
                {
                    field.SetValue(handler, null);
                }
            }
        }

        /// <summary>
        /// Grows the shaded panel behind the top group so it holds three icons instead of two, and
        /// lifts the group so the gap to the section below survives.
        ///
        /// The panel is grown downward, top edge fixed, and every child is then compensated back to
        /// where it was. How far a child drifts when its parent grows depends on where it is
        /// anchored: one anchored to the top does not move at all, one anchored to the bottom moves
        /// the full distance, and a fraction in between for anything else. Reading that fraction
        /// off the child is exact for any of them, which beats assuming the legend is anchored the
        /// way it looks. A layout group would re-place the children itself, so the compensation is
        /// skipped there.
        /// </summary>
        private static void ResizeLegendPanel(Transform group, float step, bool autoLayout)
        {
            if (step <= 0f)
            {
                LogWarn("Quest pin toggle: the icon spacing could not be measured, so the shaded "
                    + "panel was left at its original size");
                return;
            }
            // The shaded background is usually the buttons' own parent, but a legend that wrapped
            // the column in a separate frame would put it one level up
            RectTransform panel = group as RectTransform;
            if (panel != null && panel.GetComponent<Image>() == null)
            {
                RectTransform outer = group.parent as RectTransform;
                if (outer != null && outer.GetComponent<Image>() != null)
                {
                    panel = outer;
                }
            }
            if (panel == null)
            {
                LogWarn("Quest pin toggle: could not find the legend's background panel, so it was "
                    + "left at its original size");
                return;
            }

            Vector2 size = panel.sizeDelta;
            panel.sizeDelta = new Vector2(size.x, size.y + step);
            // Growing with the top edge fixed, then lifting by a full slot, leaves the bottom edge
            // exactly where it started - so the gap below is the same size it was
            float lift = step + (QuestPinLegendNudge != null ? QuestPinLegendNudge.Value : 0f);
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x,
                panel.anchoredPosition.y - (1f - panel.pivot.y) * step + lift);

            if (!autoLayout)
            {
                foreach (RectTransform child in panel)
                {
                    // A child whose top and bottom anchors differ is stretched: it already tracks
                    // the parent's height, and the panel's own shaded background is one of these.
                    // Compensating it shifts it out of line with the panel instead of holding it
                    // still, which is what put two offset shadings on the legend first time round.
                    if (!Mathf.Approximately(child.anchorMin.y, child.anchorMax.y))
                    {
                        continue;
                    }
                    float anchor = child.anchorMin.y;
                    child.anchoredPosition = new Vector2(child.anchoredPosition.x,
                        child.anchoredPosition.y + (1f - anchor) * step);
                }
            }
            LogInfo($"Quest pin toggle: legend panel '{panel.name}' grown by {step} and lifted by {lift}");
        }

        /// <summary>
        /// Prints the legend's layout once, so a button that lands in the wrong place can be fixed
        /// from the log rather than from another guess about how the panel is put together.
        /// </summary>
        private static void LogLegendHierarchy(Transform group)
        {
            if (_questLegendLogged || VerboseLogging == null || !VerboseLogging.Value)
            {
                return;
            }
            _questLegendLogged = true;
            var lines = new List<string>();
            for (Transform t = group; t != null; t = t.parent)
            {
                lines.Add("  up: " + DescribeRect(t));
                if (t.parent == null || lines.Count > 4)
                {
                    break;
                }
            }
            foreach (Transform child in group)
            {
                lines.Add("  child: " + DescribeRect(child));
            }
            LogInfo("Map legend layout:\n" + string.Join("\n", lines.ToArray()));
        }

        private static string DescribeRect(Transform t)
        {
            var components = t.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .ToArray();
            RectTransform rect = t as RectTransform;
            string geometry = rect == null
                ? "(not a RectTransform)"
                : $"pos={rect.anchoredPosition} size={rect.sizeDelta} "
                    + $"anchor={rect.anchorMin.y}-{rect.anchorMax.y} pivot={rect.pivot.y}";
            return $"{t.name} [{string.Join(",", components)}] {geometry}";
        }

        private static void ToggleQuestPinFilterOnDoubleTap()
        {
            if (ZInput.HasDoubleTapped())
            {
                ToggleQuestPinFilter();
            }
        }

        /// <summary>
        /// The icon sprite for the first configured type that has one. m_icons is the same table
        /// vanilla's GetSprite searches; it is read directly here rather than through GetSprite
        /// because that method is non-public in the shipped assembly and the list is not.
        /// </summary>
        private static Sprite FindPinSprite(Minimap map)
        {
            if (map.m_icons == null)
            {
                return null;
            }
            foreach (Minimap.PinType type in _questPinTypes)
            {
                foreach (Minimap.SpriteData entry in map.m_icons)
                {
                    if (entry.m_name == type && entry.m_icon != null)
                    {
                        return entry.m_icon;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Prints the whole PinType-to-sprite table once. This is what makes QuestPinTypes usable
        /// without a rebuild: the sprite names say which types carry the question mark, so a type
        /// the default misses can be added to the config instead of guessed at.
        /// </summary>
        private static void LogPinIconTable(Minimap map)
        {
            if (_questIconTableLogged || VerboseLogging == null || !VerboseLogging.Value
                || map.m_icons == null)
            {
                return;
            }
            _questIconTableLogged = true;
            var lines = new List<string>();
            foreach (Minimap.SpriteData entry in map.m_icons)
            {
                lines.Add($"  {(int)entry.m_name,2} {entry.m_name,-12} "
                    + (entry.m_icon != null ? entry.m_icon.name : "(no sprite)"));
            }
            LogInfo("Map pin icons:\n" + string.Join("\n", lines.ToArray()));
        }

        private static bool QuestPinsVisible(bool[] filters)
        {
            if (filters == null)
            {
                return true;
            }
            foreach (Minimap.PinType type in _questPinTypes)
            {
                int index = (int)type;
                if (index >= 0 && index < filters.Length && filters[index])
                {
                    return true;
                }
            }
            return false;
        }

        private static void SyncQuestToggleTint(bool[] filters)
        {
            if (_questToggleImage != null)
            {
                _questToggleImage.color = QuestPinsVisible(filters) ? Color.white : Color.gray;
            }
        }

        /// <summary>
        /// Flips every configured type together. Vanilla's ToggleIconFilter is deliberately not
        /// called: it flips one type at a time, so three calls would let the group drift out of
        /// step if anything else ever changed one of them, and its recolour loop only reaches the
        /// buttons registered in m_selectedIcons - which this one is not, for the reason above.
        /// Setting m_pinUpdateRequired is what vanilla does to ask for the redraw, and the redraw
        /// already skips hidden types.
        /// </summary>
        private static void ToggleQuestPinFilter()
        {
            Minimap map = Minimap.instance;
            bool[] filters = map != null ? VisibleIconTypesField?.GetValue(map) as bool[] : null;
            if (filters == null)
            {
                LogWarn("Quest pin toggle: could not read Minimap.m_visibleIconTypes, "
                    + "so nothing was hidden or shown");
                return;
            }
            bool target = !QuestPinsVisible(filters);
            foreach (Minimap.PinType type in _questPinTypes)
            {
                int index = (int)type;
                if (index >= 0 && index < filters.Length)
                {
                    filters[index] = target;
                }
            }
            PinUpdateRequiredField?.SetValue(map, true);
            SyncQuestToggleTint(filters);
            LogInfo($"Quest pins {(target ? "shown" : "hidden")}");
        }

        /// <summary>
        /// Marks the window where the game is loading the saved map, so the hold below can cover
        /// every legend icon for its duration. Set and cleared unconditionally, so the pair cannot
        /// come apart if the load throws.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "SetMapData")]
        class Minimap_SetMapData_ScopePatch
        {
            static void Prefix()
            {
                _loadingMapData = true;
            }

            static void Finalizer()
            {
                _loadingMapData = false;
            }
        }

        /// <summary>
        /// Holds the hide against the game's own pins. Minimap.AddPin un-hides the type it is
        /// adding, which is helpful for a pin you just placed by hand and unhelpful twice over
        /// here.
        ///
        /// It undoes the quest toggle every time Hildir's table or a vegvisir reveals somewhere
        /// new - the one thing that feature exists to prevent - so quest types are held always.
        ///
        /// And it undid the whole startup set, which is how this was found: the icons were hidden
        /// correctly and then the saved map arrived a moment later, re-adding every pin in it and
        /// un-hiding a category per pin. Only Death survived, because that world had no death pin
        /// to re-add. So during a map load the hold covers every icon the legend can toggle.
        /// Outside that window the un-hide is left alone, because a hand-placed pin vanishing as
        /// you drop it would be worse than the thing being fixed.
        ///
        /// Marking the type visible for the duration of the call means the un-hide branch is never
        /// taken. Calling ToggleIconFilter back afterwards would do the job too, but it would
        /// play a gamepad rumble and recolour the legend on a toggle the player never asked for.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "AddPin")]
        class Minimap_AddPin_KeepQuestFilterPatch
        {
            static void Prefix(Minimap __instance, Minimap.PinType type, out bool __state)
            {
                __state = false;
                bool holdForQuest = QuestPinToggle != null && QuestPinToggle.Value
                    && _questPinTypes.Contains(type);
                bool holdForLoad = _loadingMapData && IsLegendTogglePin(type);
                if (!holdForQuest && !holdForLoad)
                {
                    return;
                }
                bool[] filters = VisibleIconTypesField?.GetValue(__instance) as bool[];
                int index = (int)type;
                if (filters == null || index < 0 || index >= filters.Length || filters[index])
                {
                    return;
                }
                filters[index] = true;
                __state = true;
            }

            static void Postfix(Minimap __instance, Minimap.PinType type, bool __state)
            {
                if (!__state)
                {
                    return;
                }
                bool[] filters = VisibleIconTypesField?.GetValue(__instance) as bool[];
                int index = (int)type;
                if (filters != null && index >= 0 && index < filters.Length)
                {
                    filters[index] = false;
                }
            }
        }

        // ---------------- Startup pin visibility ----------------
        // Minimap.Awake fills m_visibleIconTypes with true and nothing ever writes it to disk, so
        // every icon is on again at the start of every session however they were left. That is the
        // whole reason this exists: the toggles work, they just do not keep.
        //
        // Only the types the legend can toggle are touched. Bed, Player, Ping, Shout, RandomEvent
        // and EventArea share the same array but have no button anywhere, so switching one off
        // would hide something with no way left in the UI to bring it back - a bug wearing the
        // costume of a feature. Death, Boss, Icon0 through Icon4 and the quest types all have a
        // button, so all of them are fair game.
        private static readonly FieldInfo SelectedIconsField =
            AccessTools.Field(typeof(Minimap), "m_selectedIcons");

        private static List<Minimap.PinType> _defaultVisiblePins = new List<Minimap.PinType>();
        private static bool _loadingMapData;
        private static Minimap _defaultPinsMap;

        private static readonly Minimap.PinType[] LegendTogglePins =
        {
            Minimap.PinType.Icon0, Minimap.PinType.Icon1, Minimap.PinType.Icon2,
            Minimap.PinType.Icon3, Minimap.PinType.Icon4,
            Minimap.PinType.Death, Minimap.PinType.Boss,
        };

        /// <summary>
        /// Whether the legend has a button for this type. The quest types count, since this mod
        /// gives them one.
        /// </summary>
        private static bool IsLegendTogglePin(Minimap.PinType type)
        {
            return Array.IndexOf(LegendTogglePins, type) >= 0 || _questPinTypes.Contains(type);
        }

        private static void ApplyDefaultPinVisibility()
        {
            Minimap map = Minimap.instance;
            if (map == null)
            {
                _defaultPinsMap = null;
                return;
            }
            // Nothing configured means the game starts the way it always has
            if (DefaultVisiblePins == null || _defaultVisiblePins.Count == 0
                || string.IsNullOrEmpty(DefaultVisiblePins.Value?.Trim()))
            {
                _defaultPinsMap = map;
                return;
            }
            if (ReferenceEquals(map, _defaultPinsMap))
            {
                return;
            }
            _defaultPinsMap = map;

            bool[] filters = VisibleIconTypesField?.GetValue(map) as bool[];
            if (filters == null)
            {
                LogWarn("Default pin visibility: could not read Minimap.m_visibleIconTypes, "
                    + "so the map was left as the game set it");
                return;
            }

            var hidden = new List<string>();
            foreach (Minimap.PinType type in LegendTogglePins.Concat(_questPinTypes).Distinct())
            {
                int index = (int)type;
                if (index < 0 || index >= filters.Length)
                {
                    continue;
                }
                filters[index] = _defaultVisiblePins.Contains(type);
                if (!filters[index])
                {
                    hidden.Add(type.ToString());
                }
            }
            PinUpdateRequiredField?.SetValue(map, true);
            RefreshLegendTints(map, filters);
            LogInfo("Default pin visibility: showing "
                + string.Join(", ", _defaultVisiblePins.Select(t => t.ToString()).ToArray())
                + "; hiding " + string.Join(", ", hidden.ToArray()));
        }

        /// <summary>
        /// Greys out the legend buttons for everything currently hidden. Vanilla only ever does
        /// this from inside ToggleIconFilter, which flips one type at a time and plays a gamepad
        /// rumble each call - no use for setting a whole row at once - so the same recolour is done
        /// here against m_selectedIcons, which is the dictionary ToggleIconFilter itself walks.
        /// </summary>
        private static void RefreshLegendTints(Minimap map, bool[] filters)
        {
            var icons = SelectedIconsField?.GetValue(map)
                as Dictionary<Minimap.PinType, Image>;
            if (icons != null)
            {
                foreach (KeyValuePair<Minimap.PinType, Image> entry in icons)
                {
                    // The tint lives on the button, not on the highlight the dictionary holds
                    Transform button = entry.Value != null ? entry.Value.transform.parent : null;
                    Image background = button != null ? button.GetComponent<Image>() : null;
                    int index = (int)entry.Key;
                    if (background != null && index >= 0 && index < filters.Length)
                    {
                        background.color = filters[index] ? Color.white : Color.gray;
                    }
                }
            }
            SyncQuestToggleTint(filters);
        }

        /// <summary>
        /// Says which icon was switched, by name and by the sprite the legend draws for it. The
        /// enum order is not the legend order and the numbers are not the ones they look like -
        /// Icon4 is 6, and 4 is Death - so a line naming both is the quickest way to find the
        /// number a config setting wants.
        ///
        /// This also reports the game switching an icon on by itself, which is not noise: that is
        /// exactly what quietly undid the startup set, and a line per occurrence would have named
        /// the cause immediately instead of after a screenshot.
        /// </summary>
        [HarmonyPatch(typeof(Minimap), "ToggleIconFilter")]
        class Minimap_ToggleIconFilter_LogPatch
        {
            static void Postfix(Minimap __instance, Minimap.PinType type)
            {
                if (VerboseLogging == null || !VerboseLogging.Value)
                {
                    return;
                }
                bool[] filters = VisibleIconTypesField?.GetValue(__instance) as bool[];
                int index = (int)type;
                bool shown = filters == null || index < 0 || index >= filters.Length
                    || filters[index];
                LogInfo($"Map icon {type} ({(int)type}, {PinSpriteName(__instance, type)}) "
                    + $"is now {(shown ? "shown" : "hidden")}");
            }
        }

        private static string PinSpriteName(Minimap map, Minimap.PinType type)
        {
            if (map?.m_icons == null)
            {
                return "unknown";
            }
            foreach (Minimap.SpriteData entry in map.m_icons)
            {
                if (entry.m_name == type)
                {
                    return entry.m_icon != null ? entry.m_icon.name : "no sprite";
                }
            }
            return "not in the icon table";
        }

        private static void LogInfo(string message)
        {
            if (VerboseLogging != null && VerboseLogging.Value)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Something did not work. Always written, unlike LogInfo: a line nobody reads costs a
        /// frame, but a failure nobody is told about costs an evening. Everything routed here is a
        /// one-shot, so it cannot run away with the log.
        /// </summary>
        private static void LogWarn(string message)
        {
            Debug.LogWarning(message);
        }
    }
}
