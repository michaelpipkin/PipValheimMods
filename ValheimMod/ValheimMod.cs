using BepInEx;
using BepInEx.Bootstrap;
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
        private static ConfigEntry<float> CustomResourceRate;
        private static ConfigEntry<float> CustomStackSizeMultiplier;
        private static ConfigEntry<float> CustomSmelterOutputRate;
        private static ConfigEntry<float> CustomFermenterOutputRate;
        private static ConfigEntry<float> CustomCookingOutputRate;
        private static ConfigEntry<float> CustomProcessingTimeRate;
        private static ConfigEntry<int> CustomInventoryRows;
        private static ConfigEntry<float> CustomStaminaRate;
        private static ConfigEntry<float> CustomEitrRate;
        private static ConfigEntry<float> CustomMaxCarryWeight;
        private static ConfigEntry<float> CustomSkillGainRate;
        private static ConfigEntry<float> CustomPlayerDamageRate;
        private static ConfigEntry<float> CustomAdrenalineGainRate;
        private static ConfigEntry<float> CustomAdrenalineDegenRate;
        private static ConfigEntry<bool> NoPlacementCost;
        private static ConfigEntry<bool> SkipIntroCinematic;
        private static ConfigEntry<bool> NegateKnockback;
        private static ConfigEntry<bool> NegateEquipmentMovementPenalty;
        private static ConfigEntry<string> FavoriteFoodList;
        private static ConfigEntry<string> FavoriteAmmoList;

        // Module variables
        private static List<string> _favoriteFoods;
        private static List<string> _favoriteAmmo;
        private static MessageHud _messageHud;

        // Inventory row management is a shared resource: other mods patch Player.SetInventorySize
        // too. Cap the retries so a disagreement can never become a per-frame fight.
        private const int MaxInventoryRowAttempts = 5;
        private const string ExtraSlotsGuid = "shudnal.ExtraSlots";
        private static int _inventoryRowAttempts;
        private static bool _inventoryRowsDisabled;
        private static bool _rowManagerChecked;

        // Vanilla max stack size per item type, captured before we ever change it, so the
        // multiplier is applied to the original value rather than to our own previous result.
        private static readonly Dictionary<ItemDrop.ItemData.SharedData, int> _baseStackSizes =
            new Dictionary<ItemDrop.ItemData.SharedData, int>();

        // m_inventory is declared on Humanoid, not Player. AccessTools walks the base chain and
        // ignores access level; typeof(Player).GetField(..., NonPublic) does neither for a private
        // base-class field. Resolved once rather than on every hotkey press.
        private static readonly FieldInfo m_inventoryField = AccessTools.Field(typeof(Humanoid), "m_inventory");

        private void Awake() {
            CustomResourceRate = Config.Bind("General", "CustomResourceRate", 1f, "Multiplier for resource drops (wood, ore, food, monster parts). 1 leaves the world's own Resources setting alone. Equipment and other types the game marks as non-scaling are unaffected, and a single drop still can't exceed one stack.");
            CustomStackSizeMultiplier = Config.Bind("General", "CustomStackSizeMultiplier", 1f, "Multiplier for the max stack size of every stackable item. 1 leaves vanilla stack sizes alone. Items that don't stack in vanilla (equipment) are unaffected. Warning: stacks larger than vanilla get written into your save, so lowering this later can clamp or lose the excess.");
            CustomInventoryRows = Config.Bind("General", "CustomInventoryRows", 0,
                new ConfigDescription("Number of rows in the player inventory (8 slots per row). Vanilla starts at 4 and the game itself allows up to 9, which Haldor sells. 0 leaves it alone. Affects only your own inventory, never containers. Warning: lowering this drops any items in the removed slots on the ground. Ignored when Extra Slots is installed - use its own 'Amount of extra inventory rows' setting instead.",
                    new AcceptableValueRange<int>(0, 9)));
            CustomSmelterOutputRate = Config.Bind("General", "CustomSmelterOutputRate", 1f, "Multiplier for what smelting stations produce per process - charcoal kiln, smelter, blast furnace, windmill, spinning wheel and anything else built on the Smelter component. Input cost is unchanged, so one wood still yields one batch, just a bigger one. Capped at the output item's max stack size.");
            CustomFermenterOutputRate = Config.Bind("General", "CustomFermenterOutputRate", 1f, "Multiplier for how many items a fermenter yields per batch. Mead normally produces 4, so 2 gives 8. Rounded to a whole number of items.");
            CustomCookingOutputRate = Config.Bind("General", "CustomCookingOutputRate", 1f, "Multiplier for how many items a cooking station produces per cooked slot. Whole items only, so this rounds to the nearest integer.");
            CustomProcessingTimeRate = Config.Bind("General", "CustomProcessingTimeRate", 1f, "Multiplier on how long processing takes for smelting stations, fermenters and cooking stations. Lower is faster, so 0.05 is twenty times quicker. Fuel cost per item is unchanged. Note that smelting steps in whole seconds, so values below 0.1 stop helping, and cooking stations burn food proportionally sooner.");
            CustomStaminaRate = Config.Bind("General", "CustomStaminaRate", 1f, "Custom stamina rate");
            CustomEitrRate = Config.Bind("General", "CustomEitrRate", 1f, "Custom Eitr usage rate");
            CustomMaxCarryWeight = Config.Bind("General", "CustomMaxCarryWeight", 300f, "Custom base max carry weight");
            CustomSkillGainRate = Config.Bind("General", "CustomSkillGainRate", 1f, "Custom skill gain rate");
            CustomPlayerDamageRate = Config.Bind("General", "CustomPlayerDamageRate", 1f, "Custom player damage rate");
            CustomAdrenalineGainRate = Config.Bind("General", "CustomAdrenalineGainRate", 1f, "Custom adrenaline gain rate multiplier");
            CustomAdrenalineDegenRate = Config.Bind("General", "CustomAdrenalineDegenRate", 1f, "Custom adrenaline degeneration rate multiplier");
            NoPlacementCost = Config.Bind("General", "NoPlacementCost", false, "No material cost for building/crafting");
            SkipIntroCinematic = Config.Bind("General", "SkipIntroCinematic", true, "Skip the intro cinematic that plays on launch and go straight to the main menu. Cinematics remain replayable from the menu.");
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

            ApplyInventoryRows();
        }

        /// <summary>
        /// Valheim supports a resizable player inventory natively - Haldor sells extra rows, and
        /// Player.OnSpawned restores the count from the "invrows" unique key. Reusing that keeps the
        /// change scoped to the player, persisted on the character, and resizes the inventory panel;
        /// containers keep vanilla slot counts and stack limits.
        ///
        /// Driven from Update rather than a spawn patch because SetInventorySize dereferences
        /// InventoryGui.instance, and the player can spawn before that singleton exists.
        ///
        /// Polling means another mod that also manages rows can be fought frame by frame, so this
        /// gives up after MaxInventoryRowAttempts rather than looping forever. ExtraSlots owns rows
        /// deliberately - it runs a skipping prefix on Player.SetInventorySize and adds rows of its
        /// own - so it is detected up front and this feature stands down entirely.
        /// </summary>
        private static void ApplyInventoryRows() {
            int rows = CustomInventoryRows.Value;
            if (rows <= 0 || _inventoryRowsDisabled) {
                return;
            }
            // Checked here rather than in Awake because plugins load in sequence and Extra Slots
            // loads after this one, so it isn't registered yet while Awake is running.
            if (!_rowManagerChecked) {
                _rowManagerChecked = true;
                if (Chainloader.PluginInfos.ContainsKey(ExtraSlotsGuid)) {
                    _inventoryRowsDisabled = true;
                    Debug.LogWarning($"CustomInventoryRows is {rows}, but Extra Slots is installed and manages inventory rows itself. Standing down to avoid fighting it. Set CustomInventoryRows to 0 and use 'Amount of extra inventory rows' under [Extra slots] in shudnal.ExtraSlots.cfg instead.");
                    return;
                }
            }
            Player player = Player.m_localPlayer;
            if (player == null || InventoryGui.instance == null) {
                return;
            }
            Inventory inventory = player.GetInventory();
            if (inventory == null || inventory.GetHeight() == rows) {
                return;
            }
            if (_inventoryRowAttempts >= MaxInventoryRowAttempts) {
                _inventoryRowsDisabled = true;
                Debug.LogWarning($"Giving up on CustomInventoryRows: asked for {rows} rows {MaxInventoryRowAttempts} times and the height keeps changing back, so another mod is managing inventory size. Set CustomInventoryRows to 0 and use that mod's own setting.");
                return;
            }
            _inventoryRowAttempts++;
            player.SetInventorySize(rows);
            Debug.Log($"Inventory size set to {rows} rows ({inventory.GetWidth() * rows} slots)");
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

        // Valheim already has a global resource multiplier that every drop path consults:
        // CharacterDrop (monster parts), DropTable (trees, rocks, destructibles, chests),
        // Pickable / PickableItem (foraging), Beehive and SapCollector all route their counts
        // through Game.ScaleDrops, which reads Game.m_resourceRate. So rather than patching six
        // systems, override the one value they share. UpdateWorldRates is the only place the game
        // assigns it, and it re-runs on world load and whenever global keys change, so a postfix
        // here survives the game resetting it back to the world's own setting.
        [HarmonyPatch(typeof(Game), nameof(Game.UpdateWorldRates))]
        class Game_UpdateWorldRates_Patch
        {
            static void Postfix() {
                // A rate of 1 means "don't interfere", leaving the world's Resources modifier intact
                if (CustomResourceRate.Value > 0f && CustomResourceRate.Value != 1f) {
                    Game.m_resourceRate = CustomResourceRate.Value;
                }
            }
        }

        // Every ItemData created from a prefab is a MemberwiseClone, so m_shared is copied by
        // reference rather than duplicated - which is why ObjectDB can key m_itemByData on it.
        // That means editing the prefab's SharedData.m_maxStackSize reaches every stack of that
        // item already sitting in an inventory, not just newly created ones.
        //
        // UpdateRegisters is the hook because both ObjectDB.Awake and ObjectDB.CopyOtherDB call it,
        // so this covers the menu DB and the world DB without patching each separately.
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.UpdateRegisters))]
        class ObjectDB_UpdateRegisters_Patch
        {
            static void Postfix(ObjectDB __instance) {
                ApplyStackSizeMultiplier(__instance);
            }
        }

        /// <summary>
        /// Scales every stackable item's max stack size. Safe to call repeatedly: the game's own
        /// value is remembered the first time each item is seen, so the multiplier is always
        /// recomputed from that baseline instead of compounding on each call. Setting the
        /// multiplier back to 1 therefore restores vanilla stack sizes.
        /// </summary>
        private static void ApplyStackSizeMultiplier(ObjectDB odb) {
            if (odb == null || odb.m_items == null) {
                return;
            }
            float multiplier = CustomStackSizeMultiplier.Value;
            int changed = 0;
            foreach (var prefab in odb.m_items) {
                if (prefab == null) {
                    continue;
                }
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) {
                    continue;
                }
                var shared = drop.m_itemData.m_shared;
                if (!_baseStackSizes.TryGetValue(shared, out int baseSize)) {
                    baseSize = shared.m_maxStackSize;
                    _baseStackSizes[shared] = baseSize;
                }
                // Equipment and other one-per-slot items stay unstackable
                if (baseSize <= 1) {
                    continue;
                }
                int target = Mathf.Max(1, Mathf.RoundToInt(baseSize * multiplier));
                if (shared.m_maxStackSize != target) {
                    shared.m_maxStackSize = target;
                    changed++;
                }
            }
            if (changed > 0) {
                Debug.Log($"Stack size multiplier {multiplier:0.##} applied to {changed} item(s)");
            }
        }

        // FejdStartup.PlayIntroCinematic only plays the intro when m_introOnStartup is set;
        // otherwise it takes its else branch and shows the main menu straight away. Nothing in the
        // game ever writes that field, which is why there's no in-game option for it. Clearing it
        // during Awake reuses the game's own skip path rather than suppressing playback, so no
        // "Failed to play intro cinematic" error is logged and the Cinematics menu still works.
        // Every smelting station routes its output through Smelter.Spawn, both the one-at-a-time
        // path (QueueProcessed calls Spawn(ore, 1) when m_spawnStack is off, which is what the
        // charcoal kiln does) and the batched path via SpawnProcessed. Scaling the stack here
        // therefore covers all of them, and leaves input cost untouched.
        [HarmonyPatch(typeof(Smelter), "Spawn")]
        class Smelter_Spawn_Patch
        {
            static void Prefix(Smelter __instance, string ore, ref int stack) {
                float multiplier = CustomSmelterOutputRate.Value;
                if (multiplier <= 1f || stack <= 0) {
                    return;
                }
                int scaled = Mathf.Max(1, Mathf.RoundToInt(stack * multiplier));

                // Cap at what the produced item can actually hold in one stack. GetItemConversion
                // is non-public in the shipped assembly, so match on m_conversion the same way.
                foreach (var conversion in __instance.m_conversion) {
                    if (conversion.m_from == null || conversion.m_from.gameObject.name == ore) {
                        if (conversion.m_to != null) {
                            scaled = Mathf.Min(scaled, conversion.m_to.m_itemData.m_shared.m_maxStackSize);
                        }
                        break;
                    }
                }
                stack = scaled;
            }
        }

        // A fermenter drops m_producedItems copies in a loop, so the count lives on the conversion
        // rather than in a stack. Scaling it transiently - raise in the prefix, restore in the
        // finalizer - avoids permanently mutating prefab-derived data, so nothing compounds if the
        // method runs again and nothing is left modified if it throws partway.
        [HarmonyPatch(typeof(Fermenter), "DropAllItems")]
        class Fermenter_DropAllItems_Patch
        {
            static void Prefix(Fermenter __instance, out int[] __state) {
                __state = null;
                float multiplier = CustomFermenterOutputRate.Value;
                if (multiplier <= 1f || __instance.m_conversion == null) {
                    return;
                }
                var conversions = __instance.m_conversion;
                __state = new int[conversions.Count];
                for (int i = 0; i < conversions.Count; i++) {
                    __state[i] = conversions[i].m_producedItems;
                    conversions[i].m_producedItems = Mathf.Max(1, Mathf.RoundToInt(__state[i] * multiplier));
                }
            }

            static void Finalizer(Fermenter __instance, int[] __state) {
                if (__state == null || __instance.m_conversion == null) {
                    return;
                }
                var conversions = __instance.m_conversion;
                for (int i = 0; i < conversions.Count && i < __state.Length; i++) {
                    conversions[i].m_producedItems = __state[i];
                }
            }
        }

        // A cooking station spawns exactly one item per finished slot with no stack to scale, so
        // extra copies are produced by re-entering SpawnItem. The guard stops those re-entries
        // from each triggering this postfix again.
        [HarmonyPatch(typeof(CookingStation), "SpawnItem")]
        class CookingStation_SpawnItem_Patch
        {
            private static readonly MethodInfo SpawnItemMethod = AccessTools.Method(typeof(CookingStation), "SpawnItem");
            private static bool _spawningExtras;

            static void Postfix(CookingStation __instance, string name, int slot, Vector3 userPoint, bool cheated) {
                if (_spawningExtras || SpawnItemMethod == null) {
                    return;
                }
                int total = Mathf.Max(1, Mathf.RoundToInt(CustomCookingOutputRate.Value));
                if (total <= 1) {
                    return;
                }
                _spawningExtras = true;
                try {
                    for (int i = 1; i < total; i++) {
                        SpawnItemMethod.Invoke(__instance, new object[] { name, slot, userPoint, cheated });
                    }
                }
                finally {
                    _spawningExtras = false;
                }
            }
        }

        // Processing duration lives in a field each station reads while it ticks, so the same
        // transient scale-and-restore used for fermenter output applies: raise in the prefix,
        // put it back in the finalizer. Nothing prefab-derived is left modified, config changes
        // take effect immediately, and a throw mid-update can't strand a scaled value.
        //
        // Fuel is unaffected by design. UpdateSmelter burns m_secPerProduct / m_fuelPerProduct per
        // second over m_secPerProduct seconds, so fuel per item is always m_fuelPerProduct
        // regardless of how the time is scaled.
        [HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
        class Smelter_UpdateSmelter_Patch
        {
            static void Prefix(Smelter __instance, out float __state) {
                __state = __instance.m_secPerProduct;
                float multiplier = CustomProcessingTimeRate.Value;
                if (multiplier > 0f && multiplier != 1f) {
                    // UpdateSmelter treats a non-positive value as "disabled", so keep it above zero
                    __instance.m_secPerProduct = Mathf.Max(0.01f, __state * multiplier);
                }
            }

            static void Finalizer(Smelter __instance, float __state) {
                __instance.m_secPerProduct = __state;
            }
        }

        [HarmonyPatch(typeof(Fermenter), "GetStatus")]
        class Fermenter_GetStatus_Patch
        {
            static void Prefix(Fermenter __instance, out float __state) {
                __state = __instance.m_fermentationDuration;
                float multiplier = CustomProcessingTimeRate.Value;
                if (multiplier > 0f && multiplier != 1f) {
                    __instance.m_fermentationDuration = Mathf.Max(1f, __state * multiplier);
                }
            }

            static void Finalizer(Fermenter __instance, float __state) {
                __instance.m_fermentationDuration = __state;
            }
        }

        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        class CookingStation_UpdateCooking_Patch
        {
            static void Prefix(CookingStation __instance, out float[] __state) {
                __state = null;
                float multiplier = CustomProcessingTimeRate.Value;
                if (multiplier <= 0f || multiplier == 1f || __instance.m_conversion == null) {
                    return;
                }
                var conversions = __instance.m_conversion;
                __state = new float[conversions.Count];
                for (int i = 0; i < conversions.Count; i++) {
                    __state[i] = conversions[i].m_cookTime;
                    conversions[i].m_cookTime = Mathf.Max(0.1f, __state[i] * multiplier);
                }
            }

            static void Finalizer(CookingStation __instance, float[] __state) {
                if (__state == null || __instance.m_conversion == null) {
                    return;
                }
                var conversions = __instance.m_conversion;
                for (int i = 0; i < conversions.Count && i < __state.Length; i++) {
                    conversions[i].m_cookTime = __state[i];
                }
            }
        }

        [HarmonyPatch(typeof(CinematicsManager), "Awake")]
        class CinematicsManager_Awake_Patch
        {
            static void Postfix(CinematicsManager __instance) {
                if (SkipIntroCinematic.Value) {
                    __instance.m_introOnStartup = false;
                }
            }
        }

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
