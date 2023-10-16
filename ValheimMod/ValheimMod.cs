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
        private static ConfigEntry<float> customIncomingDamageRate;
        private static ConfigEntry<float> customStaminaRate;
        private static ConfigEntry<float> customMaxCarryWeight;
        private static ConfigEntry<float> customSkillGainRate;
        private static ConfigEntry<float> customPlayerDamageRate;
        private static ConfigEntry<bool> noPlacementCost;
        private static ConfigEntry<KeyboardShortcut> repairHotkey;
        private static ConfigEntry<bool> negateKnockback;
        private static ConfigEntry<bool> negateEquipmentMovementPenalty;
        private static ConfigEntry<string> favoriteFoodList;
        private static List<string> favoriteFoods;
        private static ConfigEntry<string> favoriteAmmoList;
        private static List<string> favoriteAmmo;
        private static Player player;

        private void Awake() {
            customIncomingDamageRate = Config.Bind("General", "CustomIncomingDamageRate", 1f, "Custom incoming damage rate");
            customStaminaRate = Config.Bind("General", "CustomStaminaRate", 1f, "Custom stamina rate");
            customMaxCarryWeight = Config.Bind("General", "CustomMaxCarryWeight", 300f, "Custom base max carry weight");
            customSkillGainRate = Config.Bind("General", "CustomSkillGainRate", 1f, "Custom skill gain rate");
            customPlayerDamageRate = Config.Bind("General", "CustomPlayerDamageRate", 1f, "Custom player damage rate");
            noPlacementCost = Config.Bind("General", "NoPlacementCost", false, "No material cost for building/crafting");
            negateKnockback = Config.Bind("General", "NegateKnockback", true, "Turn off knockback when hit");
            negateEquipmentMovementPenalty = Config.Bind("General", "NegateEquipPenalty", true, "Turn off equipment movement penalty");

            repairHotkey = Config.Bind("Hotkeys", "RepairHotkey", new KeyboardShortcut(KeyCode.LeftBracket), "Hotkey to repair all gear in inventory, heal player, replenish ammo, and spawn or replenish favorite foods");

            favoriteFoodList = Config.Bind("Inventory", "FavoriteFoods", "MisthareSupreme,MushroomOmelette,Salad", "Comma-separated list of foods to spawn");
            favoriteFoods = favoriteFoodList.Value.Split(',').ToList();
            favoriteAmmoList = Config.Bind("Inventory", "FavoriteAmmo", "ArrowCarapace,BoltCarapace", "Comma-separated list of ammo to replenish when repairing gear");
            favoriteAmmo = favoriteAmmoList.Value.Split(',').ToList();

            CustomUpdateWorldRates();
            Game.isModded = true;

            harmony.PatchAll();
        }

        private void Update() {
            if (repairHotkey.Value.IsDown()) {
                foreach (var item in player.m_inventory.GetAllItems().Where(i => i.IsEquipable() && i.m_durability < i.GetMaxDurability())) {
                    item.m_durability = item.GetMaxDurability();
                }
                foreach (var ammo in favoriteAmmo) {
                    int count = 0;
                    var prefab = ZNetScene.instance.GetPrefab(ammo.Trim());
                    var itemData = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (player.m_inventory.ContainsItemByName(itemData.m_name)) {
                        count = player.m_inventory.CountItems(itemData.m_name);
                    }
                    if (count < itemData.m_maxStackSize) {
                        player.m_inventory.AddItem(prefab, itemData.m_maxStackSize - count);
                    }
                }
                foreach (var food in favoriteFoods) {
                    int count = 0;
                    var prefab = ZNetScene.instance.GetPrefab(food.Trim());
                    var itemData = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (player.m_inventory.ContainsItemByName(itemData.m_name)) {
                        count = player.m_inventory.CountItems(itemData.m_name);
                    }
                    if (count < itemData.m_maxStackSize) {
                        player.m_inventory.AddItem(prefab, itemData.m_maxStackSize - count);
                    }
                }
                if (player.GetHealthPercentage() < 1f) {
                    player.SetHealth(player.GetMaxHealth());
                }
                if (player.GetStaminaPercentage() < 1f) {
                    player.AddStamina(player.GetMaxStamina());
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

        [HarmonyPatch(typeof(Game), "UpdateWorldRates")]
        class Game_UpdateWorldRates_Patch
        {
            static void Postfix() {
                CustomUpdateWorldRates();
            }
        }

        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        class Player_UpdateMovementModifier_Patch
        {
            static void Prefix(ref ItemDrop.ItemData item) {
                if (negateEquipmentMovementPenalty.Value && item.m_shared.m_movementModifier < 0) {
                    item.m_shared.m_movementModifier = 0;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplyPushback), new Type[] { typeof(Vector3), typeof(float) })]
        class Character_ApplyPushback_Patch
        {
            static void Prefix(ref float pushForce) {
                if (negateKnockback.Value) {
                    pushForce = 0f;
                }
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

        static void CustomUpdateWorldRates() {
            Game.m_localDamgeTakenRate = customIncomingDamageRate.Value;
            Game.m_staminaRate = customStaminaRate.Value;
            Game.m_skillGainRate = customSkillGainRate.Value;
            Game.m_playerDamageRate = customPlayerDamageRate.Value;
        }
    }
}
