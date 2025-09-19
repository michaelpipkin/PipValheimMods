using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private const string modVersion = "1.0.12";
        private readonly Harmony harmony = new Harmony(modGUID);

        // Config file entries
        private static ConfigEntry<string> AutoPickupIgnoreList;
        private static ConfigEntry<KeyboardShortcut> ToggleBehaviorHotkey;

        // Module variables
        private static List<string> _ignoreList;
        private static PickupBehavior _currentPickupBehavior = PickupBehavior.Custom;
        private static MessageHud _messageHud;
        private static List<ItemTracking> _itemTracking = new List<ItemTracking>();

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
            private static readonly FieldInfo m_enableAutoPickupField = AccessTools.Field(typeof(Player), "m_enableAutoPickup");
            private static readonly FieldInfo m_autoPickupMaskField = AccessTools.Field(typeof(Player), "m_autoPickupMask");
            private static readonly FieldInfo m_inventoryField = AccessTools.Field(typeof(Humanoid), "m_inventory");

            [HarmonyPrefix]
            static bool AutoPickupPrefix(Player __instance, MethodBase __originalMethod, params object[] __args) {
                var dt = (float)__args[0];
                try {
                    bool m_enableAutoPickup = (bool)m_enableAutoPickupField.GetValue(__instance);
                    int m_autoPickupMask = (int)m_autoPickupMaskField.GetValue(__instance);
                    Inventory m_inventory = (Inventory)m_inventoryField.GetValue(__instance);

                    if (__instance.IsTeleporting() || !m_enableAutoPickup) {
                        return false;
                    }
                    Vector3 vector = __instance.transform.position + Vector3.up;
                    Collider[] array = Physics.OverlapSphere(vector, __instance.m_autoPickupRange, m_autoPickupMask);
                    foreach (Collider val in array) {
                        if (!(UnityEngine.Object)(object)val.attachedRigidbody) {
                            continue;
                        }
                        ItemDrop component = ((Component)(object)val.attachedRigidbody).GetComponent<ItemDrop>();
                        // If the item ignore condition is met, return false to skip the pickup
                        if (_currentPickupBehavior == PickupBehavior.IgnoreAll || IgnoreItem(component.m_itemData)) {
                            return false;
                        }
                        FloatingTerrainDummy floatingTerrainDummy = null;
                        if (component == null && (bool)(floatingTerrainDummy = ((Component)(object)val.attachedRigidbody).gameObject.GetComponent<FloatingTerrainDummy>()) && (bool)floatingTerrainDummy) {
                            component = floatingTerrainDummy.m_parent.gameObject.GetComponent<ItemDrop>();
                        }
                        if (component == null || !component.m_autoPickup || __instance.HaveUniqueKey(component.m_itemData.m_shared.m_name) || !component.GetComponent<ZNetView>().IsValid()) {
                            continue;
                        }
                        if (!component.CanPickup()) {
                            component.RequestOwn();
                        } else {
                            if (component.InTar()) {
                                continue;
                            }
                            component.Load();
                            if (!m_inventory.CanAddItem(component.m_itemData) || component.m_itemData.GetWeight() + m_inventory.GetTotalWeight() > __instance.GetMaxCarryWeight()) {
                                continue;
                            }
                            float num = Vector3.Distance(component.transform.position, vector);
                            if (num > __instance.m_autoPickupRange) {
                                continue;
                            }
                            if (num < 0.3f) {
                                __instance.Pickup(component.gameObject);
                                continue;
                            }
                            Vector3 vector2 = Vector3.Normalize(vector - component.transform.position);
                            float num2 = 15f;
                            Vector3 vector3 = vector2 * num2 * dt;
                            component.transform.position += vector3;
                            if ((bool)floatingTerrainDummy) {
                                floatingTerrainDummy.transform.position += vector3;
                            }
                        }
                    }
                    return false;
                }
                catch (NullReferenceException) {
                    return false;
                }
                catch (Exception ex) {
                    Debug.unityLogger.Log($"{DateTime.Now:MM/dd/yyyy HH:mm:ss}: Exception in AutoPickupIgnorer: " + ex.Message); // Log the error
                    return false;
                }
            }
        }

        public static bool IgnoreItem(ItemDrop.ItemData itemData) {
            // Check if the current pickup behavior is set to Custom and the item is in the ignore list
            if (_currentPickupBehavior == PickupBehavior.Custom && _ignoreList.Contains(itemData.m_dropPrefab.name)) {
                var item = _itemTracking.Find(i => i.ItemName == itemData.m_dropPrefab.name);
                // If the item is already tracked, check if enough time has passed since the last pickup
                if (item != null) {
                    if ((DateTime.Now - item.LastPickupTime).TotalSeconds > 30) {
                        Debug.unityLogger.Log($"{DateTime.Now:MM/dd/yyyy HH:mm:ss}: Ignoring item: {itemData.m_dropPrefab.name}");
                        // Update the last pickup time to the current time
                        item.LastPickupTime = DateTime.Now;
                    }
                } else {
                    Debug.unityLogger.Log($"{DateTime.Now:MM/dd/yyyy HH:mm:ss}: Ignoring item: {itemData.m_dropPrefab.name}");
                    // If the item is not tracked, add it to the tracking list with the current time
                    _itemTracking.Add(new ItemTracking { ItemName = itemData.m_dropPrefab.name, LastPickupTime = DateTime.Now });
                }
                return true;
            }
            return false;
        }
    }

    public class ItemTracking
    {
        public string ItemName { get; set; }
        public DateTime LastPickupTime { get; set; }
    }
}
