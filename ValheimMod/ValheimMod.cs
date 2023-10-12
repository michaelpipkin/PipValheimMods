using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimMod
{
    [BepInPlugin("482ac281-ce1b-451d-b843-488fd8e61b20", "Pip's Mod", "0.0.3")]
    [BepInProcess("valheim.exe")]
    public class ValheimMod : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("482ac281-ce1b-451d-b843-488fd8e61b20");
        private static ConfigEntry<float> customDamageRate;
        private static ConfigEntry<float> customStaminaRate;
        private static ConfigEntry<float> customMaxCarryWeight;
        private static ConfigEntry<bool> noPlacementCost;
        private static ConfigEntry<KeyboardShortcut> favoriteFoodHotkey;
        private static ConfigEntry<string> favoriteFoodList;
        private static List<string> favoriteFoods;
        private static ConfigEntry<KeyboardShortcut> repairHotkey;
        private static ConfigEntry<string> favoriteAmmoList;
        private static List<string> favoriteAmmo;
        private static Player player;

        private void Awake() {
            favoriteFoodHotkey = Config.Bind("Foods", "FavoriteFoodHotkey", new KeyboardShortcut(KeyCode.RightBracket), "Hotkey to spawn favorite foods in inventory");
            favoriteFoodList = Config.Bind("Foods", "FavoriteFoods", "MisthareSupreme,MushroomOmelette,Salad", "Comma-separated list of foods to spawn");
            favoriteFoods = favoriteFoodList.Value.Split(',').ToList();

            customDamageRate = Config.Bind("General", "CustomDamageRate", 1f, "Custom damage rate (0-1)");
            customStaminaRate = Config.Bind("General", "CustomStaminaRate", 1f, "Custom stamina rate (0-1)");
            customMaxCarryWeight = Config.Bind("General", "CustomMaxCarryWeight", 300f, "Custom base max carry weight");
            noPlacementCost = Config.Bind("General", "NoPlacementCost", false, "No material cost for building/crafting");
            repairHotkey = Config.Bind("General", "RepairHotkey", new KeyboardShortcut(KeyCode.LeftBracket), "Hotkey to repair all gear in inventory");
            favoriteAmmoList = Config.Bind("General", "FavoriteAmmo", "ArrowCarapace,BoltCarapace", "List of ammo to replenish when repairing gear");
            favoriteAmmo = favoriteAmmoList.Value.Split(',').ToList();

            harmony.PatchAll();
        }

        private void Update() {
            if (favoriteFoodHotkey.Value.IsDown()) {
                foreach (var food in favoriteFoods) {
                    int count = 0;
                    GameObject prefab = ZNetScene.instance.GetPrefab(food.Trim());
                    var sharedName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                    if (player.m_inventory.ContainsItemByName(sharedName)) {
                        count = player.m_inventory.CountItems(sharedName);
                    }
                    player.m_inventory.AddItem(prefab, 10 - count);
                }
            }
            if (repairHotkey.Value.IsDown()) {
                foreach (var item in player.m_inventory.GetAllItems().Where(i => i.IsEquipable() && i.m_durability < i.GetMaxDurability())) {
                    item.m_durability = item.GetMaxDurability();
                }
                foreach (var ammo in favoriteAmmo) {
                    int count = 0;
                    GameObject prefab = ZNetScene.instance.GetPrefab(ammo.Trim());
                    var sharedName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                    if (player.m_inventory.ContainsItemByName(sharedName)) {
                        count = player.m_inventory.CountItems(sharedName);
                    }
                    player.m_inventory.AddItem(prefab, 100 - count);
                }
            }
        }

        void OnDestroy() {
            harmony.UnpatchSelf();
        }

        [HarmonyPatch(typeof(Game), "Awake")]
        class Game_Awake_Patch
        {
            static void Postfix(ref bool ___isModded) {
                ___isModded = true;
            }
        }

        //[HarmonyPatch(typeof(Console), "Awake")]
        //class Console_Awake_Patch
        //{
        //    [HarmonyPostfix]
        //    static void GetConsole(ref Console ___m_instance) {
        //        console = ___m_instance;
        //    }
        //}

        [HarmonyPatch(typeof(Game), "SpawnPlayer")]
        class Game_SpawnPlayer_Patch
        {
            static void Postfix(ref float ___m_staminaRate, ref float ___m_localDamgeTakenRate) {
                if (___m_localDamgeTakenRate > customDamageRate.Value || ___m_staminaRate > customStaminaRate.Value) {
                    Debug.Log($"Setting damage and stamina rates.");
                    ___m_localDamgeTakenRate = (float)customDamageRate.Value;
                    ___m_staminaRate = (float)customStaminaRate.Value;
                    Debug.Log($"Damage rate: {___m_localDamgeTakenRate}; Stamina rate: {___m_staminaRate}");
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        class Player_Awake_Patch
        {
            static void Postfix(ref float ___m_maxCarryWeight, ref bool ___m_noPlacementCost, ref Player __instance) {
                Debug.Log($"Setting base maximum carry weight.");
                ___m_maxCarryWeight = (float)customMaxCarryWeight.Value;
                Debug.Log($"Base max carry weight: {___m_maxCarryWeight}");
                ___m_noPlacementCost = noPlacementCost.Value;
                player = __instance;
            }
        }

        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        class Player_UpdateMovementModifier_Patch
        {
            static void Prefix(ref ItemDrop.ItemData item) {
                if (item.m_shared.m_movementModifier < 0) {
                    item.m_shared.m_movementModifier = 0;
                }
            }
        }

        [HarmonyPatch(typeof(Character), "SetWalk")]
        class Character_SetWalk_Patch
        {
            static void Prefix(ref Character __instance) {
                if (__instance.GetHealthPercentage() < 1f) {
                    __instance.SetHealth(__instance.GetMaxHealth());
                }
                if (__instance.GetStaminaPercentage() < 1f) {
                    __instance.AddStamina(__instance.GetMaxStamina());
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplyPushback), new Type[] { typeof(Vector3), typeof(float) })]
        class Character_ApplyPushback_Patch
        {
            static void Prefix(ref float pushForce) {
                pushForce = 0f;
            }
        }

        [HarmonyPatch(typeof(Odin), "Awake")]
        class Odin_Awake_Patch
        {
            static void Postfix(ref float ___m_despawnCloseDistance) {
                ___m_despawnCloseDistance = 1f;
            }
        }

        [HarmonyPatch(typeof(ResourceRoot), "Drain")]
        class ResourceRoot_Drain_Patch
        {
            static void Postfix(ref float ___m_regenPerSec) {
                ___m_regenPerSec = 20f;
            }
        }
    }
}
