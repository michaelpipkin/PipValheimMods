using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private static ConfigEntry<KeyboardShortcut> DumpItemListHotkey;

        // Config values
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
        private static MessageHud _messageHud;

        // m_inventory is declared on Humanoid, not Player. AccessTools walks the base chain and
        // ignores access level; typeof(Player).GetField(..., NonPublic) does neither for a private
        // base-class field. Resolved once rather than on every hotkey press.
        private static readonly FieldInfo m_inventoryField = AccessTools.Field(typeof(Humanoid), "m_inventory");

        private void Awake() {
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
            DumpItemListHotkey = Config.Bind("Hotkeys", "DumpItemListHotkey", new KeyboardShortcut(KeyCode.RightBracket), "Hotkey to dump every item prefab in the game to files in the BepInEx config folder. Must be pressed in a loaded world.");

            FavoriteFoodList = Config.Bind("Inventory", "FavoriteFoods", "MisthareSupreme,FishAndBread,SeekerAspic", "Comma-separated list of foods to spawn");
            _favoriteFoods = FavoriteFoodList.Value.Split(',').ToList();
            FavoriteAmmoList = Config.Bind("Inventory", "FavoriteAmmo", "ArrowCarapace,BoltCarapace", "Comma-separated list of ammo to replenish when repairing gear");
            _favoriteAmmo = FavoriteAmmoList.Value.Split(',').ToList();

            Game.isModded = true;

            harmony.PatchAll();
        }

        private void Update() {
            if (RepairHotkey.Value.IsDown()) {
                // Read the local player at point of use; m_localPlayer isn't assigned until
                // SetLocalPlayer runs, which is after Player.Awake
                Player player = Player.m_localPlayer;
                if (player == null) {
                    return;
                }
                var inventory = (Inventory)m_inventoryField.GetValue(player);

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
                if (player.GetHealthPercentage() < 1f) {
                    player.SetHealth(player.GetMaxHealth());
                }
                if (player.GetStaminaPercentage() < 1f) {
                    player.AddStamina(player.GetMaxStamina());
                }
                if (player.GetEitrPercentage() < 1f) {
                    player.AddEitr(player.GetMaxEitr());
                }
            }

            if (DumpItemListHotkey.Value.IsDown()) {
                DumpItemList();
            }
        }

        /// <summary>
        /// Writes every item prefab known to ObjectDB out to two files in the BepInEx config folder:
        /// a tab-separated reference table, and a ready-to-paste AutoPickupIgnorer ignore list.
        /// ObjectDB isn't fully populated until a world is loaded, so this does nothing at the menu.
        /// </summary>
        private static void DumpItemList() {
            ObjectDB odb = ObjectDB.instance;
            if (odb == null || odb.m_items == null || odb.m_items.Count == 0) {
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "Item list unavailable - load a world first");
                return;
            }

            var rows = new List<ItemRow>();
            foreach (var prefab in odb.m_items) {
                if (prefab == null) {
                    continue;
                }
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) {
                    continue;
                }
                var shared = drop.m_itemData.m_shared;
                // Localization can be unavailable very early; fall back to the raw token
                string display = Localization.instance != null
                    ? Localization.instance.Localize(shared.m_name)
                    : shared.m_name;
                rows.Add(new ItemRow {
                    PrefabName = prefab.name,
                    DisplayName = display,
                    NameToken = shared.m_name,
                    ItemType = shared.m_itemType.ToString(),
                    AutoPickup = drop.m_autoPickup,
                    // Registered in ZNetScene means the prefab can exist as a world object, which
                    // is what separates real drops from creature attack prefabs and cosmetics.
                    InZNetScene = ZNetScene.instance != null && ZNetScene.instance.GetPrefab(prefab.name) != null,
                    MaxStackSize = shared.m_maxStackSize,
                    Weight = shared.m_weight,
                });
            }
            rows.Sort((a, b) => string.Compare(a.PrefabName, b.PrefabName, StringComparison.OrdinalIgnoreCase));

            string dir = Paths.ConfigPath;

            // Reference table: prefab name is the id the "spawn" console command takes
            var table = new StringBuilder();
            table.AppendLine("PrefabName\tDisplayName\tNameToken\tItemType\tAutoPickup\tInZNetScene\tMaxStack\tWeight");
            foreach (var r in rows) {
                table.AppendLine($"{r.PrefabName}\t{r.DisplayName}\t{r.NameToken}\t{r.ItemType}\t{r.AutoPickup}\t{r.InZNetScene}\t{r.MaxStackSize}\t{r.Weight:0.##}");
            }
            string tablePath = Path.Combine(dir, "PipsMod_ItemList.tsv");
            File.WriteAllText(tablePath, table.ToString());

            // An item can only be ignored if it auto-picks-up AND can exist as a world drop.
            // Creature attack prefabs and cosmetics live in ObjectDB but never in ZNetScene.
            // Every entry is commented out with #, matching AutoPickupIgnorer's default convention.
            var eligible = rows.Where(r => r.AutoPickup && r.InZNetScene).Select(r => "#" + r.PrefabName).ToList();
            string listPath = Path.Combine(dir, "PipsMod_AutoPickupIgnoreList.txt");
            File.WriteAllText(listPath, string.Join(", ", eligible));

            Debug.Log($"Dumped {rows.Count} items ({eligible.Count} auto-pickup) to {dir}");
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                $"Dumped {rows.Count} items ({eligible.Count} auto-pickup) to BepInEx/config");
        }

        private class ItemRow
        {
            public string PrefabName;
            public string DisplayName;
            public string NameToken;
            public string ItemType;
            public bool AutoPickup;
            public bool InZNetScene;
            public int MaxStackSize;
            public float Weight;
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
            static void Postfix(ref float ___m_maxCarryWeight, ref bool ___m_noPlacementCost) {
                Debug.Log($"Setting base maximum carry weight.");
                ___m_maxCarryWeight = (float)CustomMaxCarryWeight.Value;
                Debug.Log($"Base max carry weight: {___m_maxCarryWeight}");
                ___m_noPlacementCost = NoPlacementCost.Value;
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
                // Blood magic spends health instead of eitr, so the eitr rate doubles as the
                // magic-cost multiplier. Local player only, matching UseEitr and UseStamina.
                if (__instance == Player.m_localPlayer) {
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
            // Gain and degeneration are both scaled here. Applying degeneration from a postfix that
            // called AddAdrenaline again re-entered this patch, and the correction diverged once the
            // degen rate reached 2.
            static void Prefix(ref float v) {
                if (v > 0f) {
                    v *= CustomAdrenalineGainRate.Value;
                } else if (v < 0f) {
                    v *= CustomAdrenalineDegenRate.Value;
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
            static bool Prefix(Character __instance, HitData hit, bool showDamageText, bool triggerEffects, HitData.DamageModifier mod) {
                // Handle damage dealt BY the local player TO enemies
                if (hit.GetAttacker() == Player.m_localPlayer && !__instance.IsPlayer()) {
                    hit.ApplyModifier(CustomPlayerDamageRate.Value);
                    return true; // Continue with original method
                }

                // Handle damage taken BY the local player
                if (__instance == Player.m_localPlayer) {
                    Player player = __instance as Player;
                    float currentHealth = player.GetHealth();
                    float maxHealth = player.GetMaxHealth();
                    float healthPercentage = currentHealth / maxHealth;

                    // Calculate total incoming damage before any modifiers
                    float totalDamage = hit.GetTotalDamage();

                    if (healthPercentage <= 0.25f) {
                        // Below 25% health: nullify all damage
                        return false; // Skip the original method entirely
                    } else if (healthPercentage <= 0.5f) {
                        // 25-50% health: cap damage at 10% of current health (very protective)
                        float maxAllowedDamage = currentHealth * 0.1f;
                        if (totalDamage > maxAllowedDamage) {
                            float reductionFactor = maxAllowedDamage / totalDamage;
                            hit.ApplyModifier(reductionFactor);
                        }
                        return true;
                    } else {
                        // Above 50% health: cap damage at 25% of current health  
                        float maxAllowedDamage = currentHealth * 0.25f;
                        if (totalDamage > maxAllowedDamage) {
                            float reductionFactor = maxAllowedDamage / totalDamage;
                            hit.ApplyModifier(reductionFactor);
                        }
                        return true;
                    }
                }
                return true; // Continue with the original method for all other cases
            }
        }
    }
}
