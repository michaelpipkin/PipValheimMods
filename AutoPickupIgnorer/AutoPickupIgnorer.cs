using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace AutoPickupIgnorer
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInProcess("valheim.exe")]
    public class AutoPickupIgnorer : BaseUnityPlugin
    {
        private enum PickupBehavior
        {
            Custom,
            IgnoreAll,
            Default
        }

        private const string modGUID = "PipMod.AutoPickupIgnorer";
        private const string modName = "Pip's Auto-pickup Ignorer";
        private const string modVersion = "1.0.0.1";
        private readonly Harmony harmony = new Harmony(modGUID);

        private static ConfigEntry<string> AutoPickupIgnoreList;
        private static List<string> IgnoreList;
        private static ConfigEntry<KeyboardShortcut> ToggleBehaviorHotkey;
        private static PickupBehavior CurrentPickupBehavior = PickupBehavior.Custom;
        private static MessageHud MessageHud;

        private void Awake() {
            AutoPickupIgnoreList = Config.Bind("General", "AutoPickupIgnoreList", GetDefaultItemList(), "Comma-separated list of items to ignore auto-pickup. Delete items or add # before item to enable auto-pickup.");
            IgnoreList = AutoPickupIgnoreList.Value.Split(',').Where(i => !i.StartsWith("#")).ToList();
            ToggleBehaviorHotkey = Config.Bind("General", "BehaviorHotkey", new KeyboardShortcut(KeyCode.Quote), "Hotkey to change pickup behavior between custom ignore, ignore all, and default behavior");
            HarmonyFileLog.Enabled = true;
            harmony.PatchAll();
        }

        private void Update() {
            if (ToggleBehaviorHotkey.Value.IsDown()) {
                switch (CurrentPickupBehavior) {
                    case PickupBehavior.Custom:
                        CurrentPickupBehavior = PickupBehavior.IgnoreAll;
                        MessageHud.ShowMessage(MessageHud.MessageType.Center, "Ignoring all items");
                        break;
                    case PickupBehavior.IgnoreAll:
                        CurrentPickupBehavior = PickupBehavior.Default;
                        MessageHud.ShowMessage(MessageHud.MessageType.Center, "Default pickup behavior");
                        break;
                    case PickupBehavior.Default:
                        CurrentPickupBehavior = PickupBehavior.Custom;
                        MessageHud.ShowMessage(MessageHud.MessageType.Center, "Ignoring custom items");
                        break;
                    default:
                        CurrentPickupBehavior = PickupBehavior.Custom;
                        MessageHud.ShowMessage(MessageHud.MessageType.Center, "Ignoring custom items");
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(Game), "Awake")]
        class Game_Awake_Patch
        {
            static void Postfix(ref bool ___isModded) {
                ___isModded = true;
            }
        }

        [HarmonyPatch(typeof(MessageHud), "Awake")]
        class MessageHud_Awake_Patch
        {
            [HarmonyPostfix]
            static void GetMessageHud(ref MessageHud ___m_instance) {
                MessageHud = ___m_instance;
            }
        }

        [HarmonyPatch(typeof(Player), "AutoPickup")]
        [HarmonyDebug]
        public static class Player_AutoPickup_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> AutoPickupTranspiler(IEnumerable<CodeInstruction> instructions) {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++) {
                    if (codes[i].opcode == OpCodes.Stloc_S &&
                        codes[i].operand.GetType() == typeof(LocalBuilder) &&
                        ((LocalBuilder)codes[i].operand).LocalIndex == 4) {
                        var gotoLabel = codes[i - 4].operand;
                        var instructionsToInsert = new List<CodeInstruction> {
                            new CodeInstruction(OpCodes.Ldloc_S, 4),
                            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop), "m_itemData")),
                            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(AutoPickupIgnorer), "IgnoreItem", new [] {typeof(ItemDrop.ItemData) })),
                            new CodeInstruction(OpCodes.Brtrue, gotoLabel)
                        };
                        codes.InsertRange(i + 5, instructionsToInsert);
                    }
                }
                return codes.AsEnumerable();
            }
        }

        public static bool IgnoreItem(ItemDrop.ItemData itemData) {
            return CurrentPickupBehavior == PickupBehavior.IgnoreAll ||
                (CurrentPickupBehavior == PickupBehavior.Custom && IgnoreList.Contains(itemData.m_dropPrefab.name));
        }

        static string GetDefaultItemList() {
            using (StreamReader r = new StreamReader("/valheim_Data/Plugins/items.json")) {
                string json = r.ReadToEnd();
                List<ValheimItem> itemList = JsonConvert.DeserializeObject<List<ValheimItem>>(json);
                var listBuilder = new StringBuilder();
                foreach (ValheimItem item in itemList) {
                    listBuilder.Append($"#{item.Item},");
                }
                return listBuilder.ToString().TrimEnd(',');
            }
        }
    }
}
