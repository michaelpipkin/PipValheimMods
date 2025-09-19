using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ValheimMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInProcess("valheim.exe")]
    public class ValheimMod : BaseUnityPlugin
    {
        // Module Info
        private const string modGUID = "Pip.PipsMod";
        private const string modName = "Pip's Mod";
        private const string modVersion = "0.0.4";
        private readonly Harmony harmony = new Harmony(modGUID);

        // Keyboard shortcuts
        private static ConfigEntry<KeyboardShortcut> RepairHotkey;

        // Config values
        private static ConfigEntry<float> CustomIncomingDamageRate;
        private static ConfigEntry<float> CustomStaminaRate;
        private static ConfigEntry<float> CustomEitrRate;
        private static ConfigEntry<float> CustomMaxCarryWeight;
        private static ConfigEntry<float> CustomSkillGainRate;
        private static ConfigEntry<float> CustomPlayerDamageRate;
        private static ConfigEntry<float> CustomAdrenalineGainRate;
        private static ConfigEntry<float> CustomAdrenalineDegenRate;
        private static ConfigEntry<bool> NoPlacementCost;
        private static ConfigEntry<bool> NegateKnockback;
        private static ConfigEntry<bool> NegateEquipmentMovementPenalty;
        private static ConfigEntry<string> FavoriteFoodList;
        private static ConfigEntry<string> FavoriteAmmoList;

        // Module variables
        private static List<string> _favoriteFoods;
        private static List<string> _favoriteAmmo;
        private static Player _player;
        private static MessageHud _messageHud;

        private void Awake() {
            CustomIncomingDamageRate = Config.Bind("General", "CustomIncomingDamageRate", 1f, "Custom incoming damage rate");
            CustomStaminaRate = Config.Bind("General", "CustomStaminaRate", 1f, "Custom stamina rate");
            CustomEitrRate = Config.Bind("General", "CustomEitrRate", 1f, "Custom Eitr usage rate");
            CustomMaxCarryWeight = Config.Bind("General", "CustomMaxCarryWeight", 300f, "Custom base max carry weight");
            CustomSkillGainRate = Config.Bind("General", "CustomSkillGainRate", 1f, "Custom skill gain rate");
            CustomPlayerDamageRate = Config.Bind("General", "CustomPlayerDamageRate", 1f, "Custom player damage rate");
            CustomAdrenalineGainRate = Config.Bind("General", "CustomAdrenalineGainRate", 1f, "Custom adrenaline gain rate multiplier");
            CustomAdrenalineDegenRate = Config.Bind("General", "CustomAdrenalineDegenRate", 1f, "Custom adrenaline degeneration rate multiplier");
            NoPlacementCost = Config.Bind("General", "NoPlacementCost", false, "No material cost for building/crafting");
            NegateKnockback = Config.Bind("General", "NegateKnockback", true, "Turn off knockback when hit");
            NegateEquipmentMovementPenalty = Config.Bind("General", "NegateEquipPenalty", true, "Turn off equipment movement penalty");

            RepairHotkey = Config.Bind("Hotkeys", "RepairHotkey", new KeyboardShortcut(KeyCode.LeftBracket), "Hotkey to repair all gear in inventory, heal player, replenish ammo, and spawn or replenish favorite foods");

            FavoriteFoodList = Config.Bind("Inventory", "FavoriteFoods", "MisthareSupreme,FishAndBread,SeekerAspic", "Comma-separated list of foods to spawn");
            _favoriteFoods = FavoriteFoodList.Value.Split(',').ToList();
            FavoriteAmmoList = Config.Bind("Inventory", "FavoriteAmmo", "ArrowCarapace,BoltCarapace", "Comma-separated list of ammo to replenish when repairing gear");
            _favoriteAmmo = FavoriteAmmoList.Value.Split(',').ToList();

            Game.isModded = true;

            harmony.PatchAll();
        }

        private void Update() {
            if (RepairHotkey.Value.IsDown()) {
                // Use reflection to get the m_inventory field
                var inventoryField = typeof(Player).GetField("m_inventory", BindingFlags.NonPublic | BindingFlags.Instance);
                var inventory = (Inventory)inventoryField.GetValue(_player);

                foreach (var item in inventory.GetAllItems().Where(i => i.IsEquipable() && i.m_durability < i.GetMaxDurability())) {
                    item.m_durability = item.GetMaxDurability();
                }
                foreach (var ammo in _favoriteAmmo) {
                    int count = 0;
                    var prefab = ZNetScene.instance.GetPrefab(ammo.Trim());
                    var itemData = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (inventory.ContainsItemByName(itemData.m_name)) {
                        count = inventory.CountItems(itemData.m_name);
                    }
                    if (count < itemData.m_maxStackSize) {
                        inventory.AddItem(prefab, itemData.m_maxStackSize - count);
                    }
                }
                foreach (var food in _favoriteFoods) {
                    int count = 0;
                    var prefab = ZNetScene.instance.GetPrefab(food.Trim());
                    var itemData = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (inventory.ContainsItemByName(itemData.m_name)) {
                        count = inventory.CountItems(itemData.m_name);
                    }
                    if (count < itemData.m_maxStackSize) {
                        inventory.AddItem(prefab, itemData.m_maxStackSize - count);
                    }
                }
                if (_player.GetHealthPercentage() < 1f) {
                    _player.SetHealth(_player.GetMaxHealth());
                }
                if (_player.GetStaminaPercentage() < 1f) {
                    _player.AddStamina(_player.GetMaxStamina());
                }
                if (_player.GetEitrPercentage() < 1f) {
                    _player.AddEitr(_player.GetMaxEitr());
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

        [HarmonyPatch(typeof(Player), "Awake")]
        class Player_Awake_Patch
        {
            static void Postfix(ref float ___m_maxCarryWeight, ref bool ___m_noPlacementCost, ref Player __instance) {
                Debug.Log($"Setting base maximum carry weight.");
                ___m_maxCarryWeight = (float)CustomMaxCarryWeight.Value;
                Debug.Log($"Base max carry weight: {___m_maxCarryWeight}");
                ___m_noPlacementCost = NoPlacementCost.Value;
                _player = __instance;
            }
        }

        // Removed Game_UpdateWorldRates_Patch - no longer needed with player-specific patches

        [HarmonyPatch(typeof(Player), "UseEitr")]
        class Player_UseEitr_Patch
        {
            static void Prefix(Player __instance, ref float v) {
                // Only apply to local player
                if (__instance == Player.m_localPlayer) {
                    v *= CustomEitrRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), "UseHealth")]
        class Character_UseHealth_Patch
        {
            static void Prefix(ref Character __instance, ref float hp) {
                if (__instance is Player) {
                    hp *= CustomEitrRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        class Player_UpdateMovementModifier_Patch
        {
            static void Prefix(ref ItemDrop.ItemData item) {
                if (NegateEquipmentMovementPenalty.Value && item.m_shared.m_movementModifier < 0) {
                    item.m_shared.m_movementModifier = 0;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplyPushback), new Type[] { typeof(Vector3), typeof(float) })]
        class Character_ApplyPushback_Patch
        {
            static void Prefix(ref float pushForce) {
                if (NegateKnockback.Value) {
                    pushForce = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(Skills.Skill), "Raise")]
        class Skill_Raise_Patch
        {
            [HarmonyPostfix]
            static void Postfix(ref Skills.Skill __instance) {
                if (__instance.m_level < 100f) {
                    _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, $"{__instance.m_info.m_skill} ({__instance.m_level:N0}):  {__instance.GetLevelPercentage():P3}");
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

        [HarmonyPatch(typeof(Player), "AddAdrenaline")]
        class Player_AddAdrenaline_Patch
        {
            static void Prefix(ref float v) {
                if (v > 0f) {
                    v *= CustomAdrenalineGainRate.Value;
                }
            }
        }

        // Store original adrenaline for degeneration rate modification
        private static float _lastAdrenalineDegenModification = 0f;

        [HarmonyPatch(typeof(Player), "AddAdrenaline")]
        class Player_AddAdrenaline_Degeneration_Patch
        {
            static void Postfix(Player __instance, float v) {
                // Apply custom degeneration rate by adjusting adrenaline after negative addition (degeneration)
                if (v < 0f && CustomAdrenalineDegenRate.Value != 1f) {
                    // Calculate the difference to apply based on our custom rate
                    float adjustedDegenAmount = v * (CustomAdrenalineDegenRate.Value - 1f);
                    __instance.AddAdrenaline(adjustedDegenAmount);
                }
            }
        }

        [HarmonyPatch(typeof(Player), "UseStamina")]
        class Player_UseStamina_PlayerSpecific_Patch
        {
            static void Prefix(Player __instance, ref float v) {
                // Only apply to local player
                if (__instance == Player.m_localPlayer) {
                    v *= CustomStaminaRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Player), "RaiseSkill")]
        class Player_RaiseSkill_PlayerSpecific_Patch
        {
            static void Prefix(Player __instance, ref float value) {
                // Only apply to local player
                if (__instance == Player.m_localPlayer) {
                    value *= CustomSkillGainRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), "ApplyDamage")]
        class Character_ApplyDamage_PlayerSpecific_Patch
        {
            static void Prefix(Character __instance, HitData hit) {
                // Only modify damage dealt BY the local player TO enemies
                if (hit.GetAttacker() == Player.m_localPlayer && !__instance.IsPlayer()) {
                    hit.ApplyModifier(CustomPlayerDamageRate.Value);
                }
                // Only modify damage taken BY the local player
                else if (__instance == Player.m_localPlayer) {
                    hit.ApplyModifier(CustomIncomingDamageRate.Value);
                }
            }
        }
    }
}
