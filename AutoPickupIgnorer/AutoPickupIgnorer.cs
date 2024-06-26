﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using static AutoPickupIgnorer.Common;

namespace AutoPickupIgnorer
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInProcess("valheim.exe")]
    public class AutoPickupIgnorer : BaseUnityPlugin
    {
        // Module info
        private const string modGUID = "Pip.AutoPickupIgnorer";
        private const string modName = "Pip's Auto-pickup Ignorer";
        private const string modVersion = "1.0.6";
        private readonly Harmony harmony = new Harmony(modGUID);

        // Config file entries
        private static ConfigEntry<string> AutoPickupIgnoreList;
        private static ConfigEntry<KeyboardShortcut> ToggleBehaviorHotkey;

        // Module variables
        private static List<string> _ignoreList;
        private static PickupBehavior _currentPickupBehavior = PickupBehavior.Custom;
        private static MessageHud _messageHud;

        private void Awake() {
            Game.isModded = true;

            AutoPickupIgnoreList = Config.Bind("General", "AutoPickupIgnoreList", _defaultItemList,
                "Comma-separated list of items to ignore auto-pickup. Remove # before item to add to ignore list.");
            ToggleBehaviorHotkey = Config.Bind("General", "BehaviorHotkey", new KeyboardShortcut(KeyCode.Quote),
                "Hotkey to change pickup behavior between custom ignore, ignore all, and default behavior");

            _ignoreList = AutoPickupIgnoreList.Value.Split(',').Select(i => i.Trim()).Where(i => !i.StartsWith("#")).ToList();

            harmony.PatchAll();
        }

        private void Update() {
            if (ToggleBehaviorHotkey.Value.IsDown()) {
                switch (_currentPickupBehavior) {
                    case PickupBehavior.Custom:
                        _currentPickupBehavior = PickupBehavior.IgnoreAll;
                        _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, "Ignoring all items");
                        break;
                    case PickupBehavior.IgnoreAll:
                        _currentPickupBehavior = PickupBehavior.Default;
                        _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, "Default pickup behavior");
                        break;
                    case PickupBehavior.Default:
                        _currentPickupBehavior = PickupBehavior.Custom;
                        _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, "Ignoring custom items");
                        break;
                    default:
                        _currentPickupBehavior = PickupBehavior.Custom;
                        _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, "Ignoring custom items");
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(MessageHud), "Awake")]
        class MessageHud_Awake_Patch
        {
            [HarmonyPostfix]
            static void GetMessageHud(ref MessageHud ___m_instance) {
                _messageHud = ___m_instance;
            }
        }

        [HarmonyPatch(typeof(Player), "AutoPickup")]
        public static class Player_AutoPickup_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> AutoPickupTranspiler(IEnumerable<CodeInstruction> instructions) {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++) {
                    Debug.unityLogger.Log($"Index: {i} | OpCode: {codes[i].opcode} | Operand: {codes[i].operand}");
                    if (codes[i].opcode == OpCodes.Stloc_S &&
                        codes[i].operand.GetType() == typeof(LocalBuilder) &&
                        ((LocalBuilder)codes[i].operand).LocalIndex == 4 &&
                        codes[i - 4].opcode == OpCodes.Brfalse) {
                        Debug.unityLogger.Log("Entered if block");
                        var gotoLabel = codes[i - 4].operand;
                        Debug.unityLogger.Log($"Getting label from index: {i - 4}");
                        var instructionsToInsert = new List<CodeInstruction> {
                            new CodeInstruction(OpCodes.Ldloc_S, 4),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop), "m_itemData")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(AutoPickupIgnorer), "IgnoreItem",
                                new [] { typeof(ItemDrop.ItemData) })),
                            new CodeInstruction(OpCodes.Brtrue, gotoLabel)
                        };
                        Debug.unityLogger.Log($"instructions: {instructionsToInsert} | index: {i + 1}");
                        codes.InsertRange(i + 1, instructionsToInsert);
                    }
                }
                return codes.AsEnumerable();
            }
        }

        public static bool IgnoreItem(ItemDrop.ItemData itemData) {
            return _currentPickupBehavior == PickupBehavior.IgnoreAll ||
                (_currentPickupBehavior == PickupBehavior.Custom && _ignoreList.Contains(itemData.m_dropPrefab.name));
        }
    }
}
