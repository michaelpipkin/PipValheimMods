using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
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
        private static ConfigEntry<bool> DumpRealItemsOnly;
        private static ConfigEntry<bool> PassiveForsakenPowers;
        private static ConfigEntry<bool> HidePassivePowerIcons;

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
        private static ConfigEntry<bool> ShowClock;
        private static ConfigEntry<bool> Clock24Hour;
        private static ConfigEntry<bool> ClockShowDay;
        private static ConfigEntry<bool> ShowCoordinates;
        private static ConfigEntry<int> ClockFontSize;
        private static ConfigEntry<float> ClockPositionX;
        private static ConfigEntry<float> ClockPositionY;
        private static ConfigEntry<bool> CraftFromContainers;
        private static ConfigEntry<bool> FeedStationsFromContainers;
        private static ConfigEntry<KeyCode> FillStationKey;
        private static ConfigEntry<bool> ShowFillStationHint;
        private static ConfigEntry<bool> LogStationInputs;
        private static ConfigEntry<string> SmelterInputPriority;
        private static ConfigEntry<string> CookingStationInputPriority;
        private static ConfigEntry<float> ContainerRange;
        private static ConfigEntry<float> MinHealthPercent;
        private static ConfigEntry<bool> LogPlayerDamage;
        private static ConfigEntry<KeyCode> MassPlantKey;
        private static ConfigEntry<int> MassPlantGridSize;
        private static ConfigEntry<float> MassPlantSpacing;
        private static ConfigEntry<float> WorkstationRangeMultiplier;
        private static ConfigEntry<bool> WorldEffectsSoloOnly;
        private static ConfigEntry<float> TamingSpeedMultiplier;
        private static ConfigEntry<bool> AlwaysSlowFall;
        private static ConfigEntry<float> SlowFallMaxSpeed;
        private static ConfigEntry<bool> SlowFallNegatesFallDamage;
        private static ConfigEntry<KeyboardShortcut> PlayerLightHotkey;
        private static ConfigEntry<string> PlayerLightSourceItem;
        private static ConfigEntry<float> PlayerLightRange;
        private static ConfigEntry<float> PlayerLightIntensity;
        private static ConfigEntry<float> PlayerLightHeight;
        private static ConfigEntry<bool> WeightlessPlayerInventory;
        private static ConfigEntry<bool> SuppressPickupMessages;
        private static ConfigEntry<bool> SuppressRemovedMessages;
        private static ConfigEntry<bool> SuppressStationAddedMessages;
        private static ConfigEntry<bool> SuppressSkillMessages;
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

        // ---------------- Craft from nearby containers ----------------
        // Crafting, building, upgrading and the requirement display all decide what you have by
        // calling Inventory.CountItems on the player's own inventory. Rather than reimplementing
        // four checks, that count is widened to include nearby containers - but only while one of
        // those checks is actually running, tracked by _containerScopeDepth. Everything else that
        // counts items (the repair hotkey, fireplaces, offering bowls) still sees the real bag.
        private static readonly List<Container> _containers = new List<Container>();
        private static readonly List<Inventory> _nearbyInventories = new List<Inventory>();
        private static readonly List<ItemDrop.ItemData> _tempItems = new List<ItemDrop.ItemData>();
        private static readonly List<ItemDrop> _feedCandidates = new List<ItemDrop>();
        private static List<string> _smelterPriority = new List<string>();
        private static List<string> _cookingPriority = new List<string>();
        private static readonly List<string> _noPriority = new List<string>();
        private static readonly MethodInfo CheckAccessMethod = AccessTools.Method(typeof(Container), "CheckAccess");
        private static int _containerScopeDepth;
        private static bool _summingContainers;

        // Rebuilt whenever the configured font size changes
        private static GUIStyle _clockStyle;

        // Vanilla max stack size per item type, captured before we ever change it, so the
        // multiplier is applied to the original value rather than to our own previous result.
        private static readonly Dictionary<ItemDrop.ItemData.SharedData, int> _baseStackSizes =
            new Dictionary<ItemDrop.ItemData.SharedData, int>();

        // m_inventory is declared on Humanoid, not Player. AccessTools walks the base chain and
        // ignores access level; typeof(Player).GetField(..., NonPublic) does neither for a private
        // base-class field. Resolved once rather than on every hotkey press.
        private static readonly FieldInfo m_inventoryField = AccessTools.Field(typeof(Humanoid), "m_inventory");

        private void Awake()
        {
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
            ShowClock = Config.Bind("Clock", "ShowClock", false, "Show the in-game day and time on screen.");
            Clock24Hour = Config.Bind("Clock", "Clock24Hour", true, "Show the time as 24-hour (14:30) rather than 12-hour (2:30 PM).");
            ClockShowDay = Config.Bind("Clock", "ClockShowDay", true, "Include the day number in the clock.");
            ShowCoordinates = Config.Bind("Clock", "ShowCoordinates", false, "Show your position to the right of the clock. Altitude is measured from sea level, so 0 is the waterline. Shares the clock's position and font settings.");
            ClockFontSize = Config.Bind("Clock", "ClockFontSize", 18, new ConfigDescription("Font size of the clock text.", new AcceptableValueRange<int>(8, 48)));
            ClockPositionX = Config.Bind("Clock", "ClockPositionX", 12f, "Clock position in pixels from the left edge of the screen.");
            ClockPositionY = Config.Bind("Clock", "ClockPositionY", 12f, "Clock position in pixels from the top edge of the screen.");
            CraftFromContainers = Config.Bind("Containers", "CraftFromContainers", false, "Let crafting, building, upgrading and repairing draw materials from nearby containers instead of only your own inventory. Only containers you could open by hand are used, so wards and private chests are still respected.");
            FeedStationsFromContainers = Config.Bind("Containers", "FeedStationsFromContainers", false, "Let smelters, kilns, cooking stations, fermenters, fireplaces, turrets and shield generators take ore, fuel, food and ammo from nearby containers when you interact with them. Uses the same range as CraftFromContainers.");
            FillStationKey = Config.Bind("Containers", "FillStationKey", KeyCode.LeftShift, "Hold this key while interacting with a station to fill it in one go instead of adding a single item. Works whether the materials come from your inventory or from nearby containers.");
            LogStationInputs = Config.Bind("Containers", "LogStationInputs", false, "Diagnostic. Logs every input a hovered station accepts, in the order the priority list puts them, with how many of each you are carrying and how many are in nearby containers. Use it to check that the names in the priority lists match the prefab names the station actually uses.");
            ShowFillStationHint = Config.Bind("Containers", "ShowFillStationHint", true, "Add a line to a station's hover tooltip showing the fill shortcut.");
            const string priorityHelp = " Comma-separated prefab names, earlier meaning higher priority. Anything not listed keeps the station's own order, after everything that is listed. Leave empty to always use the station's own order. This only orders items within a source: whatever you are carrying is always used before anything in a container, so you can force a choice by putting it in your inventory.";
            SmelterInputPriority = Config.Bind("Containers", "SmelterInputPriority", "FlametalOreNew,FlametalOre,BlackMetalScrap,SilverOre,IronScrap,CopperOre,TinOre", "Which ore a smelter, kiln or blast furnace reaches for first when several are available." + priorityHelp);
            CookingStationInputPriority = Config.Bind("Containers", "CookingStationInputPriority", "SerpentMeat,LoxMeat,BugMeat,ChickenMeat,HareMeat,WolfMeat,DeerMeat,RawMeat,NeckTail,FishRaw", "Which raw item a cooking station or oven reaches for first when several are available. The default is ordered roughly by biome progression and is only a starting point - reorder it to taste." + priorityHelp);
            _smelterPriority = SmelterInputPriority.Value.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            _cookingPriority = CookingStationInputPriority.Value.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            ContainerRange = Config.Bind("Containers", "ContainerRange", 20f, "How far away, in metres, a container can be and still count toward crafting requirements.");
            MinHealthPercent = Config.Bind("General", "MinHealthPercent", 0.25f, new ConfigDescription("Damage can never take you below this fraction of your maximum health. 0.25 keeps you at a quarter health no matter how big the hit. Set to 0 to disable the floor and take damage normally.", new AcceptableValueRange<float>(0f, 0.95f)));
            LogPlayerDamage = Config.Bind("General", "LogPlayerDamage", false, "Diagnostic. Logs every hit that reaches the damage gate, with its type, the raw amount, what the gate allowed through and your health at the time. Use it to find out what actually killed you.");
            MassPlantKey = Config.Bind("Farming", "MassPlantKey", KeyCode.LeftShift, "Hold this while planting with the cultivator to plant a whole grid at once instead of a single seed.");
            MassPlantGridSize = Config.Bind("Farming", "MassPlantGridSize", 5, new ConfigDescription("Width of the grid planted while the mass-plant key is held. 5 plants a 5x5 block of 25. Set to 1 to disable.", new AcceptableValueRange<int>(1, 11)));
            PassiveForsakenPowers = Config.Bind("Powers", "PassiveForsakenPowers", true,
                "Keep every forsaken power this character has unlocked permanently active - no selecting, no activating, no cooldown. A power counts as unlocked once you have selected it at its stone, which the game records on the character rather than in the world, so unlocked powers follow you into any world. Powers carrying a networked status attribute (Moder's sailing power) stay off in a shared session, since that flag reaches any ship you are aboard.");
            HidePassivePowerIcons = Config.Bind("Powers", "HidePassivePowerIcons", true,
                "Keep passively held forsaken powers out of the status effect row. They are always on, so the icons carry no information and just crowd out effects that change. Only affects powers this mod is holding - a power you activate yourself, or one a nearby player grants you, still shows normally.");
            MassPlantSpacing = Config.Bind("Farming", "MassPlantSpacing", 2f, "Spacing between plants, as a multiple of the plant's own grow radius. 2 is the tightest that reliably clears each plant's space check, since the check tests a sphere of that radius against neighbouring colliders. Lower it to pack tighter at the risk of some seeds being rejected.");
            WorkstationRangeMultiplier = Config.Bind("General", "WorkstationRangeMultiplier", 1f, "Multiplier for how far a workbench, forge or other crafting station reaches for building. 2 doubles the radius. The on-screen coverage circle scales with it. Applies to stations as they load, so change it before entering a world.");
            WorldEffectsSoloOnly = Config.Bind("General", "WorldEffectsSoloOnly", true, "Suspend the settings that change shared world state - CustomResourceRate, CustomStackSizeMultiplier, CustomSmelterOutputRate, CustomProcessingTimeRate and TamingSpeedMultiplier - whenever anyone else is in the world with you. They resume automatically once you are alone again. Settings that only affect you are never suspended.");
            TamingSpeedMultiplier = Config.Bind("General", "TamingSpeedMultiplier", 1f, "How much faster animals tame. 2 is twice as fast, 10 is ten times. Applies to every tameable creature; the animal must still be fed and calm for progress to happen at all.");
            AlwaysSlowFall = Config.Bind("General", "AlwaysSlowFall", false, "Apply the Feather Cape's slow-fall effect permanently, whatever cape you are wearing.");
            SlowFallMaxSpeed = Config.Bind("General", "SlowFallMaxSpeed", 0f, "Maximum downward speed in metres per second while AlwaysSlowFall is on. 0 copies the Feather Cape's own value, so it behaves exactly like the cape.");
            SlowFallNegatesFallDamage = Config.Bind("General", "SlowFallNegatesFallDamage", true, "Also apply the Feather Cape's fall-damage reduction. Fall damage in Valheim is based on distance fallen, not speed, so capping the speed alone does not prevent it.");
            PlayerLightHotkey = Config.Bind("Hotkeys", "PlayerLightHotkey", new KeyboardShortcut(KeyCode.Semicolon), "Toggles a personal light on and off. Starts off each time the game launches.");
            PlayerLightSourceItem = Config.Bind("General", "PlayerLightSourceItem", "Torch", "Prefab name of the item whose light is copied for the personal light. Torch gives a warm point light that lights all around you; HelmetDverger gives the circlet's narrower forward beam.");
            PlayerLightRange = Config.Bind("General", "PlayerLightRange", 0f, "Light radius in metres. 0 copies the source item's own value.");
            PlayerLightIntensity = Config.Bind("General", "PlayerLightIntensity", 0f, "Light brightness. 0 copies the source item's own value.");
            PlayerLightHeight = Config.Bind("General", "PlayerLightHeight", 1.7f, "Height above your feet, in metres, that the light sits at. Roughly head height by default.");
            WeightlessPlayerInventory = Config.Bind("General", "WeightlessPlayerInventory", false, "Treat everything in your own inventory as weighing nothing, so slots are the only limit. Containers, carts and other players are unaffected. Makes CustomMaxCarryWeight irrelevant while enabled.");
            SuppressPickupMessages = Config.Bind("General", "SuppressPickupMessages", false, "Stop 'picked up <item>' notifications from being queued in the top-left message HUD, so they can't delay more important messages.");
            SuppressRemovedMessages = Config.Bind("General", "SuppressRemovedMessages", false, "Stop 'removed <item>' notifications from being queued in the top-left message HUD.");
            SuppressStationAddedMessages = Config.Bind("General", "SuppressStationAddedMessages", false, "Stop the centre-screen 'added <item>' confirmation shown when loading ore or fuel into a smelter, kiln, cooking station, turret or shield generator.");
            SuppressSkillMessages = Config.Bind("General", "SuppressSkillMessages", false, "Stop skill notifications from being queued in the message HUD. Covers both Valheim's own skill level-up messages and this mod's per-tick skill progress readout.");
            NegateKnockback = Config.Bind("General", "NegateKnockback", true, "Turn off knockback when hit");
            NegateEquipmentMovementPenalty = Config.Bind("General", "NegateEquipPenalty", true, "Turn off equipment movement penalty");

            RepairHotkey = Config.Bind("Hotkeys", "RepairHotkey", new KeyboardShortcut(KeyCode.LeftBracket), "Hotkey to repair all gear in inventory, heal player, replenish ammo, and spawn or replenish favorite foods");
            DumpRealItemsOnly = Config.Bind("General", "DumpRealItemsOnly", true, "Limit the item dump to real items. Creature attack prefabs carry plain text in place of a localisation token, and character customisation uses $customization, so both are dropped. Turn off to dump every entry in ObjectDB.");
            DumpItemListHotkey = Config.Bind("Hotkeys", "DumpItemListHotkey", new KeyboardShortcut(KeyCode.RightBracket), "Hotkey to dump every item prefab in the game to files in the BepInEx config folder. Must be pressed in a loaded world.");

            FavoriteFoodList = Config.Bind("Inventory", "FavoriteFoods", "MisthareSupreme,FishAndBread,SeekerAspic", "Comma-separated list of foods to spawn");
            _favoriteFoods = FavoriteFoodList.Value.Split(',').ToList();
            FavoriteAmmoList = Config.Bind("Inventory", "FavoriteAmmo", "ArrowCarapace,BoltCarapace", "Comma-separated list of ammo to replenish when repairing gear");
            _favoriteAmmo = FavoriteAmmoList.Value.Split(',').ToList();

            Game.isModded = true;

            harmony.PatchAll();
        }

        private void Update()
        {
            if (RepairHotkey.Value.IsDown())
            {
                // Read the local player at point of use; m_localPlayer isn't assigned until
                // SetLocalPlayer runs, which is after Player.Awake
                Player player = Player.m_localPlayer;
                if (player == null)
                {
                    return;
                }
                var inventory = (Inventory)m_inventoryField.GetValue(player);

                foreach (var item in inventory.GetAllItems().Where(i => i.IsEquipable() && i.m_durability < i.GetMaxDurability()))
                {
                    item.m_durability = item.GetMaxDurability();
                }
                foreach (var ammo in _favoriteAmmo)
                {
                    int count = 0;
                    var prefab = ZNetScene.instance.GetPrefab(ammo.Trim());
                    var itemData = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (inventory.ContainsItemByName(itemData.m_name))
                    {
                        count = inventory.CountItems(itemData.m_name);
                    }
                    if (count < itemData.m_maxStackSize)
                    {
                        inventory.AddItem(prefab, itemData.m_maxStackSize - count);
                    }
                }
                foreach (var food in _favoriteFoods)
                {
                    int count = 0;
                    var prefab = ZNetScene.instance.GetPrefab(food.Trim());
                    var itemData = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                    if (inventory.ContainsItemByName(itemData.m_name))
                    {
                        count = inventory.CountItems(itemData.m_name);
                    }
                    if (count < itemData.m_maxStackSize)
                    {
                        inventory.AddItem(prefab, itemData.m_maxStackSize - count);
                    }
                }
                if (player.GetHealthPercentage() < 1f)
                {
                    player.SetHealth(player.GetMaxHealth());
                }
                if (player.GetStaminaPercentage() < 1f)
                {
                    player.AddStamina(player.GetMaxStamina());
                }
                if (player.GetEitrPercentage() < 1f)
                {
                    player.AddEitr(player.GetMaxEitr());
                }
            }

            if (DumpItemListHotkey.Value.IsDown())
            {
                DumpItemList();
            }

            ApplyInventoryRows();
            UpdatePlayerLight();
            UpdateMassPlantPreview();
            ApplyResourceRate();
            RefreshStackSizeMultiplier();
            UpdatePassiveForsakenPowers();
        }

        // ---------------- Toggleable personal light ----------------
        // Unlike slow fall, this is not a status effect at all. VisEquipment.AttachArmor walks an
        // item prefab's "attach_" children and instantiates them onto the character model, so an
        // item's glow is simply a Light component living inside its own prefab hierarchy. There is
        // nothing to apply through SEMan - the light has to be recreated.
        //
        // Settings are copied off whichever item PlayerLightSourceItem names rather than invented,
        // so the result matches the real thing and follows any change the game makes to it. A torch
        // is a point light that illuminates all around; the Dvergr Circlet is a narrow forward spot,
        // which is why swapping the source item changes the character of the light so much.
        private static readonly FieldInfo LightFlickerLightField = AccessTools.Field(typeof(LightFlicker), "m_light");
        private static readonly FieldInfo LightFlickerBaseIntensityField = AccessTools.Field(typeof(LightFlicker), "m_baseIntensity");

        private static bool _playerLightOn;
        private static string _playerLightResolvedFor;
        private static float _playerLightSourceRange;
        private static float _playerLightSourceIntensity;
        private static Color _playerLightColor = Color.white;
        private static LightType _playerLightType = LightType.Point;
        private static GameObject _playerLightObject;
        private static Light _playerLight;

        private static void ResolvePlayerLightSource()
        {
            string itemName = PlayerLightSourceItem.Value;
            if (_playerLightResolvedFor == itemName)
            {
                return;
            }
            ObjectDB odb = ObjectDB.instance;
            if (odb == null)
            {
                return;   // not loaded yet; retry next frame
            }
            _playerLightResolvedFor = itemName;
            _playerLightSourceRange = 0f;
            _playerLightSourceIntensity = 0f;

            GameObject prefab = odb.GetItemPrefab(itemName);
            if (prefab == null)
            {
                Debug.LogWarning($"PlayerLightSourceItem: no item prefab named '{itemName}'");
                return;
            }
            // Prefabs and their attach nodes are inactive, so inactive children must be included
            Light source = prefab.GetComponentInChildren<Light>(includeInactive: true);
            if (source == null)
            {
                Debug.LogWarning($"PlayerLightSourceItem: '{itemName}' has no Light in its prefab");
                return;
            }
            _playerLightSourceRange = source.range;
            _playerLightColor = source.color;
            _playerLightType = source.type;

            // A flickering light's authored intensity is whatever the flicker happened to leave it
            // at, so prefer the value it oscillates around. Both LightFlicker fields are private in
            // the shipped assembly - reading them directly makes Mono refuse to compile this whole
            // method with a FieldAccessException, so they go through AccessTools.
            _playerLightSourceIntensity = source.intensity;
            if (LightFlickerLightField != null && LightFlickerBaseIntensityField != null)
            {
                foreach (LightFlicker flicker in prefab.GetComponentsInChildren<LightFlicker>(includeInactive: true))
                {
                    if (!ReferenceEquals(LightFlickerLightField.GetValue(flicker), source))
                    {
                        continue;
                    }
                    float baseIntensity = (float)LightFlickerBaseIntensityField.GetValue(flicker);
                    if (baseIntensity > 0f)
                    {
                        _playerLightSourceIntensity = baseIntensity;
                    }
                    break;
                }
            }
            Debug.Log($"Personal light from '{itemName}': type={_playerLightType}, range={_playerLightSourceRange}, intensity={_playerLightSourceIntensity}");
        }

        private static void DestroyPlayerLight()
        {
            if (_playerLightObject != null)
            {
                UnityEngine.Object.Destroy(_playerLightObject);
                _playerLightObject = null;
                _playerLight = null;
            }
        }

        private static void UpdatePlayerLight()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                DestroyPlayerLight();
                return;
            }

            if (PlayerLightHotkey.Value.IsDown())
            {
                _playerLightOn = !_playerLightOn;
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, _playerLightOn ? "Light on" : "Light off");
            }

            if (!_playerLightOn)
            {
                DestroyPlayerLight();
                return;
            }

            ResolvePlayerLightSource();
            float range = PlayerLightRange.Value > 0f ? PlayerLightRange.Value : _playerLightSourceRange;
            float intensity = PlayerLightIntensity.Value > 0f ? PlayerLightIntensity.Value : _playerLightSourceIntensity;
            if (range <= 0f)
            {
                return;   // couldn't read the source item and no override configured
            }

            if (_playerLightObject == null)
            {
                _playerLightObject = new GameObject("PipsMod_PlayerLight");
                _playerLight = _playerLightObject.AddComponent<Light>();
                _playerLight.type = _playerLightType;
                _playerLight.color = _playerLightColor;
                // Real-time point-light shadows are expensive and a carried torch doesn't cast them
                _playerLight.shadows = LightShadows.None;
            }
            // Re-parent after a respawn, which replaces the player object
            if (_playerLightObject.transform.parent != player.transform)
            {
                _playerLightObject.transform.SetParent(player.transform, worldPositionStays: false);
            }
            _playerLightObject.transform.localPosition = new Vector3(0f, PlayerLightHeight.Value, 0f);
            _playerLight.range = range;
            _playerLight.intensity = intensity;
        }

        // ---------------- On-screen clock ----------------
        // Drawn with IMGUI rather than by adding a label to Valheim's HUD hierarchy, because other
        // mods here rearrange that hierarchy (ExtraSlots resizes the inventory panel, ImprovedBuildHud
        // rebuilds the piece info) and a transform added into it is easy to lose or fight over.
        // The text is plain: no rich-text markup, so nothing can leak a colour tag as visible glyphs.
        private void OnGUI()
        {
            if ((!ShowClock.Value && !ShowCoordinates.Value) || Player.m_localPlayer == null || Hud.IsUserHidden())
            {
                return;
            }
            string line = BuildHudLine();
            if (string.IsNullOrEmpty(line))
            {
                return;
            }
            if (_clockStyle == null || _clockStyle.fontSize != ClockFontSize.Value)
            {
                // Inherits the skin's default upper-left alignment; setting it explicitly would
                // pull in UnityEngine.TextRenderingModule for no visual difference
                _clockStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = ClockFontSize.Value,
                };
                _clockStyle.normal.textColor = Color.white;
            }
            // Wide enough for clock and position together; the label is left-aligned so extra
            // width simply goes unused when only one of them is on
            var area = new Rect(ClockPositionX.Value, ClockPositionY.Value, 900f, ClockFontSize.Value * 2f);
            GUI.Label(area, line, _clockStyle);
        }

        /// <summary>
        /// Builds the single HUD line, joining whichever readouts are enabled so the position sits
        /// to the right of the clock and either can be turned off without leaving a gap.
        /// </summary>
        private static string BuildHudLine()
        {
            string clock = null;
            if (ShowClock.Value)
            {
                EnvMan env = EnvMan.instance;
                if (env != null && ZNet.instance != null)
                {
                    clock = BuildClockText(env);
                }
            }
            string coordinates = ShowCoordinates.Value ? BuildCoordinateText() : null;

            if (string.IsNullOrEmpty(clock))
            {
                return coordinates;
            }
            if (string.IsNullOrEmpty(coordinates))
            {
                return clock;
            }
            return clock + "      " + coordinates;
        }

        /// <summary>
        /// Altitude is reported relative to ZoneSystem's water level rather than raw world Y, so
        /// the waterline reads as 0 the way players expect. X and Z stay as world coordinates,
        /// which are the numbers the map and shared locations use.
        /// </summary>
        private static string BuildCoordinateText()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return null;
            }
            Vector3 position = player.transform.position;
            float seaLevel = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 0f;
            return $"X {position.x:0}   Y {position.z:0}   Alt {position.y - seaLevel:0}";
        }

        private static string BuildClockText(EnvMan env)
        {
            float fraction = Mathf.Repeat(env.GetDayFraction(), 1f);
            float hours = fraction * 24f;
            int hour = Mathf.FloorToInt(hours) % 24;
            int minute = Mathf.FloorToInt((hours - Mathf.Floor(hours)) * 60f);

            string time;
            if (Clock24Hour.Value)
            {
                time = $"{hour:00}:{minute:00}";
            }
            else
            {
                int hour12 = hour % 12;
                if (hour12 == 0)
                {
                    hour12 = 12;
                }
                time = $"{hour12}:{minute:00} {(hour < 12 ? "AM" : "PM")}";
            }

            // GetCurrentDay is non-public in the shipped assembly; GetDay is public and equivalent
            return ClockShowDay.Value
                ? $"Day {env.GetDay()}   {time}   {DayPhaseName(fraction)}"
                : $"{time}   {DayPhaseName(fraction)}";
        }

        /// <summary>
        /// Uses EnvMan's own thresholds so the label never disagrees with the game: it treats
        /// 0.25-0.75 of the day as daytime and 0.5-0.75 as afternoon, leaving 0.25-0.5 as morning.
        /// On that scale a day fraction maps directly onto a 24-hour clock, dawn landing at 06:00.
        /// </summary>
        private static string DayPhaseName(float fraction)
        {
            if (fraction < 0.25f)
            {
                return "Night";
            }
            if (fraction < 0.5f)
            {
                return "Morning";
            }
            if (fraction < 0.75f)
            {
                return "Afternoon";
            }
            return "Night";
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
        private static void ApplyInventoryRows()
        {
            int rows = CustomInventoryRows.Value;
            if (rows <= 0 || _inventoryRowsDisabled)
            {
                return;
            }
            // Checked here rather than in Awake because plugins load in sequence and Extra Slots
            // loads after this one, so it isn't registered yet while Awake is running.
            if (!_rowManagerChecked)
            {
                _rowManagerChecked = true;
                if (Chainloader.PluginInfos.ContainsKey(ExtraSlotsGuid))
                {
                    _inventoryRowsDisabled = true;
                    Debug.LogWarning($"CustomInventoryRows is {rows}, but Extra Slots is installed and manages inventory rows itself. Standing down to avoid fighting it. Set CustomInventoryRows to 0 and use 'Amount of extra inventory rows' under [Extra slots] in shudnal.ExtraSlots.cfg instead.");
                    return;
                }
            }
            Player player = Player.m_localPlayer;
            if (player == null || InventoryGui.instance == null)
            {
                return;
            }
            Inventory inventory = player.GetInventory();
            if (inventory == null || inventory.GetHeight() == rows)
            {
                return;
            }
            if (_inventoryRowAttempts >= MaxInventoryRowAttempts)
            {
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
        private static void DumpItemList()
        {
            ObjectDB odb = ObjectDB.instance;
            if (odb == null || odb.m_items == null || odb.m_items.Count == 0)
            {
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "Item list unavailable - load a world first");
                return;
            }

            var rows = new List<ItemRow>();
            foreach (var prefab in odb.m_items)
            {
                if (prefab == null)
                {
                    continue;
                }
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                {
                    continue;
                }
                var shared = drop.m_itemData.m_shared;
                // Localization can be unavailable very early; fall back to the raw token
                string display = Localization.instance != null
                    ? Localization.instance.Localize(shared.m_name)
                    : shared.m_name;
                rows.Add(new ItemRow
                {
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
                    // VariantDialog.Setup loops to m_variants but indexes m_icons, so an item
                    // where these disagree throws when its variant picker is opened
                    Variants = shared.m_variants,
                    IconCount = shared.m_icons != null ? shared.m_icons.Length : 0,
                });
            }
            rows.Sort((a, b) => string.Compare(a.PrefabName, b.PrefabName, StringComparison.OrdinalIgnoreCase));

            string dir = Paths.ConfigPath;

            // Reference table: prefab name is the id the "spawn" console command takes
            var table = new StringBuilder();
            table.AppendLine("PrefabName\tDisplayName\tNameToken\tItemType\tAutoPickup\tInZNetScene\tMaxStack\tWeight\tVariants\tIcons");
            int skipped = 0;
            foreach (var r in rows)
            {
                if (DumpRealItemsOnly.Value && !IsRealItemRow(r))
                {
                    skipped++;
                    continue;
                }
                table.AppendLine($"{r.PrefabName}\t{r.DisplayName}\t{r.NameToken}\t{r.ItemType}\t{r.AutoPickup}\t{r.InZNetScene}\t{r.MaxStackSize}\t{r.Weight:0.##}\t{r.Variants}\t{r.IconCount}");
            }
            string tablePath = Path.Combine(dir, "PipsMod_ItemList.tsv");
            File.WriteAllText(tablePath, table.ToString());

            // Deliberately scans every row, not just the written ones: a mismatched item that the
            // filter excluded would still crash if its picker were opened, so hiding it would be
            // the one case where the filter could cost us the answer.
            // Called out rather than left for the reader to spot: any item whose variant count
            // exceeds its icon count throws IndexOutOfRangeException inside VariantDialog.Setup
            // the moment its variant picker is opened.
            foreach (var r in rows)
            {
                if (r.Variants > r.IconCount)
                {
                    Debug.LogWarning($"Variant/icon mismatch: {r.PrefabName} ({r.DisplayName}) declares {r.Variants} variants but has {r.IconCount} icons - opening its variant picker will throw");
                }
            }

            // An item can only be ignored if it auto-picks-up AND can exist as a world drop.
            // Creature attack prefabs and cosmetics live in ObjectDB but never in ZNetScene.
            // Every entry is commented out with #, matching AutoPickupIgnorer's default convention.
            var eligible = rows.Where(r => r.AutoPickup && r.InZNetScene).Select(r => "#" + r.PrefabName).ToList();
            string listPath = Path.Combine(dir, "PipsMod_AutoPickupIgnoreList.txt");
            File.WriteAllText(listPath, string.Join(", ", eligible));

            Debug.Log($"Dumped {rows.Count - skipped} items ({skipped} filtered out, {eligible.Count} auto-pickup) to {dir}");
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                $"Dumped {rows.Count} items ({eligible.Count} auto-pickup) to BepInEx/config");
        }

        /// <summary>
        /// A real item always carries a localisation token. Creature attack prefabs hold plain
        /// English in m_name instead ("Swing attack", "slap"), and character customisation uses
        /// $customization - neither is obtainable. Fish ($animal_), the Hooded Lantern ($piece_)
        /// and the Kvastur trophy ($enemy_) are real items that don't use $item_, which is why the
        /// test is "has a token at all" rather than the narrower "$item_".
        /// </summary>
        private static bool IsRealItemRow(ItemRow row)
        {
            return !string.IsNullOrEmpty(row.NameToken)
                && row.NameToken.StartsWith("$", StringComparison.Ordinal)
                && !row.NameToken.StartsWith("$customization", StringComparison.Ordinal);
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
            public int Variants;
            public int IconCount;
        }

        [HarmonyPatch(typeof(MessageHud), "Awake")]
        class MessageHud_Awake_Patch
        {
            [HarmonyPostfix]
            static void GetMessageHud(ref MessageHud ___m_instance)
            {
                _messageHud = ___m_instance;
            }
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        class Player_Awake_Patch
        {
            static void Postfix(ref float ___m_maxCarryWeight)
            {
                Debug.Log($"Setting base maximum carry weight.");
                ___m_maxCarryWeight = (float)CustomMaxCarryWeight.Value;
                Debug.Log($"Base max carry weight: {___m_maxCarryWeight}");
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
            static void Postfix()
            {
                GuardLoadPath("resource rate", () =>
                {
                    // Whatever the world itself decided, kept so the boost can be withdrawn later
                    _vanillaResourceRate = Game.m_resourceRate;
                    ApplyResourceRate();
                });
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
            static void Postfix(ObjectDB __instance)
            {
                GuardLoadPath("stack size multiplier", () => ApplyStackSizeMultiplier(__instance));
                GuardLoadPath("variant clamp", () => ClampVariantCounts(__instance));
            }
        }

        /// <summary>
        /// Runs a load-path patch body so that a bug in it degrades to a logged error instead of
        /// an unbootable game. Harmony lets an exception thrown in a postfix propagate into the
        /// method it patched, so anything that throws here tears down the vanilla call that led
        /// here. ObjectDB.UpdateRegisters is reached from FejdStartup.Start via SetupObjectDB, so
        /// a throw aborts startup itself: the character list silently falls back to the "Ragnar"
        /// placeholder and the Start button stops responding, with nothing in the log pointing at
        /// the mod. Degrading instead costs one broken feature rather than the whole game.
        /// </summary>
        private static void GuardLoadPath(string what, Action body)
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                Debug.LogError($"PipsMod: {what} failed, continuing without it - {ex}");
            }
        }

        /// <summary>
        /// Works around a vanilla data bug: VariantDialog.Setup loops to m_shared.m_variants while
        /// indexing m_shared.m_icons, with no bounds check, so an item declaring more variants than
        /// it has icons throws IndexOutOfRangeException the moment its variant picker opens. As of
        /// 1.0 exactly one item is affected - ShieldRoots (Shield of Roots) claims 4 variants with
        /// 3 icons. Clamping the count hides the variant that has no icon, which was unreachable
        /// anyway, and lets the picker open. Appearance only, so this is not gated on a solo
        /// session; it is also idempotent, since the clamped value already matches the icon count.
        /// </summary>
        private static void ClampVariantCounts(ObjectDB odb)
        {
            if (odb == null || odb.m_items == null)
            {
                return;
            }
            foreach (var prefab in odb.m_items)
            {
                if (prefab == null)
                {
                    continue;
                }
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                {
                    continue;
                }
                var shared = drop.m_itemData.m_shared;
                int icons = shared.m_icons != null ? shared.m_icons.Length : 0;
                if (shared.m_variants > icons)
                {
                    Debug.Log($"Clamped {prefab.name} variants {shared.m_variants} -> {icons} to match its icon count");
                    shared.m_variants = icons;
                }
            }
        }

        /// <summary>
        /// Scales every stackable item's max stack size. Safe to call repeatedly: the game's own
        /// value is remembered the first time each item is seen, so the multiplier is always
        /// recomputed from that baseline instead of compounding on each call. Setting the
        /// multiplier back to 1 therefore restores vanilla stack sizes.
        /// </summary>
        private static void ApplyStackSizeMultiplier(ObjectDB odb)
        {
            if (odb == null || odb.m_items == null)
            {
                return;
            }
            float multiplier = SoloOnlyRate(CustomStackSizeMultiplier);
            _appliedStackMultiplier = multiplier;
            int changed = 0;
            foreach (var prefab in odb.m_items)
            {
                if (prefab == null)
                {
                    continue;
                }
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                {
                    continue;
                }
                var shared = drop.m_itemData.m_shared;
                if (!_baseStackSizes.TryGetValue(shared, out int baseSize))
                {
                    baseSize = shared.m_maxStackSize;
                    _baseStackSizes[shared] = baseSize;
                }
                // Equipment and other one-per-slot items stay unstackable
                if (baseSize <= 1)
                {
                    continue;
                }
                int target = Mathf.Max(1, Mathf.RoundToInt(baseSize * multiplier));
                if (shared.m_maxStackSize != target)
                {
                    shared.m_maxStackSize = target;
                    changed++;
                }
            }
            if (changed > 0)
            {
                Debug.Log($"Stack size multiplier {multiplier:0.##} applied to {changed} item(s)");
            }
        }

        // Every smelting station routes its output through Smelter.Spawn, both the one-at-a-time
        // path (QueueProcessed calls Spawn(ore, 1) when m_spawnStack is off, which is what the
        // charcoal kiln does) and the batched path via SpawnProcessed. Scaling the stack here
        // therefore covers all of them, and leaves input cost untouched.
        [HarmonyPatch(typeof(Smelter), "Spawn")]
        class Smelter_Spawn_Patch
        {
            static void Prefix(Smelter __instance, string ore, ref int stack)
            {
                float multiplier = SoloOnlyRate(CustomSmelterOutputRate);
                if (multiplier <= 1f || stack <= 0)
                {
                    return;
                }
                int scaled = Mathf.Max(1, Mathf.RoundToInt(stack * multiplier));

                // Cap at what the produced item can actually hold in one stack. GetItemConversion
                // is non-public in the shipped assembly, so match on m_conversion the same way.
                foreach (var conversion in __instance.m_conversion)
                {
                    if (conversion.m_from == null || conversion.m_from.gameObject.name == ore)
                    {
                        if (conversion.m_to != null)
                        {
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
            static void Prefix(Fermenter __instance, out int[] __state)
            {
                __state = null;
                float multiplier = CustomFermenterOutputRate.Value;
                if (multiplier <= 1f || __instance.m_conversion == null)
                {
                    return;
                }
                var conversions = __instance.m_conversion;
                __state = new int[conversions.Count];
                for (int i = 0; i < conversions.Count; i++)
                {
                    __state[i] = conversions[i].m_producedItems;
                    conversions[i].m_producedItems = Mathf.Max(1, Mathf.RoundToInt(__state[i] * multiplier));
                }
            }

            static void Finalizer(Fermenter __instance, int[] __state)
            {
                if (__state == null || __instance.m_conversion == null)
                {
                    return;
                }
                var conversions = __instance.m_conversion;
                for (int i = 0; i < conversions.Count && i < __state.Length; i++)
                {
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

            static void Postfix(CookingStation __instance, string name, int slot, Vector3 userPoint, bool cheated)
            {
                if (_spawningExtras || SpawnItemMethod == null)
                {
                    return;
                }
                int total = Mathf.Max(1, Mathf.RoundToInt(CustomCookingOutputRate.Value));
                if (total <= 1)
                {
                    return;
                }
                _spawningExtras = true;
                try
                {
                    for (int i = 1; i < total; i++)
                    {
                        SpawnItemMethod.Invoke(__instance, new object[] { name, slot, userPoint, cheated });
                    }
                }
                finally
                {
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
            static void Prefix(Smelter __instance, out float __state)
            {
                __state = __instance.m_secPerProduct;
                float multiplier = SoloOnlyRate(CustomProcessingTimeRate);
                if (multiplier > 0f && multiplier != 1f)
                {
                    // UpdateSmelter treats a non-positive value as "disabled", so keep it above zero
                    __instance.m_secPerProduct = Mathf.Max(0.01f, __state * multiplier);
                }
            }

            static void Finalizer(Smelter __instance, float __state)
            {
                __instance.m_secPerProduct = __state;
            }
        }

        [HarmonyPatch(typeof(Fermenter), "GetStatus")]
        class Fermenter_GetStatus_Patch
        {
            static void Prefix(Fermenter __instance, out float __state)
            {
                __state = __instance.m_fermentationDuration;
                float multiplier = SoloOnlyRate(CustomProcessingTimeRate);
                if (multiplier > 0f && multiplier != 1f)
                {
                    __instance.m_fermentationDuration = Mathf.Max(1f, __state * multiplier);
                }
            }

            static void Finalizer(Fermenter __instance, float __state)
            {
                __instance.m_fermentationDuration = __state;
            }
        }

        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        class CookingStation_UpdateCooking_Patch
        {
            static void Prefix(CookingStation __instance, out float[] __state)
            {
                __state = null;
                float multiplier = SoloOnlyRate(CustomProcessingTimeRate);
                if (multiplier <= 0f || multiplier == 1f || __instance.m_conversion == null)
                {
                    return;
                }
                var conversions = __instance.m_conversion;
                __state = new float[conversions.Count];
                for (int i = 0; i < conversions.Count; i++)
                {
                    __state[i] = conversions[i].m_cookTime;
                    conversions[i].m_cookTime = Mathf.Max(0.1f, __state[i] * multiplier);
                }
            }

            static void Finalizer(CookingStation __instance, float[] __state)
            {
                if (__state == null || __instance.m_conversion == null)
                {
                    return;
                }
                var conversions = __instance.m_conversion;
                for (int i = 0; i < conversions.Count && i < __state.Length; i++)
                {
                    conversions[i].m_cookTime = __state[i];
                }
            }
        }

        // ShowPickupMessage exists only to queue the "picked up <item>" notification, so skipping it
        // drops the message before it ever reaches the HUD queue rather than letting it queue and
        // then hiding it. Deliberately targets Character, not Player: the method is not virtual.
        [HarmonyPatch(typeof(Container), "Awake")]
        class Container_Awake_Patch
        {
            static void Postfix(Container __instance)
            {
                if (!_containers.Contains(__instance))
                {
                    _containers.Add(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Container), "OnDestroyed")]
        class Container_OnDestroyed_Patch
        {
            static void Postfix(Container __instance)
            {
                _containers.Remove(__instance);
            }
        }

        /// <summary>
        /// Fills _nearbyInventories with every loaded container in range that the player is allowed
        /// to open. Destroyed entries are pruned while walking the list, because unloading a zone
        /// destroys the object without calling OnDestroyed.
        /// </summary>
        private static void CollectNearbyInventories(Player player)
        {
            _nearbyInventories.Clear();
            float rangeSqr = ContainerRange.Value * ContainerRange.Value;
            Vector3 origin = player.transform.position;
            long playerId = player.GetPlayerID();
            for (int i = _containers.Count - 1; i >= 0; i--)
            {
                Container container = _containers[i];
                if (container == null)
                {
                    _containers.RemoveAt(i);
                    continue;
                }
                if ((container.transform.position - origin).sqrMagnitude > rangeSqr)
                {
                    continue;
                }
                // Checked before CheckAccess, not after. Container.Awake only builds the inventory
                // and resolves m_piece when the object has a ZDO, so a placement ghost - the
                // preview shown while positioning a piece - has neither. CheckAccess dereferences
                // m_piece for a private chest, so calling it on a ghost throws.
                Inventory inventory = container.GetInventory();
                if (inventory == null)
                {
                    continue;
                }
                // CheckAccess covers wards and private chests, so this never reaches into
                // something the player couldn't walk up and open. It's non-public, hence AccessTools.
                try
                {
                    if (CheckAccessMethod != null && !(bool)CheckAccessMethod.Invoke(container, new object[] { playerId }))
                    {
                        continue;
                    }
                }
                catch (Exception)
                {
                    continue;   // a container that can't answer is one we shouldn't be reaching into
                }
                _nearbyInventories.Add(inventory);
            }
        }

        private static bool BeginContainerScope()
        {
            if (!CraftFromContainers.Value || Player.m_localPlayer == null)
            {
                return false;
            }
            if (_containerScopeDepth == 0)
            {
                CollectNearbyInventories(Player.m_localPlayer);
            }
            _containerScopeDepth++;
            return true;
        }

        private static void EndContainerScope(bool entered)
        {
            if (entered && _containerScopeDepth > 0)
            {
                _containerScopeDepth--;
            }
        }

        private static bool IsLocalPlayerInventory(Inventory inventory)
        {
            Player player = Player.m_localPlayer;
            return player != null && ReferenceEquals(inventory, player.GetInventory());
        }

        // Scope markers. Each wraps one of the methods that asks "do I have the materials", so the
        // widened count applies there and nowhere else.
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
        class Player_HaveRequirementItems_Patch
        {
            static void Prefix(out bool __state) { __state = BeginContainerScope(); }
            static void Finalizer(bool __state) { EndContainerScope(__state); }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        class Player_HaveRequirementsPiece_Patch
        {
            static void Prefix(out bool __state) { __state = BeginContainerScope(); }
            static void Finalizer(bool __state) { EndContainerScope(__state); }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
        class Player_GetFirstRequiredItem_Patch
        {
            static void Prefix(out bool __state) { __state = BeginContainerScope(); }
            static void Finalizer(bool __state) { EndContainerScope(__state); }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
        class InventoryGui_SetupRequirement_Patch
        {
            static void Prefix(out bool __state) { __state = BeginContainerScope(); }
            static void Finalizer(bool __state) { EndContainerScope(__state); }
        }

        // The single place the widening actually happens.
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
        class Inventory_CountItems_Patch
        {
            static void Postfix(Inventory __instance, string name, int quality, ref int __result)
            {
                if (_containerScopeDepth <= 0 || _summingContainers || !IsLocalPlayerInventory(__instance))
                {
                    return;
                }
                // Counting the containers re-enters this method; the flag stops it recursing
                _summingContainers = true;
                try
                {
                    for (int i = 0; i < _nearbyInventories.Count; i++)
                    {
                        __result += _nearbyInventories[i].CountItems(name, quality);
                    }
                }
                finally
                {
                    _summingContainers = false;
                }
            }
        }

        // HaveRequirements uses HaveItem rather than CountItems for its CanAlmostBuild mode.
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveItem), new Type[] { typeof(string), typeof(bool) })]
        class Inventory_HaveItem_Patch
        {
            static void Postfix(Inventory __instance, string name, ref bool __result)
            {
                if (__result || _containerScopeDepth <= 0 || _summingContainers || !IsLocalPlayerInventory(__instance))
                {
                    return;
                }
                _summingContainers = true;
                try
                {
                    for (int i = 0; i < _nearbyInventories.Count; i++)
                    {
                        if (_nearbyInventories[i].HaveItem(name))
                        {
                            __result = true;
                            return;
                        }
                    }
                }
                finally
                {
                    _summingContainers = false;
                }
            }
        }

        // Consumption isn't reimplemented. ConsumeResources removes from the player's own
        // inventory, so any shortfall is moved out of nearby containers first and vanilla then
        // does the removal exactly as it always would - keeping quality, upgrader and multiplier
        // handling intact instead of duplicating it here.
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        class Player_ConsumeResources_Patch
        {
            static void Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
            {
                if (!CraftFromContainers.Value || requirements == null)
                {
                    return;
                }
                if (!ReferenceEquals(__instance, Player.m_localPlayer))
                {
                    return;
                }
                CollectNearbyInventories(__instance);
                if (_nearbyInventories.Count == 0)
                {
                    return;
                }
                Inventory playerInventory = __instance.GetInventory();
                if (playerInventory == null)
                {
                    return;
                }
                CraftingStation station = __instance.GetCurrentCraftingStation();
                foreach (Piece.Requirement requirement in requirements)
                {
                    if (requirement == null || !requirement.m_resItem)
                    {
                        continue;
                    }
                    // Mirrors the station/upgrader filter in ConsumeResources so nothing is pulled
                    // for a requirement vanilla is about to skip
                    if ((station != null && station.m_upgrader != requirement.m_upgraderResource)
                        || (station == null && requirement.m_upgraderResource))
                    {
                        continue;
                    }
                    int needed = requirement.GetAmount(qualityLevel) * multiplier;
                    if (needed <= 0)
                    {
                        continue;
                    }
                    string itemName = requirement.m_resItem.m_itemData.m_shared.m_name;
                    int shortfall = needed - playerInventory.CountItems(itemName, itemQuality);
                    if (shortfall > 0)
                    {
                        PullFromContainers(playerInventory, itemName, itemQuality, shortfall);
                    }
                }
            }
        }

        /// <summary>
        /// Moves up to <paramref name="amount"/> of an item out of nearby containers and into the
        /// player's inventory. Stops early if the inventory has no room, which leaves the shortfall
        /// in place and lets vanilla fail the craft rather than silently destroying anything.
        /// </summary>
        private static int PullFromContainers(Inventory playerInventory, string itemName, int itemQuality, int amount)
        {
            int wanted = amount;
            for (int i = 0; i < _nearbyInventories.Count && amount > 0; i++)
            {
                Inventory source = _nearbyInventories[i];
                _tempItems.Clear();
                source.GetAllItems(itemName, _tempItems);
                for (int j = _tempItems.Count - 1; j >= 0 && amount > 0; j--)
                {
                    ItemDrop.ItemData item = _tempItems[j];
                    if (item == null || item.m_stack <= 0)
                    {
                        continue;
                    }
                    if (itemQuality >= 0 && item.m_quality != itemQuality)
                    {
                        continue;
                    }
                    int take = Mathf.Min(amount, item.m_stack);
                    ItemDrop.ItemData moved = item.Clone();
                    moved.m_stack = take;
                    if (!playerInventory.AddItem(moved))
                    {
                        return wanted - amount;
                    }
                    source.RemoveItem(item, take);
                    amount -= take;
                }
            }
            return wanted - amount;
        }

        /// <summary>
        /// Shared entry point for the station patches: verifies the feature is on, resolves the
        /// local player's inventory and refreshes the nearby-container list. Returns false when
        /// there's nothing to do, so each patch stays a couple of lines.
        /// </summary>
        private static bool BeginStationFeed(Humanoid user, out Inventory playerInventory)
        {
            playerInventory = null;
            if (!FeedStationsFromContainers.Value)
            {
                return false;
            }
            Player player = Player.m_localPlayer;
            if (player == null || !ReferenceEquals(user, player))
            {
                return false;
            }
            playerInventory = player.GetInventory();
            if (playerInventory == null)
            {
                return false;
            }
            CollectNearbyInventories(player);
            return _nearbyInventories.Count > 0;
        }

        /// <summary>
        /// Makes sure the player is holding at least one of <paramref name="itemName"/>, pulling a
        /// single one out of a nearby container if not. The station code then finds and removes it
        /// from the player's own inventory exactly as it normally would.
        /// </summary>
        private static bool EnsureOneInInventory(Inventory playerInventory, string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }
            if (playerInventory.HaveItem(itemName))
            {
                return true;
            }
            return PullFromContainers(playerInventory, itemName, -1, 1) > 0;
        }

        /// <summary>
        /// Used by the "find something to process" patches. Walks the station's own conversion list
        /// and pulls in the first candidate a nearby container can supply, returning the item as it
        /// now exists in the player's inventory - which is what the caller goes on to remove.
        /// </summary>
        // Stations check their own capacity *after* searching the inventory - Smelter.OnAddOre calls
        // FindCookableItem first and only then tests GetQueueSize() >= m_maxOre. Pulling from a
        // container during that search therefore leaves one item stranded in the inventory when the
        // station turns out to be full. These predicates let the pull be skipped instead, which also
        // covers pressing the key once against an already-full station.
        private static readonly MethodInfo CookingGetFuelMethod = AccessTools.Method(typeof(CookingStation), "GetFuel");
        private static readonly MethodInfo ShieldGeneratorGetFuelMethod = AccessTools.Method(typeof(ShieldGenerator), "GetFuel");
        private static readonly MethodInfo FermenterGetContentMethod = AccessTools.Method(typeof(Fermenter), "GetContent");
        private static readonly FieldInfo FireplaceNviewField = AccessTools.Field(typeof(Fireplace), "m_nview");

        private static bool SmelterHasOreRoom(Smelter smelter)
        {
            return SmelterGetQueueSizeMethod != null
                && (int)SmelterGetQueueSizeMethod.Invoke(smelter, null) < smelter.m_maxOre;
        }

        private static bool SmelterHasFuelRoom(Smelter smelter)
        {
            return SmelterGetFuelMethod != null
                && (float)SmelterGetFuelMethod.Invoke(smelter, null) <= smelter.m_maxFuel - 1f;
        }

        private static bool CookingHasFuelRoom(CookingStation station)
        {
            return CookingGetFuelMethod != null
                && (float)CookingGetFuelMethod.Invoke(station, null) <= station.m_maxFuel - 1f;
        }

        private static bool FireplaceHasFuelRoom(Fireplace fireplace)
        {
            ZNetView nview = FireplaceNviewField?.GetValue(fireplace) as ZNetView;
            if (nview == null || !nview.IsValid())
            {
                return false;
            }
            return Mathf.CeilToInt(nview.GetZDO().GetFloat(ZDOVars.s_fuel)) < fireplace.m_maxFuel;
        }

        private static bool TurretHasAmmoRoom(Turret turret)
        {
            // m_maxAmmo of 0 means the turret has no declared limit
            return turret.m_maxAmmo <= 0 || turret.GetAmmo() < turret.m_maxAmmo;
        }

        private static bool ShieldGeneratorHasFuelRoom(ShieldGenerator generator)
        {
            return ShieldGeneratorGetFuelMethod != null
                && (float)ShieldGeneratorGetFuelMethod.Invoke(generator, null) <= generator.m_maxFuel - 1f;
        }

        private static bool FermenterIsEmpty(Fermenter fermenter)
        {
            return FermenterGetContentMethod == null
                || (int)FermenterGetContentMethod.Invoke(fermenter, null) == 0;
        }

        /// <summary>
        /// Walks the candidates in priority order, then in the station's own order for anything not
        /// on the list, returning the first that <paramref name="resolve"/> can supply.
        /// </summary>
        private static ItemDrop.ItemData SelectByPriority(List<ItemDrop> candidates, List<string> priority, Func<ItemDrop, ItemDrop.ItemData> resolve)
        {
            for (int p = 0; p < priority.Count; p++)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    ItemDrop candidate = candidates[i];
                    if (candidate == null || candidate.gameObject.name != priority[p])
                    {
                        continue;
                    }
                    ItemDrop.ItemData found = resolve(candidate);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == null)
                {
                    continue;
                }
                ItemDrop.ItemData found = resolve(candidates[i]);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Decides which of a station's accepted inputs to use.
        ///
        /// Carried stock always wins: the inventory is searched completely before any container is
        /// touched, so putting iron scrap in your pocket forces iron even with a chest full of
        /// higher-priority flametal beside you. The priority list only orders the candidates
        /// *within* each of those two passes - it never lets a container outrank your own bag.
        ///
        /// Vanilla picks the first entry in the station's conversion list the player is carrying,
        /// which is why a smelter reaches for copper and ignores everything else.
        /// </summary>
        private static ItemDrop.ItemData ChooseStationInput(Inventory inventory, List<ItemDrop> candidates, List<string> priority, bool stationHasRoom, ItemDrop.ItemData vanillaChoice)
        {
            Player player = Player.m_localPlayer;
            if (player == null || candidates.Count == 0 || !ReferenceEquals(inventory, player.GetInventory()))
            {
                return vanillaChoice;
            }

            // Pass 1 - anything already carried, best first
            ItemDrop.ItemData carried = SelectByPriority(candidates, priority,
                candidate => inventory.GetItem(candidate.m_itemData.m_shared.m_name));
            if (carried != null)
            {
                return carried;
            }
            if (vanillaChoice != null)
            {
                return vanillaChoice;
            }

            // Pass 2 - nothing carried, so reach into nearby containers, best first. Skipped when
            // the station is full, or the pulled item would be stranded in the inventory.
            if (!stationHasRoom || !BeginStationFeed(player, out _))
            {
                return null;
            }
            return SelectByPriority(candidates, priority, candidate =>
            {
                string sharedName = candidate.m_itemData.m_shared.m_name;
                return PullFromContainers(inventory, sharedName, -1, 1) > 0
                    ? inventory.GetItem(sharedName)
                    : null;
            });
        }

        // ---------------- Fill a station in one interaction ----------------
        // Every "add one item" entry point returns bool, so filling is just repeating the call
        // until the station refuses. That deliberately reuses each station's own capacity and
        // availability checks ("it's full", "don't have any") as the stopping condition instead of
        // reading m_maxOre / m_maxFuel and duplicating the arithmetic per station type.
        //
        // The repeat passes null for the item argument so the station re-searches the inventory
        // each pass, which is also what lets the container feeding above top it up as it goes.
        private const int MaxStationFillSteps = 500;
        private static bool _fillingStation;
        private static bool _suppressFillMessages;

        private static readonly MethodInfo SmelterAddOreMethod = AccessTools.Method(typeof(Smelter), "OnAddOre");
        private static readonly MethodInfo SmelterAddFuelMethod = AccessTools.Method(typeof(Smelter), "OnAddFuel");
        private static readonly MethodInfo CookingInteractMethod = AccessTools.Method(typeof(CookingStation), "OnInteract");
        private static readonly MethodInfo CookingAddFuelMethod = AccessTools.Method(typeof(CookingStation), "OnAddFuelSwitch");
        private static readonly MethodInfo FireplaceInteractMethod = AccessTools.Method(typeof(Fireplace), "Interact");
        private static readonly MethodInfo TurretUseItemMethod = AccessTools.Method(typeof(Turret), "UseItem");
        private static readonly MethodInfo ShieldGeneratorAddFuelMethod = AccessTools.Method(typeof(ShieldGenerator), "OnAddFuel");

        private static bool ShouldFillStation(bool addSucceeded)
        {
            return addSucceeded && !_fillingStation && Input.GetKey(FillStationKey.Value);
        }

        /// <param name="sameModeStill">
        /// Optional guard checked before each repeat. CookingStation.OnInteract both unloads cooked
        /// food and loads raw food, returning true either way, so without this a single fill would
        /// empty the station and immediately refill it. The guard stops the repeat at the moment the
        /// station would switch from one job to the other.
        /// </param>
        private static void RunStationFill(MethodInfo addOne, object station, object[] args, Func<bool> sameModeStill = null)
        {
            if (addOne == null)
            {
                return;
            }
            _fillingStation = true;
            // The repeated adds each announce themselves centre-screen; one summary is plenty
            _suppressFillMessages = true;
            int added = 0;
            try
            {
                while (added < MaxStationFillSteps)
                {
                    if (sameModeStill != null && !sameModeStill())
                    {
                        break;
                    }
                    if (!(addOne.Invoke(station, args) is bool success) || !success)
                    {
                        break;
                    }
                    added++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Station fill stopped early: {ex.Message}");
            }
            finally
            {
                _fillingStation = false;
                _suppressFillMessages = false;
            }
            if (added > 0 && _messageHud != null)
            {
                _messageHud.ShowMessage(MessageHud.MessageType.Center, $"Added {added + 1}");
            }
        }

        [HarmonyPatch(typeof(Smelter), "OnAddOre")]
        class Smelter_OnAddOre_FillPatch
        {
            static void Postfix(Smelter __instance, Switch sw, Humanoid user, bool __result)
            {
                if (ShouldFillStation(__result))
                {
                    RunStationFill(SmelterAddOreMethod, __instance, new object[] { sw, user, null });
                }
            }
        }

        [HarmonyPatch(typeof(Smelter), "OnAddFuel")]
        class Smelter_OnAddFuel_FillPatch
        {
            static void Postfix(Smelter __instance, Switch sw, Humanoid user, bool __result)
            {
                if (ShouldFillStation(__result))
                {
                    RunStationFill(SmelterAddFuelMethod, __instance, new object[] { sw, user, null });
                }
            }
        }

        private static readonly MethodInfo CookingHaveDoneItemMethod = AccessTools.Method(typeof(CookingStation), "HaveDoneItem");

        private static bool CookingHasDoneItem(CookingStation station)
        {
            return CookingHaveDoneItemMethod != null
                && (bool)CookingHaveDoneItemMethod.Invoke(station, null);
        }

        [HarmonyPatch(typeof(CookingStation), "OnInteract")]
        class CookingStation_OnInteract_FillPatch
        {
            static void Postfix(CookingStation __instance, Humanoid user, bool __result)
            {
                if (!ShouldFillStation(__result))
                {
                    return;
                }
                // OnInteract unloads cooked food when any is ready and otherwise loads raw food.
                // Sampling that state now means the fill only ever does one of those two jobs:
                // collect everything that's ready, or fill every empty slot - never both.
                bool unloading = CookingHasDoneItem(__instance);
                RunStationFill(CookingInteractMethod, __instance, new object[] { user },
                    () => CookingHasDoneItem(__instance) == unloading);
            }
        }

        [HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch")]
        class CookingStation_OnAddFuelSwitch_FillPatch
        {
            static void Postfix(CookingStation __instance, Switch sw, Humanoid user, bool __result)
            {
                if (ShouldFillStation(__result))
                {
                    RunStationFill(CookingAddFuelMethod, __instance, new object[] { sw, user, null });
                }
            }
        }

        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
        class Fireplace_Interact_FillPatch
        {
            static void Postfix(Fireplace __instance, Humanoid user, bool __result)
            {
                if (ShouldFillStation(__result))
                {
                    RunStationFill(FireplaceInteractMethod, __instance, new object[] { user, false, false });
                }
            }
        }

        [HarmonyPatch(typeof(Turret), nameof(Turret.UseItem))]
        class Turret_UseItem_FillPatch
        {
            static void Postfix(Turret __instance, Humanoid user, bool __result)
            {
                if (ShouldFillStation(__result))
                {
                    RunStationFill(TurretUseItemMethod, __instance, new object[] { user, null });
                }
            }
        }

        [HarmonyPatch(typeof(ShieldGenerator), "OnAddFuel")]
        class ShieldGenerator_OnAddFuel_FillPatch
        {
            static void Postfix(ShieldGenerator __instance, Switch sw, Humanoid user, bool __result)
            {
                if (ShouldFillStation(__result))
                {
                    RunStationFill(ShieldGeneratorAddFuelMethod, __instance, new object[] { sw, user, null });
                }
            }
        }

        // ---------------- Fill shortcut hint in station tooltips ----------------
        // These hover methods return text that has already been through Localization.Localize, so
        // the appended line has to be localised here rather than left as tokens. $KEY_Use is
        // resolved explicitly so the hint follows a rebound interact key.
        private static string FillHintSuffix()
        {
            if (!ShowFillStationHint.Value || FillStationKey.Value == KeyCode.None)
            {
                return null;
            }
            string useKey = Localization.instance != null
                ? Localization.instance.Localize("$KEY_Use")
                : "Use";
            return $"\n[<color=yellow><b>{FillStationKey.Value} + {useKey}</b></color>] Fill";
        }

        private static void AppendFillHint(ref string hoverText)
        {
            if (string.IsNullOrEmpty(hoverText))
            {
                return;
            }
            string suffix = FillHintSuffix();
            if (suffix != null)
            {
                hoverText += suffix;
            }
        }

        // ---------------- Predicting what a smelter will actually take ----------------
        // The hover text says "Add item" without saying which, which is ambiguous once several
        // inputs are available across your bag and nearby chests. This runs the same selection the
        // fill loop would - carried stock first in priority order, then containers in priority
        // order, bounded by remaining capacity - and reports the result.
        private static readonly MethodInfo SmelterGetQueueSizeMethod = AccessTools.Method(typeof(Smelter), "GetQueueSize");
        private static readonly MethodInfo SmelterGetFuelMethod = AccessTools.Method(typeof(Smelter), "GetFuel");
        private static readonly FieldInfo CookingStationNviewField = AccessTools.Field(typeof(CookingStation), "m_nview");

        private static readonly List<ItemDrop> _orderedCandidates = new List<ItemDrop>();
        private static string _lastStationInputLog;
        private static readonly List<string> _planNames = new List<string>();
        private static readonly List<int> _planCounts = new List<int>();
        private static readonly List<bool> _planFromContainer = new List<bool>();

        // The hover runs every frame; recomputing a container sweep that often is wasteful
        private const float HoverCacheSeconds = 0.25f;
        private static object _hoverCacheOwner;
        private static bool _hoverCacheIsOre;
        private static float _hoverCacheTime;
        private static string _hoverCacheNext;
        private static string _hoverCacheFill;

        private static void OrderCandidates(List<ItemDrop> candidates, List<string> priority)
        {
            _orderedCandidates.Clear();
            for (int p = 0; p < priority.Count; p++)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    ItemDrop candidate = candidates[i];
                    if (candidate != null && candidate.gameObject.name == priority[p] && !_orderedCandidates.Contains(candidate))
                    {
                        _orderedCandidates.Add(candidate);
                    }
                }
            }
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null && !_orderedCandidates.Contains(candidates[i]))
                {
                    _orderedCandidates.Add(candidates[i]);
                }
            }
        }

        private static int CountInContainers(string sharedName)
        {
            int total = 0;
            for (int i = 0; i < _nearbyInventories.Count; i++)
            {
                total += _nearbyInventories[i].CountItems(sharedName);
            }
            return total;
        }

        private static int AccumulatePlan(int remaining, bool fromContainer, Func<string, int> available)
        {
            for (int i = 0; i < _orderedCandidates.Count && remaining > 0; i++)
            {
                string sharedName = _orderedCandidates[i].m_itemData.m_shared.m_name;
                int take = Mathf.Min(available(sharedName), remaining);
                if (take <= 0)
                {
                    continue;
                }
                _planNames.Add(Localization.instance != null ? Localization.instance.Localize(sharedName) : sharedName);
                _planCounts.Add(take);
                _planFromContainer.Add(fromContainer);
                remaining -= take;
            }
            return remaining;
        }

        /// <summary>
        /// Mirrors ChooseStationInput: everything carried is consumed before any container is
        /// touched, with the priority list ordering the candidates within each pass.
        /// </summary>
        private static void BuildFillPlan(Player player, List<ItemDrop> candidates, List<string> priority, int capacity)
        {
            _planNames.Clear();
            _planCounts.Clear();
            _planFromContainer.Clear();
            if (capacity <= 0 || candidates.Count == 0 || player == null)
            {
                return;
            }
            Inventory inventory = player.GetInventory();
            if (inventory == null)
            {
                return;
            }
            OrderCandidates(candidates, priority);

            int remaining = AccumulatePlan(capacity, false, name => inventory.CountItems(name));
            if (remaining > 0 && FeedStationsFromContainers.Value)
            {
                CollectNearbyInventories(player);
                AccumulatePlan(remaining, true, CountInContainers);
            }

            if (LogStationInputs.Value)
            {
                LogStationInputSnapshot(player, inventory);
            }
        }

        /// <summary>
        /// Diagnostic: reports what a station will actually accept, already sorted the way the
        /// priority list puts it, alongside how much of each is reachable. The prefab names printed
        /// here are the ones the priority list has to match - which is how a station turning out to
        /// use a renamed prefab (FlametalOreNew rather than FlametalOre) becomes visible instead of
        /// silently falling through to the station's own order.
        /// </summary>
        private static void LogStationInputSnapshot(Player player, Inventory inventory)
        {
            if (FeedStationsFromContainers.Value)
            {
                CollectNearbyInventories(player);
            }
            else
            {
                _nearbyInventories.Clear();
            }
            var sb = new StringBuilder("Station inputs (priority order): ");
            for (int i = 0; i < _orderedCandidates.Count; i++)
            {
                ItemDrop drop = _orderedCandidates[i];
                string sharedName = drop.m_itemData.m_shared.m_name;
                if (i > 0)
                {
                    sb.Append("  |  ");
                }
                sb.Append(drop.gameObject.name)
                  .Append(" inv=").Append(inventory.CountItems(sharedName))
                  .Append(" chests=").Append(CountInContainers(sharedName));
            }
            string line = sb.ToString();
            // The hover recomputes several times a second; only report when something changes
            if (line != _lastStationInputLog)
            {
                _lastStationInputLog = line;
                Debug.Log(line);
            }
        }

        private static string PlanSourceSuffix()
        {
            bool anyContainer = false;
            bool anyCarried = false;
            for (int i = 0; i < _planFromContainer.Count; i++)
            {
                if (_planFromContainer[i]) { anyContainer = true; } else { anyCarried = true; }
            }
            if (!anyContainer)
            {
                return "";
            }
            return anyCarried ? " (inventory + containers)" : " (from containers)";
        }

        /// <summary>
        /// Counts empty slots the way CookingStation.GetFreeSlot does - it only reports the first
        /// free index, so the loop is repeated here to get a total. m_nview is non-public, hence
        /// the cached field handle.
        /// </summary>
        private static int CountFreeCookingSlots(CookingStation station)
        {
            if (station.m_slots == null || CookingStationNviewField == null)
            {
                return 0;
            }
            ZNetView nview = CookingStationNviewField.GetValue(station) as ZNetView;
            if (nview == null || !nview.IsValid())
            {
                return 0;
            }
            ZDO zdo = nview.GetZDO();
            if (zdo == null)
            {
                return 0;
            }
            int free = 0;
            for (int i = 0; i < station.m_slots.Length; i++)
            {
                if (string.IsNullOrEmpty(zdo.GetString("slot" + i, "")))
                {
                    free++;
                }
            }
            return free;
        }

        private static void RefreshHoverPlan(object owner, bool isOre, List<ItemDrop> candidates, List<string> priority, int capacity)
        {
            bool cacheValid = ReferenceEquals(_hoverCacheOwner, owner)
                && _hoverCacheIsOre == isOre
                && Time.realtimeSinceStartup - _hoverCacheTime < HoverCacheSeconds;
            if (cacheValid)
            {
                return;
            }
            _hoverCacheOwner = owner;
            _hoverCacheIsOre = isOre;
            _hoverCacheTime = Time.realtimeSinceStartup;
            _hoverCacheNext = null;
            _hoverCacheFill = null;

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            BuildFillPlan(player, candidates, priority, capacity);
            if (_planNames.Count == 0)
            {
                return;
            }

            string suffix = PlanSourceSuffix();
            _hoverCacheNext = _planNames[0] + (_planFromContainer[0] ? " (from containers)" : "");

            var sb = new StringBuilder();
            for (int i = 0; i < _planNames.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(_planCounts[i]).Append(' ').Append(_planNames[i]);
            }
            sb.Append(suffix);
            _hoverCacheFill = sb.ToString();
        }

        private static void AppendPlanHover(ref string hoverText)
        {
            // Vanilla's "[E] Add item" is the last line, so naming the item reads as part of it
            if (_hoverCacheNext != null)
            {
                hoverText += ": " + _hoverCacheNext;
            }
            string hint = FillHintSuffix();
            if (hint == null)
            {
                return;
            }
            hoverText += hint;
            if (_hoverCacheFill != null)
            {
                hoverText += ": " + _hoverCacheFill;
            }
        }

        private static void CollectConversionInputs(List<Smelter.ItemConversion> conversions)
        {
            _feedCandidates.Clear();
            foreach (var conversion in conversions)
            {
                if (conversion.m_from != null)
                {
                    _feedCandidates.Add(conversion.m_from);
                }
            }
        }

        private static void CollectConversionInputs(List<CookingStation.ItemConversion> conversions)
        {
            _feedCandidates.Clear();
            foreach (var conversion in conversions)
            {
                if (conversion.m_from != null)
                {
                    _feedCandidates.Add(conversion.m_from);
                }
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddOre))]
        class Smelter_OnHoverAddOre_Patch
        {
            static void Postfix(Smelter __instance, ref string __result)
            {
                if (string.IsNullOrEmpty(__result) || __instance.m_conversion == null || SmelterGetQueueSizeMethod == null)
                {
                    return;
                }
                CollectConversionInputs(__instance.m_conversion);
                int capacity = __instance.m_maxOre - (int)SmelterGetQueueSizeMethod.Invoke(__instance, null);
                RefreshHoverPlan(__instance, isOre: true, _feedCandidates, _smelterPriority, capacity);
                AppendPlanHover(ref __result);
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnHoverAddFuel))]
        class Smelter_OnHoverAddFuel_Patch
        {
            static void Postfix(Smelter __instance, ref string __result)
            {
                if (string.IsNullOrEmpty(__result) || __instance.m_fuelItem == null || SmelterGetFuelMethod == null)
                {
                    return;
                }
                _feedCandidates.Clear();
                _feedCandidates.Add(__instance.m_fuelItem);
                int capacity = __instance.m_maxFuel - Mathf.CeilToInt((float)SmelterGetFuelMethod.Invoke(__instance, null));
                RefreshHoverPlan(__instance, isOre: false, _feedCandidates, _noPriority, capacity);
                AppendPlanHover(ref __result);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetHoverText))]
        class CookingStation_GetHoverText_Patch
        {
            static void Postfix(CookingStation __instance, ref string __result)
            {
                if (string.IsNullOrEmpty(__result) || __instance.m_conversion == null)
                {
                    return;
                }
                CollectConversionInputs(__instance.m_conversion);
                RefreshHoverPlan(__instance, isOre: true, _feedCandidates, _cookingPriority, CountFreeCookingSlots(__instance));
                AppendPlanHover(ref __result);
            }
        }

        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
        class Fireplace_GetHoverText_Patch
        {
            static void Postfix(ref string __result) { AppendFillHint(ref __result); }
        }

        [HarmonyPatch(typeof(Turret), nameof(Turret.GetHoverText))]
        class Turret_GetHoverText_Patch
        {
            static void Postfix(ref string __result) { AppendFillHint(ref __result); }
        }

        // ---------------- Feed processing stations from containers ----------------
        // Each station finds what to consume by searching the player's inventory and then removes
        // it from that same inventory, so the item genuinely has to be in the player's bag. These
        // patches therefore move one item in from a nearby container and let the station's own code
        // find and consume it, rather than trying to make the station read a container directly.
        //
        // The "find" methods take an Inventory and are the natural seam; the fuel methods have no
        // such hook, so those are prefixes that top the player up before the check runs.

        [HarmonyPatch(typeof(Smelter), "FindCookableItem")]
        class Smelter_FindCookableItem_Patch
        {
            static void Postfix(Smelter __instance, Inventory inventory, ref ItemDrop.ItemData __result)
            {
                if (__instance.m_conversion == null)
                {
                    return;
                }
                _feedCandidates.Clear();
                foreach (var entry in __instance.m_conversion)
                {
                    if (entry.m_from != null)
                    {
                        _feedCandidates.Add(entry.m_from);
                    }
                }
                __result = ChooseStationInput(inventory, _feedCandidates, _smelterPriority, SmelterHasOreRoom(__instance), __result);
            }
        }

        [HarmonyPatch(typeof(Smelter), "OnAddFuel")]
        class Smelter_OnAddFuel_Patch
        {
            static void Prefix(Smelter __instance, Humanoid user)
            {
                if (__instance.m_fuelItem == null)
                {
                    return;
                }
                if (SmelterHasFuelRoom(__instance) && BeginStationFeed(user, out Inventory playerInventory))
                {
                    EnsureOneInInventory(playerInventory, __instance.m_fuelItem.m_itemData.m_shared.m_name);
                }
            }
        }

        [HarmonyPatch(typeof(CookingStation), "FindCookableItem")]
        class CookingStation_FindCookableItem_Patch
        {
            static void Postfix(CookingStation __instance, Inventory inventory, ref ItemDrop.ItemData __result)
            {
                if (__instance.m_conversion == null)
                {
                    return;
                }
                _feedCandidates.Clear();
                foreach (var entry in __instance.m_conversion)
                {
                    if (entry.m_from != null)
                    {
                        _feedCandidates.Add(entry.m_from);
                    }
                }
                __result = ChooseStationInput(inventory, _feedCandidates, _cookingPriority, CountFreeCookingSlots(__instance) > 0, __result);
            }
        }

        [HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch")]
        class CookingStation_OnAddFuelSwitch_Patch
        {
            static void Prefix(CookingStation __instance, Humanoid user)
            {
                if (__instance.m_fuelItem == null)
                {
                    return;
                }
                if (CookingHasFuelRoom(__instance) && BeginStationFeed(user, out Inventory playerInventory))
                {
                    EnsureOneInInventory(playerInventory, __instance.m_fuelItem.m_itemData.m_shared.m_name);
                }
            }
        }

        [HarmonyPatch(typeof(Fermenter), "FindCookableItem")]
        class Fermenter_FindCookableItem_Patch
        {
            static void Postfix(Fermenter __instance, Inventory inventory, ref ItemDrop.ItemData __result)
            {
                if (__instance.m_conversion == null)
                {
                    return;
                }
                _feedCandidates.Clear();
                foreach (var entry in __instance.m_conversion)
                {
                    if (entry.m_from != null)
                    {
                        _feedCandidates.Add(entry.m_from);
                    }
                }
                __result = ChooseStationInput(inventory, _feedCandidates, _noPriority, FermenterIsEmpty(__instance), __result);
            }
        }

        [HarmonyPatch(typeof(Turret), "FindAmmoItem")]
        class Turret_FindAmmoItem_Patch
        {
            static void Postfix(Turret __instance, Inventory inventory, ref ItemDrop.ItemData __result)
            {
                if (__instance.m_allowedAmmo == null)
                {
                    return;
                }
                _feedCandidates.Clear();
                foreach (var entry in __instance.m_allowedAmmo)
                {
                    if (entry.m_ammo != null)
                    {
                        _feedCandidates.Add(entry.m_ammo);
                    }
                }
                __result = ChooseStationInput(inventory, _feedCandidates, _noPriority, TurretHasAmmoRoom(__instance), __result);
            }
        }

        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
        class Fireplace_Interact_Patch
        {
            static void Prefix(Fireplace __instance, Humanoid user)
            {
                if (__instance.m_fuelItem == null)
                {
                    return;
                }
                if (FireplaceHasFuelRoom(__instance) && BeginStationFeed(user, out Inventory playerInventory))
                {
                    EnsureOneInInventory(playerInventory, __instance.m_fuelItem.m_itemData.m_shared.m_name);
                }
            }
        }

        [HarmonyPatch(typeof(ShieldGenerator), "OnAddFuel")]
        class ShieldGenerator_OnAddFuel_Patch
        {
            static void Prefix(ShieldGenerator __instance, Humanoid user)
            {
                if (__instance.m_fuelItems == null)
                {
                    return;
                }
                if (!ShieldGeneratorHasFuelRoom(__instance) || !BeginStationFeed(user, out Inventory playerInventory))
                {
                    return;
                }
                foreach (var fuelItem in __instance.m_fuelItems)
                {
                    if (fuelItem != null && EnsureOneInInventory(playerInventory, fuelItem.m_itemData.m_shared.m_name))
                    {
                        return;
                    }
                }
            }
        }

        // Build range is derived, not stored: GetExtensions recomputes
        //   m_buildRange = m_rangeBuild + extensionCount * m_extraRangePerLevel
        // every couple of seconds and pushes the result into the coverage circle and the effect
        // collider as well. Scaling the two public source fields once per station therefore widens
        // the range, the visible circle and the collider together, where patching the derived
        // m_buildRange or GetStationBuildRange would have moved the gameplay range while leaving
        // the circle showing the old size.
        //
        // Both fields are scaled so an upgraded station keeps its proportions. Start runs once per
        // placed station, so this cannot compound.
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
        class CraftingStation_Start_Patch
        {
            static void Postfix(CraftingStation __instance)
            {
                float multiplier = WorkstationRangeMultiplier.Value;
                if (multiplier > 0f && multiplier != 1f)
                {
                    __instance.m_rangeBuild *= multiplier;
                    __instance.m_extraRangePerLevel *= multiplier;
                }
            }
        }

        // ---------------- Mass planting ----------------
        // Vanilla plants exactly one seed per click: UpdatePlacement checks requirements, calls
        // TryPlacePiece (which validates the ghost and calls PlacePiece at the ghost's transform),
        // then consumes the resources. PlacePiece happily takes an arbitrary position, so the grid
        // is built by calling it directly for the surrounding cells.
        //
        // The ghost's validation can't be reused for those cells - UpdatePlacementGhost recomputes
        // the position from the camera ray, so moving the ghost and re-validating is not possible.
        // The two checks that matter for a seed are replicated instead: cultivated ground, and the
        // clear radius Plant.HaveGrowSpace demands. Cells that fail are skipped rather than
        // aborting the batch, so one bad square doesn't cost you the planting.
        private static readonly FieldInfo PlayerPlacementGhostField = AccessTools.Field(typeof(Player), "m_placementGhost");
        private static readonly FieldInfo PlayerNoPlacementCostField = AccessTools.Field(typeof(Player), "m_noPlacementCost");
        private static readonly Collider[] _plantColliders = new Collider[128];
        private static int _plantSpaceMask;
        private static readonly List<GameObject> _plantPreviews = new List<GameObject>();
        private static string _previewPieceName;

        /// <summary>
        /// Computes a grid cell's world position and whether a seed would take there. Shared by the
        /// preview and the planting itself so what you see is what you get - if this ever drifted,
        /// the preview would start lying about the result.
        /// </summary>
        private static bool TryGetPlantCell(Vector3 centre, Quaternion rotation, float spacing, int gx, int gz,
                                            Piece piece, Plant plant, out Vector3 position) {
            position = centre + rotation * new Vector3(gx * spacing, 0f, gz * spacing);
            if (!Heightmap.GetHeight(position, out float groundHeight)) {
                return false;
            }
            position.y = groundHeight;
            if (piece.m_cultivatedGroundOnly) {
                Heightmap heightmap = Heightmap.FindHeightmap(position);
                if (heightmap == null || !heightmap.IsCultivated(position)) {
                    return false;
                }
            }
            return HasGrowSpaceAt(position, plant.m_growRadius);
        }

        private static void ClearPlantPreviews() {
            for (int i = 0; i < _plantPreviews.Count; i++) {
                if (_plantPreviews[i] != null) {
                    UnityEngine.Object.Destroy(_plantPreviews[i]);
                }
            }
            _plantPreviews.Clear();
            _previewPieceName = null;
        }

        /// <summary>
        /// Clones the live placement ghost rather than rebuilding one from the prefab. The ghost has
        /// already had its rigidbodies, joints, lights and colliders stripped or disabled by
        /// SetupPlacementGhost, so a clone inherits all of that - including the disabled colliders,
        /// which is what stops the previews from registering as obstructions in each other's space
        /// checks. The two force-disable flags mirror the guards the game itself uses.
        /// </summary>
        private static GameObject ClonePlacementGhost(GameObject ghost) {
            bool previousInit = ZNetView.m_forceDisableInit;
            bool previousTerrain = TerrainOp.m_forceDisableTerrainOps;
            ZNetView.m_forceDisableInit = true;
            TerrainOp.m_forceDisableTerrainOps = true;
            try {
                GameObject clone = UnityEngine.Object.Instantiate(ghost);
                clone.name = ghost.name + "_PipsMassPlantPreview";

                // Plant derives from SlowUpdate, whose Awake registers every instance into a
                // static list that SlowUpdater walks - and SUpdate is the only thing that ever
                // grows a plant. Leaving the Plant component on a preview would put two dozen
                // throwaway objects into that registry and churn its index-based swap-removal
                // every time the previews are rebuilt. A preview only has to look like a plant,
                // so the component goes immediately, deregistering it in the same breath.
                Plant previewPlant = clone.GetComponent<Plant>();
                if (previewPlant != null) {
                    UnityEngine.Object.DestroyImmediate(previewPlant);
                }
                return clone;
            }
            finally {
                ZNetView.m_forceDisableInit = previousInit;
                TerrainOp.m_forceDisableTerrainOps = previousTerrain;
            }
        }

        private static void UpdateMassPlantPreview() {
            Player player = Player.m_localPlayer;
            if (player == null || MassPlantGridSize.Value <= 1 || !Input.GetKey(MassPlantKey.Value)) {
                ClearPlantPreviews();
                return;
            }
            GameObject ghost = PlayerPlacementGhostField?.GetValue(player) as GameObject;
            if (ghost == null || !ghost.activeSelf) {
                ClearPlantPreviews();
                return;
            }
            Piece piece = ghost.GetComponent<Piece>();
            Plant plant = ghost.GetComponent<Plant>();
            if (piece == null || plant == null) {
                ClearPlantPreviews();
                return;
            }

            int needed = MassPlantGridSize.Value * MassPlantGridSize.Value - 1;
            if (_previewPieceName != ghost.name || _plantPreviews.Count != needed) {
                ClearPlantPreviews();
                _previewPieceName = ghost.name;
                for (int i = 0; i < needed; i++) {
                    _plantPreviews.Add(ClonePlacementGhost(ghost));
                }
            }

            Vector3 centre = ghost.transform.position;
            Quaternion rotation = ghost.transform.rotation;
            float spacing = Mathf.Max(0.1f, plant.m_growRadius * MassPlantSpacing.Value);
            int half = MassPlantGridSize.Value / 2;
            int index = 0;
            for (int gx = -half; gx <= half; gx++) {
                for (int gz = -half; gz <= half; gz++) {
                    if (gx == 0 && gz == 0) {
                        continue;   // the real ghost already stands here
                    }
                    if (index >= _plantPreviews.Count) {
                        return;
                    }
                    GameObject preview = _plantPreviews[index++];
                    if (preview == null) {
                        continue;
                    }
                    bool valid = TryGetPlantCell(centre, rotation, spacing, gx, gz, piece, plant, out Vector3 position);
                    preview.transform.SetPositionAndRotation(position, rotation);
                    preview.GetComponent<Piece>()?.SetInvalidPlacementHeightlight(!valid);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        class Player_TryPlacePiece_MassPlant_Patch
        {
            static void Postfix(Player __instance, Piece piece, bool __result) {
                if (!__result || piece == null || MassPlantGridSize.Value <= 1) {
                    return;
                }
                if (!ReferenceEquals(__instance, Player.m_localPlayer) || !Input.GetKey(MassPlantKey.Value)) {
                    return;
                }
                Plant plant = piece.GetComponent<Plant>();
                if (plant != null) {
                    MassPlant(__instance, piece, plant);
                }
            }
        }

        private static bool PlayerHasFreeBuild(Player player) {
            return PlayerNoPlacementCostField != null
                && (bool)PlayerNoPlacementCostField.GetValue(player);
        }

        /// <summary>
        /// Replicates Plant.HaveGrowSpace for a point that has no Plant on it yet. Any non-plant
        /// collider inside the radius blocks, as does any healthy neighbouring plant - matching the
        /// game's own rule, so anything this accepts would also satisfy the plant once placed.
        /// </summary>
        private static bool HasGrowSpaceAt(Vector3 position, float growRadius) {
            if (_plantSpaceMask == 0) {
                _plantSpaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
            }
            int hits = Physics.OverlapSphereNonAlloc(position, growRadius, _plantColliders, _plantSpaceMask);
            for (int i = 0; i < hits; i++) {
                Plant other = _plantColliders[i].GetComponent<Plant>();
                if (other == null || other.GetStatus() == Plant.Status.Healthy) {
                    return false;
                }
            }
            return true;
        }

        private static void MassPlant(Player player, Piece piece, Plant plant) {
            GameObject ghost = PlayerPlacementGhostField?.GetValue(player) as GameObject;
            if (ghost == null) {
                return;
            }
            // These statics are what the preview sets while cloning ghosts. If either were still
            // set here the plants would be instantiated without a ZDO, which is silently ruinous:
            // Plant.m_status defaults to Healthy and GetHoverText reads it without checking
            // validity, while SUpdate - the only thing that grows a plant - bails out on an invalid
            // ZNetView. The result looks like a healthy crop that never grows and never saves.
            ZNetView.m_forceDisableInit = false;
            TerrainOp.m_forceDisableTerrainOps = false;

            Vector3 centre = ghost.transform.position;
            Quaternion rotation = ghost.transform.rotation;
            float spacing = Mathf.Max(0.1f, plant.m_growRadius * MassPlantSpacing.Value);
            bool freeBuild = PlayerHasFreeBuild(player)
                || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()));

            int half = MassPlantGridSize.Value / 2;
            int planted = 0;
            for (int gx = -half; gx <= half; gx++) {
                for (int gz = -half; gz <= half; gz++) {
                    if (gx == 0 && gz == 0) {
                        continue;   // vanilla already placed the centre
                    }
                    // Same cell logic the preview uses, so the red squares are exactly the ones skipped
                    if (!TryGetPlantCell(centre, rotation, spacing, gx, gz, piece, plant, out Vector3 position)) {
                        continue;
                    }
                    // Checked per seed so the batch stops cleanly when the last one is used
                    if (!freeBuild && !player.HaveRequirements(piece, Player.RequirementMode.CanBuild)) {
                        break;
                    }

                    player.PlacePiece(piece, position, rotation, doAttack: false);
                    if (!freeBuild) {
                        player.ConsumeResources(piece.m_resources, 0);
                    }
                    planted++;
                }
            }

            if (planted > 0 && _messageHud != null) {
                _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, $"Planted {planted + 1}");
            }

            // Grow time is serialised per prefab and picked per plant by lerping between these two
            // with a seed, so the real window is only visible at runtime. Reported in days as well
            // as seconds because a Valheim day is m_dayLengthSec, not 24 hours of anything.
            if (planted > 0) {
                float dayLength = EnvMan.instance != null ? EnvMan.instance.m_dayLengthSec : 1200f;
                Debug.Log($"{piece.gameObject.name} grow time: {plant.m_growTime:0}-{plant.m_growTimeMax:0}s "
                        + $"({plant.m_growTime / dayLength:0.0}-{plant.m_growTimeMax / dayLength:0.0} in-game days)");
            }
        }

        // ---------------- Solo-only gate for world-changing settings ----------------
        // Some multipliers alter state stored in the world rather than only what this client sees:
        // drops are rolled by whoever owns the object, station output and taming progress live in
        // ZDOs. Whoever owns the object computes the result, so on a shared world those effects
        // reach everyone. These are suspended unless the session is genuinely solo.
        //
        // Solo means hosting with nobody connected. ZNet.IsServer() is false when you have joined
        // someone else's world, and GetPeerConnections counts anyone connected to yours - so a
        // player joining or leaving mid-session flips this without needing a reload.
        private const float SharedSessionCacheSeconds = 1f;
        private static float _sharedSessionCheckedAt = float.NegativeInfinity;
        private static bool _sharedSessionCached;
        private static float _vanillaResourceRate = 1f;
        private static float _appliedStackMultiplier = float.NaN;

        /// <summary>
        /// Stack size isn't read at point of use - it's written into each item's SharedData - so
        /// like the resource rate it has to be re-asserted when the session changes. Rewriting
        /// ~1000 items every frame would be wasteful, so the full pass runs only when the effective
        /// multiplier actually differs from what is currently applied. ApplyStackSizeMultiplier
        /// always recomputes from the captured vanilla baseline, so withdrawing the boost restores
        /// the real sizes rather than dividing back down.
        /// </summary>
        private static void RefreshStackSizeMultiplier() {
            if (SoloOnlyRate(CustomStackSizeMultiplier) == _appliedStackMultiplier) {
                return;
            }
            ObjectDB odb = ObjectDB.instance;
            if (odb != null) {
                ApplyStackSizeMultiplier(odb);
            }
        }

        private static bool IsSharedSession() {
            if (!WorldEffectsSoloOnly.Value) {
                return false;
            }
            float now = Time.realtimeSinceStartup;
            if (now - _sharedSessionCheckedAt < SharedSessionCacheSeconds) {
                return _sharedSessionCached;
            }
            _sharedSessionCheckedAt = now;
            ZNet znet = ZNet.instance;
            // No ZNet at the menu; treat that as solo so nothing is suspended spuriously
            _sharedSessionCached = znet != null && (!znet.IsServer() || znet.GetPeerConnections() > 0);
            return _sharedSessionCached;
        }

        /// <summary>
        /// Returns a world-changing multiplier, or 1 when the session is shared.
        /// </summary>
        private static float SoloOnlyRate(ConfigEntry<float> setting) {
            float value = setting.Value;
            return (value > 0f && value != 1f && !IsSharedSession()) ? value : 1f;
        }

        /// <summary>
        /// The resource rate is a stored global rather than a value read at point of use, so it has
        /// to be re-asserted as the session changes - otherwise a boosted rate would linger after
        /// someone joined. _vanillaResourceRate holds whatever the world itself last set.
        /// </summary>
        private static void ApplyResourceRate() {
            float multiplier = SoloOnlyRate(CustomResourceRate);
            Game.m_resourceRate = multiplier != 1f ? multiplier : _vanillaResourceRate;
        }

        // ---------------- Passive forsaken powers ----------------
        // Powers are character state, not world state. Selecting one at a stone calls
        // Player.SetGuardianPower, which stores it in m_guardianPower *and* records the power's
        // name through AddUniqueKey; Player.Save writes both. That is why an established character
        // carries its last power into a brand new world, and it means HaveUniqueKey("GP_Eikthyr")
        // answers "has this character ever unlocked Eikthyr's power" from anywhere, in any world,
        // with no lookup cost.
        //
        // Whether a trophy is *currently* mounted is world state instead - it lives in the stand's
        // own ZDO under ZDOVars.s_item - so it is deliberately not what this reads. Using the
        // character record keeps powers with the character the way the game itself does. The
        // trade-offs: a trophy mounted without ever clicking the stone does not count (one click
        // per stone, ever, is enough), and taking a trophy back down does not revoke the power.
        //
        // BossStone.m_setsWorldKey looked like a better signal but is empty on every shipped stone,
        // and the GlobalKeys enum has no boss-stone entry - the field is unused here.
        //
        // The stone-to-power mapping is still read out of the prefabs rather than hardcoded, so a
        // boss added by a later patch is picked up with no code change. Only plain values are kept,
        // not the StatusEffect reference: ZNetScene's prefabs are torn down when the world unloads,
        // and the hash is all AddStatusEffect needs to find the live asset in the current ObjectDB.
        private const float PowerCheckSeconds = 2f;
        private static float _powerCheckedAt = float.NegativeInfinity;
        private static List<BossPower> _bossPowers;

        private class BossPower {
            public string StoneName;
            public string PowerName;
            public int PowerHash;
            public StatusEffect.StatusAttribute Attributes;
        }

        /// <summary>
        /// Collects every guardian power the game's boss stones can grant, from the prefabs
        /// ZNetScene already has loaded. Cached after the first successful pass, since the prefab
        /// set is fixed for the lifetime of the process.
        /// </summary>
        private static void ScanBossPowers() {
            if (_bossPowers != null) {
                return;
            }
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || scene.m_prefabs == null) {
                return;
            }
            var found = new List<BossPower>();
            foreach (GameObject prefab in scene.m_prefabs) {
                if (prefab == null) {
                    continue;
                }
                // Shipped prefabs carry BossStone on the root, but search children as well so a
                // nested or modded layout still resolves
                BossStone stone = prefab.GetComponentInChildren<BossStone>(true);
                if (stone == null || stone.m_itemStand == null) {
                    continue;
                }
                StatusEffect power = stone.m_itemStand.m_guardianPower;
                if (power == null) {
                    continue;
                }
                found.Add(new BossPower {
                    StoneName = prefab.name,
                    PowerName = power.name,
                    PowerHash = power.NameHash(),
                    Attributes = power.m_attributes,
                });
            }
            _bossPowers = found;
            Debug.Log($"Discovered {found.Count} boss stone power(s): " +
                string.Join(", ", found.Select(b => $"{b.StoneName} -> {b.PowerName}").ToArray()));
        }

        /// <summary>
        /// Holds a guardian power on the player for every boss power this character has unlocked.
        /// The effect is added once and then given an unlimited lifetime rather than being
        /// re-applied each tick; the periodic sweep exists to catch the cases that clear it,
        /// chiefly death, which calls SEMan.RemoveAllStatusEffects.
        /// </summary>
        private static void UpdatePassiveForsakenPowers() {
            Player player = Player.m_localPlayer;
            if (player == null) {
                return;
            }
            float now = Time.realtimeSinceStartup;
            if (now - _powerCheckedAt < PowerCheckSeconds) {
                return;
            }
            _powerCheckedAt = now;
            ScanBossPowers();
            SEMan seman = player.GetSEMan();
            if (_bossPowers == null || _bossPowers.Count == 0 || seman == null) {
                return;
            }
            bool enabled = PassiveForsakenPowers.Value;
            bool shared = IsSharedSession();
            foreach (BossPower boss in _bossPowers) {
                // The only part of a status effect that reaches other players is the four-flag
                // StatusAttribute mask written to the player's ZDO. SailingPower is one of those
                // flags and it applies to whatever ship the player is aboard, so powers carrying an
                // attribute stay off in a shared session while the purely local ones still work.
                bool wanted = enabled
                    && player.HaveUniqueKey(boss.PowerName)
                    && !(shared && boss.Attributes != StatusEffect.StatusAttribute.None);
                StatusEffect active = seman.GetStatusEffect(boss.PowerHash);
                if (wanted) {
                    if (active == null) {
                        active = seman.AddStatusEffect(boss.PowerHash, false, 0, 0f, -1);
                        if (active != null) {
                            Debug.Log($"Forsaken power {boss.PowerName} held passively ({boss.StoneName})");
                        }
                    }
                    if (active != null) {
                        // StatusEffect.IsDone only expires an effect when m_ttl > 0, so zero means
                        // it never lapses. This is SEMan's own clone, so the shared asset keeps its
                        // normal duration for the vanilla activate-by-keypress path.
                        active.m_ttl = 0f;
                        // SEMan.GetHUDStatusEffects skips hidden effects when it collects the row,
                        // so this drops the icon at source rather than filtering the HUD - which
                        // leaves other status effect mods to do their own thing undisturbed. Read
                        // from config every sweep so toggling it takes effect without a reload.
                        active.m_hidden = HidePassivePowerIcons.Value;
                    }
                } else if (active != null && active.m_ttl == 0f) {
                    // Withdraw only what we granted - an effect with a real ttl was activated
                    // normally and should run its own course
                    seman.RemoveStatusEffect(boss.PowerHash, true);
                }
            }
        }

        // Taming progress is a countdown in the creature's ZDO, and TamingUpdate credits it three
        // seconds at a time via DecreaseRemainingTime. Scaling that argument is how the game itself
        // speeds taming up - the vanilla TamingBoost status attribute multiplies the very same
        // value - so this reuses the existing mechanism rather than touching the stored timer.
        [HarmonyPatch(typeof(Tameable), "DecreaseRemainingTime")]
        class Tameable_DecreaseRemainingTime_Patch
        {
            static void Prefix(ref float time)
            {
                float multiplier = SoloOnlyRate(TamingSpeedMultiplier);
                if (multiplier > 0f && multiplier != 1f)
                {
                    time *= multiplier;
                }
            }
        }

        // ---------------- Permanent slow fall ----------------
        // The Feather Cape's effect is an SE_Stats whose ModifyWalkVelocity clamps downward speed
        // to m_maxMaxFallSpeed, with m_fallDamageModifier handling the landing separately. Rather
        // than forcing that status effect onto the player - which would occupy a buff slot and
        // fight equipment changes - the same two modifiers are applied at the aggregator the game
        // already funnels every status effect through.
        //
        // The numbers are read off the cape itself instead of being hardcoded, so this stays
        // faithful to whatever the game ships and survives a balance change.
        private static readonly FieldInfo SemanCharacterField = AccessTools.Field(typeof(SEMan), "m_character");
        private static bool _featherCapeResolved;
        private static float _featherCapeMaxFallSpeed;
        private static float _featherCapeFallDamageModifier;

        private static void ResolveFeatherCape()
        {
            if (_featherCapeResolved)
            {
                return;
            }
            ObjectDB odb = ObjectDB.instance;
            if (odb == null)
            {
                return;   // not loaded yet; try again on the next call
            }
            _featherCapeResolved = true;
            GameObject prefab = odb.GetItemPrefab("CapeFeather");
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
            {
                return;
            }
            if (drop.m_itemData.m_shared.m_equipStatusEffect is SE_Stats stats)
            {
                _featherCapeMaxFallSpeed = stats.m_maxMaxFallSpeed;
                _featherCapeFallDamageModifier = stats.m_fallDamageModifier;
                Debug.Log($"Feather Cape slow fall: maxFallSpeed={_featherCapeMaxFallSpeed}, fallDamageModifier={_featherCapeFallDamageModifier}");
            }
        }

        private static bool IsLocalPlayerSeman(SEMan seman)
        {
            return SemanCharacterField != null
                && ReferenceEquals(SemanCharacterField.GetValue(seman), Player.m_localPlayer);
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyWalkVelocity))]
        class SEMan_ModifyWalkVelocity_Patch
        {
            static void Postfix(SEMan __instance, ref Vector3 vel)
            {
                if (!AlwaysSlowFall.Value || !IsLocalPlayerSeman(__instance))
                {
                    return;
                }
                float limit = SlowFallMaxSpeed.Value;
                if (limit <= 0f)
                {
                    ResolveFeatherCape();
                    limit = _featherCapeMaxFallSpeed;
                }
                // Clamping is idempotent, so this is harmless while actually wearing the cape
                if (limit > 0f && vel.y < 0f - limit)
                {
                    vel.y = 0f - limit;
                }
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyFallDamage))]
        class SEMan_ModifyFallDamage_Patch
        {
            static void Postfix(SEMan __instance, float baseDamage, ref float damage)
            {
                if (!AlwaysSlowFall.Value || !SlowFallNegatesFallDamage.Value || !IsLocalPlayerSeman(__instance))
                {
                    return;
                }
                ResolveFeatherCape();
                damage += baseDamage * _featherCapeFallDamageModifier;
                if (damage < 0f)
                {
                    damage = 0f;
                }
            }
        }

        // Every weight check - encumbrance, the auto-pickup weight test, tombstone retrieval and the
        // HUD readout - goes through Inventory.GetTotalWeight, so zeroing its result covers them all
        // from one place. Patching the public getter rather than UpdateTotalWeight is deliberate:
        // m_totalWeight and UpdateTotalWeight are both non-public, and ExtraSlots already patches
        // UpdateTotalWeight to apply its own weight factor, so this avoids arguing over ordering.
        //
        // Inventory is a plain class rather than a UnityEngine.Object, so reference equality is the
        // right test for "is this the player's own inventory" and containers stay untouched.
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalWeight))]
        class Inventory_GetTotalWeight_Patch
        {
            static void Postfix(Inventory __instance, ref float __result)
            {
                if (!WeightlessPlayerInventory.Value || __result == 0f)
                {
                    return;
                }
                Player player = Player.m_localPlayer;
                if (player != null && ReferenceEquals(__instance, player.GetInventory()))
                {
                    __result = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ShowPickupMessage))]
        class Character_ShowPickupMessage_Patch
        {
            static bool Prefix()
            {
                return !SuppressPickupMessages.Value;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ShowRemovedMessage))]
        class Character_ShowRemovedMessage_Patch
        {
            static bool Prefix()
            {
                return !SuppressRemovedMessages.Value;
            }
        }

        // Messages emitted from the middle of a larger method can't be dropped by skipping that
        // method, so they're filtered here at the point they're raised instead.
        //
        // Targets Player.Message rather than Character.Message because Player overrides it, and
        // both Skills and the station scripts call it through a Player/Humanoid reference, so the
        // override is what actually runs.
        //
        // "$msg_added" is deliberately matched only for Center messages. Picking an item up raises
        // the same token as TopLeft, and that is handled by ShowPickupMessage under its own
        // setting - matching on the token alone would tie the two together.
        [HarmonyPatch(typeof(Player), nameof(Player.Message))]
        class Player_Message_Patch
        {
            static bool Prefix(MessageHud.MessageType type, string msg)
            {
                if (msg == null)
                {
                    return true;
                }
                if (SuppressSkillMessages.Value && msg.StartsWith("$msg_skillup", StringComparison.Ordinal))
                {
                    return false;
                }
                if (SuppressStationAddedMessages.Value
                    && type == MessageHud.MessageType.Center
                    && msg.StartsWith("$msg_added", StringComparison.Ordinal))
                {
                    return false;
                }
                // A fill repeats the add dozens of times; each pass would otherwise shout about it
                if (_suppressFillMessages && type == MessageHud.MessageType.Center)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "UseEitr")]
        class Player_UseEitr_Patch
        {
            static void Prefix(Player __instance, ref float v)
            {
                // Only apply to local player
                if (__instance == Player.m_localPlayer)
                {
                    v *= CustomEitrRate.Value;
                }
            }
        }

        /// <summary>
        /// Puts health back to the floor if anything managed to push it under.
        ///
        /// The prefix that scales an incoming hit is a prediction: it decides what to allow based
        /// on the health it can see at that moment. This runs afterwards on the result, so it holds
        /// no matter how many hits land together, in what order, or whether some path reduced
        /// health without consulting the gate at all.
        /// </summary>
        private static void EnforceHealthFloor(Character character)
        {
            if (MinHealthPercent.Value <= 0f || character == null || !ReferenceEquals(character, Player.m_localPlayer))
            {
                return;
            }
            if (character.IsDead())
            {
                return;
            }
            float floor = character.GetMaxHealth() * MinHealthPercent.Value;
            if (character.GetHealth() < floor)
            {
                character.SetHealth(floor);
            }
        }

        [HarmonyPatch(typeof(Character), "UseHealth")]
        class Character_UseHealth_Patch
        {
            static void Prefix(ref Character __instance, ref float hp)
            {
                // Blood magic spends health instead of eitr, so the eitr rate doubles as the
                // magic-cost multiplier. Local player only, matching UseEitr and UseStamina.
                if (__instance == Player.m_localPlayer)
                {
                    hp *= CustomEitrRate.Value;
                }
            }

            // UseHealth subtracts straight from health and clamps at zero, never consulting the
            // damage gate, so the floor has to be reapplied here too
            static void Postfix(Character __instance)
            {
                EnforceHealthFloor(__instance);
            }
        }

        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        class Player_UpdateMovementModifier_Patch
        {
            static void Prefix(ref ItemDrop.ItemData item)
            {
                if (NegateEquipmentMovementPenalty.Value && item.m_shared.m_movementModifier < 0)
                {
                    item.m_shared.m_movementModifier = 0;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplyPushback), new Type[] { typeof(Vector3), typeof(float) })]
        class Character_ApplyPushback_Patch
        {
            static void Prefix(ref float pushForce)
            {
                if (NegateKnockback.Value)
                {
                    pushForce = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(Skills.Skill), "Raise")]
        class Skill_Raise_Patch
        {
            [HarmonyPostfix]
            static void Postfix(ref Skills.Skill __instance)
            {
                // This readout calls ShowMessage directly, so the Player.Message filter never sees it
                if (SuppressSkillMessages.Value)
                {
                    return;
                }
                if (__instance.m_level < 100f)
                {
                    _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, $"{__instance.m_info.m_skill} ({__instance.m_level:N0}):  {__instance.GetLevelPercentage():P3}");
                }
            }
        }

        [HarmonyPatch(typeof(Odin), "Awake")]
        class Odin_Awake_Patch
        {
            static void Postfix(ref float ___m_despawnCloseDistance)
            {
                ___m_despawnCloseDistance = 1f;
            }
        }

        [HarmonyPatch(typeof(ResourceRoot), "Drain")]
        class ResourceRoot_Drain_Patch
        {
            static void Postfix(ref float ___m_regenPerSec)
            {
                ___m_regenPerSec = 20f;
            }
        }

        [HarmonyPatch(typeof(Player), "AddAdrenaline")]
        class Player_AddAdrenaline_Patch
        {
            // Gain and degeneration are both scaled here. Applying degeneration from a postfix that
            // called AddAdrenaline again re-entered this patch, and the correction diverged once the
            // degen rate reached 2.
            static void Prefix(ref float v)
            {
                if (v > 0f)
                {
                    v *= CustomAdrenalineGainRate.Value;
                }
                else if (v < 0f)
                {
                    v *= CustomAdrenalineDegenRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Player), "UseStamina")]
        class Player_UseStamina_PlayerSpecific_Patch
        {
            static void Prefix(Player __instance, ref float v)
            {
                // Only apply to local player
                if (__instance == Player.m_localPlayer)
                {
                    v *= CustomStaminaRate.Value;
                }
            }
        }

        // Patched on Skills rather than Player deliberately. Player.RaiseSkill only forwards to
        // m_skills.RaiseSkill, and several things skip the Player method and call the Skills
        // component straight: dodging (Player.cs twice), the owner skill from a tamed creature
        // (Character.cs) and the skill credited when a shield breaks (SE_Shield.cs). Patching
        // Player.RaiseSkill therefore missed all of those. Skills.RaiseSkill is the real sink that
        // every path ends at - including Player.RaiseSkill - so one patch covers everything.
        //
        // Must not be combined with a Player.RaiseSkill patch: skills routed through both would be
        // multiplied twice.
        private static readonly FieldInfo SkillsPlayerField = AccessTools.Field(typeof(Skills), "m_player");

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        class Skills_RaiseSkill_Patch
        {
            static void Prefix(Skills __instance, ref float factor)
            {
                if (SkillsPlayerField == null)
                {
                    return;
                }
                // m_player is private in the shipped assembly, so it comes through AccessTools
                if (ReferenceEquals(SkillsPlayerField.GetValue(__instance), Player.m_localPlayer))
                {
                    factor *= CustomSkillGainRate.Value;
                }
            }
        }

        [HarmonyPatch(typeof(Character), "ApplyDamage")]
        class Character_ApplyDamage_PlayerSpecific_Patch
        {
            static bool Prefix(Character __instance, HitData hit, bool showDamageText, bool triggerEffects, HitData.DamageModifier mod)
            {
                // Handle damage dealt BY the local player TO enemies
                if (hit.GetAttacker() == Player.m_localPlayer && !__instance.IsPlayer())
                {
                    hit.ApplyModifier(CustomPlayerDamageRate.Value);
                    return true; // Continue with original method
                }

                // Handle damage taken BY the local player
                if (__instance == Player.m_localPlayer)
                {
                    Player player = __instance as Player;
                    float currentHealth = player.GetHealth();
                    float maxHealth = player.GetMaxHealth();
                    float totalDamage = hit.GetTotalDamage();
                    if (totalDamage <= 0f)
                    {
                        return true;
                    }

                    // An absolute floor rather than only a per-hit percentage. The old rule capped
                    // each hit at a share of current health, which quietly assumed damage always
                    // arrives one manageable hit at a time. A floor holds however large a single
                    // burst is and however many land together.
                    float floor = maxHealth * MinHealthPercent.Value;
                    float allowed = currentHealth - floor;
                    if (allowed <= 0f)
                    {
                        return false; // already at or under the floor
                    }

                    // ApplyDamage multiplies the hit by this after we return, so budget for it or
                    // the floor is breached on any world with a raised damage-taken rate
                    float takenRate = Game.m_localDamgeTakenRate;
                    if (takenRate > 0f)
                    {
                        allowed /= takenRate;
                    }

                    // Keep the original pacing caps layered on top of the floor
                    float healthPercentage = currentHealth / maxHealth;
                    allowed = Mathf.Min(allowed, currentHealth * (healthPercentage <= 0.5f ? 0.1f : 0.25f));

                    if (totalDamage > allowed)
                    {
                        hit.ApplyModifier(allowed / totalDamage);
                    }
                    if (LogPlayerDamage.Value)
                    {
                        Debug.Log($"Damage gate: type={hit.m_hitType} incoming={totalDamage:0.#} allowed={allowed:0.#} health={currentHealth:0.#}/{maxHealth:0.#}");
                    }
                    return true;
                }
                return true;
            }

            // The prefix predicts from the health it can see; this checks the outcome, so batched
            // or out-of-order hits cannot land below the floor between predictions
            static void Postfix(Character __instance)
            {
                EnforceHealthFloor(__instance);
            }
        }

    }
}
