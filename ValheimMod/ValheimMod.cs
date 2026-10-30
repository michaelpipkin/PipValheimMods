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
        private static ConfigEntry<bool> LogConsoleOutput;
        private static ConfigEntry<string> SuppressLogMessages;
        private static ConfigEntry<KeyboardShortcut> StatBucketRepairHotkey;
        private static ConfigEntry<KeyboardShortcut> AchievementProgressHotkey;
        private static ConfigEntry<KeyboardShortcut> ReloadConfigHotkey;
        private static ConfigEntry<KeyboardShortcut> HealthFloorToggleHotkey;
        private static ConfigEntry<KeyboardShortcut> FreeBuildHotkey;
        private static ConfigEntry<KeyboardShortcut> FlyToggleHotkey;
        private static ConfigEntry<KeyboardShortcut> NoDropsHotkey;
        private static ConfigEntry<KeyboardShortcut> DestroyItemCombo;
        private static ConfigEntry<float> DestroyItemConfirmSeconds;
        private static ConfigEntry<float> AreaRepairRadius;
        private static ConfigEntry<KeyCode> AreaRepairKey;
        private static ConfigEntry<string> UnlearnRecipes;
        private static ConfigEntry<bool> UnlearnRecipesApply;
        private static ConfigEntry<float> JumpBufferSeconds;
        private static ConfigEntry<bool> LogJumpBlocks;
        private static ConfigEntry<bool> StatBucketRepairApply;
        private static ConfigEntry<bool> GuaranteeFirstTrophy;
        private static ConfigEntry<bool> PreventStatRegression;
        private static ConfigEntry<float> UpgradeSuccessChance;
        private static ConfigEntry<bool> AutoPinDungeons;
        private static ConfigEntry<int> DungeonPinIcon;
        private static ConfigEntry<float> DungeonPinMergeRadius;
        private static ConfigEntry<string> DungeonPinLabel;
        private static ConfigEntry<bool> DungeonPinUseLocationName;
        private static ConfigEntry<string> DungeonPinNameOverrides;
        private static ConfigEntry<bool> DungeonPinIncludeCamps;
        private static ConfigEntry<bool> PinInteriorLocations;
        private static ConfigEntry<string> PinExcludeNames;
        private static ConfigEntry<string> PinIncludeLocations;
        private static ConfigEntry<bool> AutoPinResources;
        private static ConfigEntry<bool> PinDiagnostics;
        private static ConfigEntry<bool> SpawnDiagnostics;
        private static ConfigEntry<float> WardSpawnBlockRadius;
        private static ConfigEntry<bool> AutoPinKeepIconFilter;
        private static ConfigEntry<bool> ShowMapCursorCoordinates;
        private static ConfigEntry<string> HudFontName;
        private static ConfigEntry<bool> ShowMapSeed;
        private static ConfigEntry<bool> MapSeedClickToCopy;
        private static ConfigEntry<float> MapSeedPositionX;
        private static ConfigEntry<float> MapSeedPositionY;
        private static ConfigEntry<float> MapCursorPositionX;
        private static ConfigEntry<float> MapCursorPositionY;
        private static ConfigEntry<int> ResourcePinIcon;
        private static ConfigEntry<string> ResourcePinNames;
        private static ConfigEntry<float> ResourcePinMergeRadius;
        private static ConfigEntry<string> DungeonPinIconOverrides;
        private static ConfigEntry<bool> ShowAchievementProgress;
        private static ConfigEntry<bool> RevealSecretAchievements;
        private static ConfigEntry<bool> NegateDeathPenalty;

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
        private static ConfigEntry<int> PlayerLightLevels;
        private static ConfigEntry<float> PlayerLightLevelStep;
        private static ConfigEntry<float> WisplightRangeMultiplier;
        private static ConfigEntry<bool> PassiveWisplight;
        private static ConfigEntry<float> SwimSpeedAtMaxSkill;
        private static ConfigEntry<bool> MatchBoatCameraZoom;
        private static ConfigEntry<float> MaxCameraDistance;
        private static ConfigEntry<bool> SneakTweaks;
        private static ConfigEntry<float> SneakStealthAtMinSkill;
        private static ConfigEntry<float> SneakStealthAtMaxSkill;
        private static ConfigEntry<float> SneakTransitionSeconds;
        private static ConfigEntry<float> SneakSpeedPenalty;
        private static ConfigEntry<float> RunSpeedSkillBonus;
        private static ConfigEntry<float> JumpPowerSkillBonus;
        private static ConfigEntry<string> HoeLevelOpPrefab;
        private static ConfigEntry<bool> HoeLevelClearsRocks;
        private static ConfigEntry<string> HoeLevelRockNames;
        private static ConfigEntry<string> HoeLevelRockExclude;
        private static ConfigEntry<bool> HoeLevelScrollRadius;
        private static ConfigEntry<string> ScrollRadiusOps;
        private static ConfigEntry<float> HoeLevelMaxRadiusMultiplier;
        private static ConfigEntry<bool> HoeLevelPerfect;
        private static ConfigEntry<float> HoeLevelMaxDelta;
        private static ConfigEntry<bool> HideWisplightWisp;
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
        private static readonly MethodInfo ScreenToWorldPointMethod =
            AccessTools.Method(typeof(Minimap), "ScreenToWorldPoint", new[] { typeof(Vector3) });

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
            SmelterInputPriority = Config.Bind("Containers", "SmelterInputPriority", "GoldOre,FlametalOreNew,FlametalOre,BlackMetalScrap,SilverOre,IronScrap,CopperOre,TinOre", "Which ore a smelter, kiln or blast furnace reaches for first when several are available." + priorityHelp);
            CookingStationInputPriority = Config.Bind("Containers", "CookingStationInputPriority", "SerpentMeat,LoxMeat,BugMeat,ChickenMeat,HareMeat,WolfMeat,DeerMeat,RawMeat,NeckTail,FishRaw", "Which raw item a cooking station or oven reaches for first when several are available. The default is ordered roughly by biome progression and is only a starting point - reorder it to taste." + priorityHelp);
            ContainerRange = Config.Bind("Containers", "ContainerRange", 20f, "How far away, in metres, a container can be and still count toward crafting requirements.");
            MinHealthPercent = Config.Bind("General", "MinHealthPercent", 0.25f, new ConfigDescription("Damage can never take you below this fraction of your maximum health. 0.25 keeps you at a quarter health no matter how big the hit. Set to 0 to disable the floor and take damage normally.", new AcceptableValueRange<float>(0f, 0.95f)));
            LogPlayerDamage = Config.Bind("General", "LogPlayerDamage", false, "Diagnostic. Logs every hit that reaches the damage gate, with its type, the raw amount, what the gate allowed through and your health at the time. Use it to find out what actually killed you.");
            MassPlantKey = Config.Bind("Farming", "MassPlantKey", KeyCode.LeftShift, "Hold this while planting with the cultivator to plant a whole grid at once instead of a single seed.");
            MassPlantGridSize = Config.Bind("Farming", "MassPlantGridSize", 5, new ConfigDescription("Width of the grid planted while the mass-plant key is held. 5 plants a 5x5 block of 25. Set to 1 to disable.", new AcceptableValueRange<int>(1, 11)));
            PassiveForsakenPowers = Config.Bind("Powers", "PassiveForsakenPowers", true,
                "Keep every forsaken power this character has unlocked permanently active - no selecting, no activating, no cooldown. A power counts as unlocked once you have selected it at its stone, which the game records on the character rather than in the world, so unlocked powers follow you into any world. Powers carrying a networked status attribute (Moder's sailing power) stay off in a shared session, since that flag reaches any ship you are aboard.");
            SuppressLogMessages = Config.Bind("General", "SuppressLogMessages", "MagicaCloth component not found,Rotation quaternions must be unit length",
                "Comma separated fragments. Any log line from the game containing one is dropped before it is written. Meant for vanilla messages that repeat endlessly and say nothing actionable - the default is Iron Gate's own prefab-authoring warning, which fires several times a second and is addressed to them, not to you. Clear it to let everything through.");
            LogConsoleOutput = Config.Bind("General", "LogConsoleOutput", true,
                "Mirror in-game console output to the BepInEx log. The console keeps only a few lines and cannot be scrolled, so commands with long output - 'achievements' in particular - are unreadable in game without this.");
            HidePassivePowerIcons = Config.Bind("Powers", "HidePassivePowerIcons", true,
                "Keep passively held forsaken powers out of the status effect row. They are always on, so the icons carry no information and just crowd out effects that change. Only affects powers this mod is holding - a power you activate yourself, or one a nearby player grants you, still shows normally.");
            MassPlantSpacing = Config.Bind("Farming", "MassPlantSpacing", 2f, "Spacing between plants, as a multiple of the plant's own grow radius. 2 is the tightest that reliably clears each plant's space check, since the check tests a sphere of that radius against neighbouring colliders. Lower it to pack tighter at the risk of some seeds being rejected.");
            WorkstationRangeMultiplier = Config.Bind("General", "WorkstationRangeMultiplier", 1f, "Multiplier for how far a workbench, forge or other crafting station reaches for building. 2 doubles the radius. The on-screen coverage circle and the no-monster-spawn area both scale with it, the latter from the moment the station loads rather than from the first time you build. Applies to stations as they load, so change it before entering a world.");
            WardSpawnBlockRadius = Config.Bind("General", "WardSpawnBlockRadius", 80f, new ConfigDescription(
                "Radius in metres of the no-spawn area a ward projects, replacing its authored one - around 20m in vanilla, the same as a workbench. Note that this is not the ward's radius setting: that one governs who may build and what may be damaged, and no spawn system consults it. Suppresses biome spawns and dungeon creature spawners inside the sphere; nests and body piles ignore no-spawn areas entirely and still have to be destroyed. Applies to player-built wards as they load, active or not, so change it before entering a world. 0 leaves the authored radius alone. Local only: the area exists on your client and nobody else's.",
                new AcceptableValueRange<float>(0f, 200f)));
            SpawnDiagnostics = Config.Bind("General", "SpawnDiagnostics", true,
                "Logs a line every time the biome spawn system places a creature, naming the ground point it used, that point's altitude and ocean depth, "
                + "whether the spawn entry is exempt from crafting-station no-spawn areas, and the distance from that point to the nearest such area. "
                + "Answers why something appeared inside what looked like full workbench coverage: either the entry is exempt or the point was outside the blanket, and the line says which. "
                + "One line per creature, not per attempt. Pairs with the spawn-entry table the dump hotkey writes.");
            WorldEffectsSoloOnly = Config.Bind("General", "WorldEffectsSoloOnly", true, "Suspend the settings that change shared world state - CustomResourceRate, CustomStackSizeMultiplier, CustomSmelterOutputRate, CustomProcessingTimeRate and TamingSpeedMultiplier - whenever anyone else is in the world with you. They resume automatically once you are alone again. Settings that only affect you are never suspended.");
            TamingSpeedMultiplier = Config.Bind("General", "TamingSpeedMultiplier", 1f, "How much faster animals tame. 2 is twice as fast, 10 is ten times. Applies to every tameable creature; the animal must still be fed and calm for progress to happen at all.");
            AlwaysSlowFall = Config.Bind("General", "AlwaysSlowFall", false, "Apply the Feather Cape's slow-fall effect permanently, whatever cape you are wearing.");
            SlowFallMaxSpeed = Config.Bind("General", "SlowFallMaxSpeed", 0f, "Maximum downward speed in metres per second while AlwaysSlowFall is on. 0 copies the Feather Cape's own value, so it behaves exactly like the cape.");
            SlowFallNegatesFallDamage = Config.Bind("General", "SlowFallNegatesFallDamage", true, "Also apply the Feather Cape's fall-damage reduction. Fall damage in Valheim is based on distance fallen, not speed, so capping the speed alone does not prevent it.");
            PlayerLightHotkey = Config.Bind("Hotkeys", "PlayerLightHotkey", new KeyboardShortcut(KeyCode.Semicolon),
                "Cycles the personal light through its brightness levels and then off again. Starts off each time the game launches.");
            PlayerLightSourceItem = Config.Bind("General", "PlayerLightSourceItem", "Torch", "Prefab name of the item whose light is copied for the personal light. Torch gives a warm point light that lights all around you; HelmetDverger gives the circlet's narrower forward beam.");
            PlayerLightRange = Config.Bind("General", "PlayerLightRange", 0f, "Light radius in metres. 0 copies the source item's own value.");
            PlayerLightIntensity = Config.Bind("General", "PlayerLightIntensity", 0f, "Light brightness. 0 copies the source item's own value.");
            PlayerLightHeight = Config.Bind("General", "PlayerLightHeight", 1.7f, "Height above your feet, in metres, that the light sits at. Roughly head height by default.");
            HoeLevelClearsRocks = Config.Bind("Hoe", "HoeLevelClearsRocks", false,
                "Let a levelling pass also break the loose rocks and cliffs standing in the area. The hoe only ever moves the heightmap, so a boulder sitting on the ground is left poking out of otherwise flat terrain - this removes it as well. "
                + "Off by default because it destroys world objects and cannot be undone. Only objects matching HoeLevelRockNames are touched, player-built pieces are never touched, and it is suspended in a shared session.");
            HoeLevelRockNames = Config.Bind("Hoe", "HoeLevelRockNames", "rock1_,rock2_,rock3_,rock4_,Rock_3,Rock_4,Rock_7,Rock_destructible,cliff_,Ashlands_rock,BigRock",
                "Comma separated prefab name fragments that count as rock, matched case insensitively. The registry uses two unrelated schemes for boulders - rock1_mistlands / rock4_coast with the digit after 'rock', and Rock_3 / Rock_4 / Rock_7 with it after the underscore - so both shapes are listed. Note that a bare 'Rock_' would also catch caverock_ice_pillar_wall and HoleRock_rootFloor1, which are cave and dungeon structure. which is why the ore deposits are kept out by HoeLevelRockExclude rather than by the fragment itself. Use the nearby-objects dump from the dump hotkey to read the real names of whatever you are standing next to.");
            HoeLevelRockExclude = Config.Bind("Hoe", "HoeLevelRockExclude", "copper,silver,tin,iron,flametal,obsidian,meteorite,mudpile,BossStone,guardstone,Gemstone,Rocket,Runestone",
                "Fragments that are never broken even when they match HoeLevelRockNames, checked before the include list. Every ore is named individually rather than excluding MineRock wholesale, because the plain stone deposits are MineRock prefabs too and those are exactly what a levelling pass should be able to clear. Ore deposits are the obvious case, but the registry is full of names that a loose fragment would catch by accident - BossStone_Eikthyr and the other boss altars contain \"Stone_\", and FireworksRocket contains \"rock\".");
            SneakTweaks = Config.Bind("Sneak", "SneakTweaks", true,
                "Replace the stealth calculation with the settings below. The one change not covered by a setting is that light is ignored: vanilla makes a creature spot you from up to twice as far when you are lit, which means carrying any light source undoes sneaking regardless of skill. "
                + "Off restores vanilla entirely, including its light handling. Suspended in a shared session.");
            SneakStealthAtMinSkill = Config.Bind("Sneak", "SneakStealthAtMinSkill", 0.5f, new ConfigDescription(
                "Fraction of its normal sight and alert range a creature keeps against you while crouched at sneak skill 0. Vanilla is 0.5 in darkness and 1.0 in bright light.",
                new AcceptableValueRange<float>(0.01f, 1f)));
            SneakStealthAtMaxSkill = Config.Bind("Sneak", "SneakStealthAtMaxSkill", 0.05f, new ConfigDescription(
                "The same fraction at sneak skill 100, scaling linearly from SneakStealthAtMinSkill. Vanilla is 0.2 in darkness and 0.6 in bright light, so 0.05 is a quarter of vanilla's best case.",
                new AcceptableValueRange<float>(0.01f, 1f)));
            SneakTransitionSeconds = Config.Bind("Sneak", "SneakTransitionSeconds", 0.5f, new ConfigDescription(
                "Seconds to move the whole way between fully visible and fully hidden after crouching or standing. Vanilla is 4, which is long enough that crouching to avoid something already in view rarely helps.",
                new AcceptableValueRange<float>(0.1f, 4f)));
            SneakSpeedPenalty = Config.Bind("Sneak", "SneakSpeedPenalty", 0.25f, new ConfigDescription(
                "How much of the crouch movement penalty to keep. 1 is the vanilla crouch speed, 0 removes the penalty entirely so crouching moves at your normal jog, and 0.25 keeps a quarter of it. "
                + "Running still overrides crouching, and this does not touch the sneak stamina drain. Suspended in a shared session.",
                new AcceptableValueRange<float>(0f, 1f)));
            RunSpeedSkillBonus = Config.Bind("Skills", "RunSpeedSkillBonus", 0.25f, new ConfigDescription(
                "Extra run speed at run skill 100, as a fraction. Vanilla is 0.25, so a fully trained runner is 25% faster than an untrained one; 1 would make them twice as fast. Scales linearly with the skill and stacks with the equipment modifier exactly as vanilla does.",
                new AcceptableValueRange<float>(0f, 5f)));
            JumpPowerSkillBonus = Config.Bind("Skills", "JumpPowerSkillBonus", 0.4f, new ConfigDescription(
                "Extra jump power at jump skill 100, as a fraction. Vanilla is 0.4. This is the same number vanilla applies to both the upward and the forward part of a jump, so raising it increases distance as well as height. "
                + "Stamina cost does not change, and a tired jump is still weakened afterwards.",
                new AcceptableValueRange<float>(0f, 5f)));
            MatchBoatCameraZoom = Config.Bind("Camera", "MatchBoatCameraZoom", true,
                "Allow the same zoom-out distance on foot that the game already allows at a ship's helm. Vanilla keeps two separate limits and picks between them by whether you are steering, so on foot you are held closer in for no reason that affects anything. "
                + "Purely a local camera setting - nothing about it is sent anywhere or seen by anyone else. Ignored when MaxCameraDistance is set.");
            MaxCameraDistance = Config.Bind("Camera", "MaxCameraDistance", 0f, new ConfigDescription(
                "Maximum third-person camera distance in metres, overriding MatchBoatCameraZoom. 0 leaves both alone. The vanilla on-foot and boat limits are written to the log once when a world loads, so there is a number to compare against.",
                new AcceptableValueRange<float>(0f, 60f)));
            SwimSpeedAtMaxSkill = Config.Bind("General", "SwimSpeedAtMaxSkill", 1f, new ConfigDescription(
                "Swim speed at swim skill 100, as a multiple of normal. Scales smoothly from 1x at skill 0, so 10 means a fully trained swimmer moves ten times as fast and an untrained one is unchanged. Vanilla does not tie swim speed to the skill at all - the skill only reduces stamina drain - so this adds the scaling rather than steepening it. 1 leaves swimming alone. "
                + "Stamina is spent per second rather than per metre, so this also multiplies how far a given amount of stamina carries you. Suspended in a shared session.",
                new AcceptableValueRange<float>(1f, 20f)));
            HoeLevelOpPrefab = Config.Bind("Hoe", "HoeLevelOpPrefab", "mud_road_v2",
                "The terrain operation these settings apply to, by prefab name. The hoe's Level ground is mud_road_v2; the name of every operation and what it does is in PipsMod_TerrainOps.tsv, written by the dump hotkey. "
                + "Named explicitly rather than detected, because Cultivate and Pave smooth the ground too and should be left alone.");
            ScrollRadiusOps = Config.Bind("Hoe", "ScrollRadiusOps", "mud_road_v2,cultivate_v2,replant_v2",
                "Which terrain operations the mouse wheel resizes, as comma separated prefab names - mud_road_v2 is the hoe's Level ground, cultivate_v2 the cultivator's Cultivate and replant_v2 its Plant grass. Each one remembers its own size. Press the dump hotkey in a world to write PipsMod_TerrainOps.tsv and see every name; path_v2, paved_road_v2 and raise_v2 work here too if you want them. Only the radius is changed - the perfect levelling and rock clearing stay with the Level ground op named by HoeLevelOpPrefab.");
            HoeLevelScrollRadius = Config.Bind("Hoe", "HoeLevelScrollRadius", true,
                "Let the mouse wheel resize the hoe's level-ground area while it is selected. The wheel is free in that state - vanilla only uses it to rotate a piece that can rotate, and a terrain operation cannot - so this takes it over and stops it zooming the camera. 1x is the vanilla size and the setting is not remembered between sessions.");
            HoeLevelMaxRadiusMultiplier = Config.Bind("Hoe", "HoeLevelMaxRadiusMultiplier", 2f, new ConfigDescription(
                "How far the wheel can grow the level-ground area, as a multiple of the vanilla radius.",
                new AcceptableValueRange<float>(1f, 4f)));
            HoeLevelPerfect = Config.Bind("Hoe", "HoeLevelPerfect", true,
                "Make level ground actually level. Vanilla does not flatten at all - it lerps each point partway towards your feet, weighted by distance from the centre, so only the exact centre ever arrives, and caps the whole move at 1 metre. This sets every point in the circle to your foot height instead, up to HoeLevelMaxDelta. "
                + "Suspended in a shared session, because terrain is permanent world state everyone sees.");
            HoeLevelMaxDelta = Config.Bind("Hoe", "HoeLevelMaxDelta", 8f, new ConfigDescription(
                "How far one point may be moved by a levelling pass, in metres. Vanilla allows 1, which is the reason a step in the ground survives however many times it is levelled.",
                new AcceptableValueRange<float>(1f, 32f)));
            PassiveWisplight = Config.Bind("General", "PassiveWisplight", false,
                "Clear the Mistlands mist without carrying the Wisplight, once this character has crafted one at least once. "
                + "This frees the utility slot, which is a real gameplay change rather than a convenience, so it starts off. "
                + "Suspended in a shared session: the wisp is a networked object, so others would see one orbiting a player with nothing equipped.");
            HideWisplightWisp = Config.Bind("General", "HideWisplightWisp", false,
                "Hide the floating wisp. Demisting is unaffected - that comes from a force field on the ball rather than from anything visible - but the ball also carries the light it gives off, so hiding it costs that light too. Applies to an equipped Wisplight as well as a passive one.");
            WisplightRangeMultiplier = Config.Bind("General", "WisplightRangeMultiplier", 1f, new ConfigDescription(
                "Multiplies how far the Wisplight pushes the Mistlands mist back. 1 leaves it alone. This is the demisting radius, not the glow - the light the wisp gives off is unchanged.",
                new AcceptableValueRange<float>(1f, 20f)));
            PlayerLightLevels = Config.Bind("General", "PlayerLightLevels", 3, new ConfigDescription(
                "How many brightness levels the light hotkey cycles through before switching off. Level 1 is the source item unchanged.",
                new AcceptableValueRange<int>(1, 6)));
            PlayerLightLevelStep = Config.Bind("General", "PlayerLightLevelStep", 2f, new ConfigDescription(
                "What each level multiplies the light radius by. 2 doubles the reach per step, so level 3 covers four times the distance of the source item. Brightness is not scaled - set that once with PlayerLightIntensity.",
                new AcceptableValueRange<float>(1f, 4f)));
            WeightlessPlayerInventory = Config.Bind("General", "WeightlessPlayerInventory", false, "Treat everything in your own inventory as weighing nothing, so slots are the only limit. Containers, carts and other players are unaffected. Makes CustomMaxCarryWeight irrelevant while enabled.");
            SuppressPickupMessages = Config.Bind("General", "SuppressPickupMessages", false, "Stop 'picked up <item>' notifications from being queued in the top-left message HUD, so they can't delay more important messages.");
            SuppressRemovedMessages = Config.Bind("General", "SuppressRemovedMessages", false, "Stop 'removed <item>' notifications from being queued in the top-left message HUD.");
            SuppressStationAddedMessages = Config.Bind("General", "SuppressStationAddedMessages", false, "Stop the centre-screen 'added <item>' confirmation shown when loading ore or fuel into a smelter, kiln, cooking station, turret or shield generator.");
            SuppressSkillMessages = Config.Bind("General", "SuppressSkillMessages", false, "Stop skill notifications from being queued in the message HUD. Covers both Valheim's own skill level-up messages and this mod's per-tick skill progress readout.");
            NegateKnockback = Config.Bind("General", "NegateKnockback", true, "Turn off knockback when hit");
            NegateEquipmentMovementPenalty = Config.Bind("General", "NegateEquipPenalty", true, "Turn off equipment movement penalty");

            RepairHotkey = Config.Bind("Hotkeys", "RepairHotkey", new KeyboardShortcut(KeyCode.LeftBracket), "Hotkey to repair all gear in inventory, heal player, replenish ammo, and spawn or replenish favorite foods");
            DumpRealItemsOnly = Config.Bind("General", "DumpRealItemsOnly", true, "Limit the item dump to real items. Creature attack prefabs carry plain text in place of a localisation token, and character customisation uses $customization, so both are dropped. Turn off to dump every entry in ObjectDB.");
            NegateDeathPenalty = Config.Bind("General", "NegateDeathPenalty", false,
                "Die with no consequences: keep every item including unequipped ones, leave no tombstone, and lose no skill levels. Intended for deliberately dying, such as working through the death achievements. Off by default because it removes the main cost of dying. Suspended in a shared session.");
            RevealSecretAchievements = Config.Bind("General", "RevealSecretAchievements", true,
                "Show the real name, description and requirements of achievements the game marks secret, instead of 'Concealed'. Also makes their tiles clickable, which vanilla disables. Display only - the same information Steam shows on its global achievements page.");
            ShowAchievementProgress = Config.Bind("General", "ShowAchievementProgress", true,
                "Show real numbers on achievement requirements you have not finished yet, instead of the vanilla '??? / ???'. Display only - it reads the same stats the panel already reads. Achievements flagged secret stay hidden.");
            AutoPinDungeons = Config.Bind("Map", "AutoPinDungeons", true,
                "Drop a map pin on a dungeon the moment its interior loads, which happens when you get close enough for its zone to load. Skipped if a saved pin is already nearby, so walking past one repeatedly does not pile up duplicates.");
            DungeonPinIcon = Config.Bind("Map", "DungeonPinIcon", 2,
                "Which Minimap.PinType to use. 0=Icon0 1=Icon1 2=Icon2 3=Icon3 4=Death 5=Bed 6=Icon4 7=Shout 8=None 9=Boss 10=Player 11=RandomEvent 12=Ping 13=EventArea 14=Hildir1 15=Hildir2 16=Hildir3 17=Memorial. The log prints the full table with each sprite name the first time a dungeon is pinned, so you can pick the one you want by name.");
            DungeonPinMergeRadius = Config.Bind("Map", "DungeonPinMergeRadius", 40f,
                "How close an existing saved pin has to be, in metres, for a dungeon to be considered already marked.");
            DungeonPinLabel = Config.Bind("Map", "DungeonPinLabel", "Dungeon",
                "Label for automatic dungeon pins. Ignored when DungeonPinUseLocationName is on.");
            DungeonPinIncludeCamps = Config.Bind("Map", "DungeonPinIncludeCamps", true,
                "Also pin surface camps - Fuling villages, Meadows villages and farms. DungeonGenerator builds these as well as real dungeons, "
                + "told apart by its m_algorithm field. Turn off to keep the map to actual dungeons only.");
            PinExcludeNames = Config.Bind("Map", "PinExcludeNames", "FortressRuins,AshlandRuins",
                "Never pin anything whose location, generator or prefab name contains one of these comma separated fragments, case insensitive. "
                + "The Ashlands are full of ruins that qualify as dungeons structurally but hold nothing worth walking back to, and they bury the useful pins. "
                + "Checked before everything else, so an excluded name cannot be brought back by a rule elsewhere.");
            PinIncludeLocations = Config.Bind("Map", "PinIncludeLocations", "CharredFortress,Mistlands_Excavation,Mistlands_Harbour,Mistlands_Lighthouse"
                + ",Mistlands_GuardTower1_new,Mistlands_GuardTower2_new,Mistlands_GuardTower3_new",
                "Also pin surface locations whose name contains one of these comma separated fragments, even though they have no interior and no dungeon generator. "
                + "Those two are what the other hooks key off, so a structure that is simply built on the surface - a charred fortress, for instance - is otherwise invisible to all of them. "
                + "PinExcludeNames still wins, so a fragment listed in both is excluded.");
            HudFontName = Config.Bind("Clock", "HudFontName", "AveriaSerifLibre-Bold",
                "Font for the clock, coordinates and map cursor readout, matched against the fonts the game has loaded. AveriaSerifLibre is the body font the game uses for item and effect names; Norsebold is the all-caps display font behind headings. An exact name beats a partial one, so AveriaSerifLibre-Bold selects that variant specifically. Leave empty for Unity's default. If the named font is not found, the log lists every font that is.");
            ShowMapCursorCoordinates = Config.Bind("Map", "ShowMapCursorCoordinates", true,
                "While the full map is open, show the world coordinates under the mouse cursor. Useful for finding a spot someone else has given you coordinates for on a known seed.");
            ShowMapSeed = Config.Bind("Map", "ShowMapSeed", true,
                "Show the world seed in the top right of the map while it is open, mirroring the cursor coordinates in the top left.");
            MapSeedClickToCopy = Config.Bind("Map", "MapSeedClickToCopy", true,
                "Clicking the seed copies it to the system clipboard, for pasting into a map generator site. The clickable area is only as wide as the text itself. Turn this off if the click interferes with dragging the map.");
            MapSeedPositionX = Config.Bind("Map", "MapSeedPositionX", 12f,
                "Distance in pixels from the right edge of the map panel to the end of the seed text. Measured from the panel rather than the screen, so it stays in the corner of the map on an ultrawide display instead of drifting out into the surrounding HUD.");
            MapSeedPositionY = Config.Bind("Map", "MapSeedPositionY", 40f,
                "Distance in pixels from the top edge of the map panel to the seed text. The default clears the biome name vanilla already draws in that corner - raising it much above 40 will overlap it again.");
            MapCursorPositionX = Config.Bind("Map", "MapCursorPositionX", 12f,
                "Map cursor readout offset in pixels from the left edge of the map panel, not the screen.");
            MapCursorPositionY = Config.Bind("Map", "MapCursorPositionY", 12f,
                "Map cursor readout offset in pixels from the top edge of the map panel, not the screen.");
            AutoPinKeepIconFilter = Config.Bind("Map", "AutoPinKeepIconFilter", true,
                "Leave a hidden pin type hidden when an automatic pin is added. Valheim un-hides an icon type whenever a pin of that type appears, "
                + "which means hiding icons to keep the map clear while exploring does not survive the next thing that gets pinned automatically. "
                + "Turn this off to get the vanilla behaviour, where a new pin always reveals its type.");
            PinDiagnostics = Config.Bind("Map", "PinDiagnostics", false,
                "Logs a line every time an automatic pin is declined, naming the reason and - when an existing pin blocked it - that pin's name and distance. Off by default because a dungeon re-pins every time its zone reloads, which would fill the log. Turn it on for a session when something that should have been pinned was not.");
            AutoPinResources = Config.Bind("Map", "AutoPinResources", true,
                "Pin resource nodes as they load - tar pits, ore deposits, scrap piles.");
            ResourcePinIcon = Config.Bind("Map", "ResourcePinIcon", 3,
                "Minimap.PinType index for resource pins, separate from the dungeon icon. 3 is the dot.");
            ResourcePinNames = Config.Bind("Map", "ResourcePinNames",
                "rock4_copper=Copper,MineRock_Copper=Copper,rock3_silver=Silver,silvervein=Silver,mudpile=Mud Pile,FlametalRockstand=Flametal,LeviathanLava=Flametal,TarPit=Tar Pit,NorthMemorial=Memorial,DragonEgg=Dragon Egg,Leviathan=Leviathan,Mistlands_Giant=Jotunn Skeleton,YggdrasilRoot=Root,TrollFrost_Dead=Petrified Gameltroll,TrollFrost_Frac=Petrified Gameltroll",
                "Comma separated prefab=label pairs. The prefab part is matched as a case insensitive fragment, so one entry covers every variant - mudpile also catches mudpile2. "
                + "These are the real prefab names from the registry scan: the intact deposit you walk up to is rock4_copper or silvervein, while the _frac MineRock5 version only exists once mined. "
                + "An entry with no '=' still works and gets a name derived from the prefab. Tin is deliberately absent. Every pin logs the prefab it matched.");
            ResourcePinMergeRadius = Config.Bind("Map", "ResourcePinMergeRadius", 30f,
                "How close an existing pin has to be, in metres, for a resource node to count as already marked. "
                + "Deposits cluster, so this is what stops one copper field becoming a dozen pins.");
            PinInteriorLocations = Config.Bind("Map", "PinInteriorLocations", true,
                "Also pin locations that declare an interior but build it as one fixed space rather than from rooms - troll caves, bear caves, putrid holes. "
                + "Those carry no room data, so DungeonGenerator never reaches the stage the other hook listens for and they were never pinned. "
                + "Keyed off Location.m_hasInterior, the same flag the game uses to decide whether to create an interior zone at all.");
            DungeonPinIconOverrides = Config.Bind("Map", "DungeonPinIconOverrides", "Fuling Camp=0,Dvergr Camp=0,Dvergr Harbor=0,Dvergr Tower=0,Dvergr Guard Tower=0,Draugr Village=0,Meadows Farm=0,North Village=0,GoblinCamp2=0,DG_NorthVillage=0",
                "Per-type icons, as comma separated name=index pairs, for example 'Fuling Camp=0,Infested Mine=4'. "
                + "The name can be the label that ends up on the pin, which is the stable choice since every variant of a type resolves to the same one, "
                + "or the surface location name for a single variant. Indexes are the same Minimap.PinType numbers as DungeonPinIcon, which is used for anything unlisted.");
            DungeonPinNameOverrides = Config.Bind("Map", "DungeonPinNameOverrides", "",
                "Optional renames, as comma separated location=label pairs, for example 'MountainCave01=Frost Cave,TrollCave=Troll Cave'. "
                + "Key on the surface location name the log prints, not the DG_ generator name - one generator is shared by several dungeon types, so DG_Cave would rename Troll Caves and Frost Caves alike. "
                + "Only needed when a dungeon has no discover label of its own, or when its in-game name is not what you want on the map.");
            DungeonPinUseLocationName = Config.Bind("Map", "DungeonPinUseLocationName", true,
                "Label each pin with the dungeon prefab name - MountainCave01, Crypt3, SunkenCrypt4 and so on - so the map tells you which caves are worth the climb. Turn off to label every dungeon with the fixed DungeonPinLabel instead.");
            UpgradeSuccessChance = Config.Bind("General", "UpgradeSuccessChance", 0f, new ConfigDescription(
                "Success chance at the Forge of Potential, as a fraction. 1 never fails and never destroys the item, 0.65 is the vanilla value for most upgraders. Leave at 0 to keep whatever each upgrader ships with. The chance belongs to the upgrader item rather than the station, so this applies to every upgrader at once.",
                new AcceptableValueRange<float>(0f, 1f)));
            PreventStatRegression = Config.Bind("General", "PreventStatRegression", true,
                "Stop the vanilla bucket bug from overwriting a high-water-mark achievement stat with a smaller number. Only the stats that record a maximum are protected, so resets that are supposed to happen - the consecutive-day streak zeroing on death - still work. Without this, repairs made by the StatBucketRepair hotkey are undone the next time the game writes one of these stats.");
            GuaranteeFirstTrophy = Config.Bind("Drops", "GuaranteeFirstTrophy", true,
                "Guarantee a trophy drop the first time you kill a creature whose trophy you have never collected. Once collected, that creature rolls its normal chance again. Rare spawns like wraiths, fenrings and serpents are otherwise close to unfarmable. Suspended in a shared session, since drops are rolled by whoever owns the creature.");
            UnlearnRecipes = Config.Bind("General", "UnlearnRecipes", "",
                "Comma separated recipe tokens to forget, as they appear in the log line 'Queue unlock msg:$msg_newpiece:$piece_x'. Runs once per session and reports what it would remove; set UnlearnRecipesApply to actually remove them. Clear this when done.");
            UnlearnRecipesApply = Config.Bind("General", "UnlearnRecipesApply", false,
                "Let UnlearnRecipes actually write to the character. Off means it only reports. Back up the character .fch before turning this on.");
            DestroyItemCombo = Config.Bind("Inventory", "DestroyItemCombo", new KeyboardShortcut(KeyCode.LeftShift, KeyCode.LeftControl),
                "Held while left-clicking an item in your own inventory to destroy it outright - an Obliterator you carry with you. Every key in the combo has to be down at the moment of the click; set the main key to None to turn this off. Only your own inventory is touched, never a container, and quest items are refused the same way vanilla refuses to let you drop them.");
            DestroyItemConfirmSeconds = Config.Bind("Inventory", "DestroyItemConfirmSeconds", 3f, new ConfigDescription(
                "A first combo-click arms the item and names what will be lost; a second one on the same item within this many seconds destroys it. "
                + "Worth keeping, because vanilla already uses plain Shift+click to split a stack - with no confirmation, one stray Ctrl turns a split into an unrecoverable loss. Set to 0 to destroy on the first click.",
                new AcceptableValueRange<float>(0f, 30f)));
            AreaRepairRadius = Config.Bind("Build", "AreaRepairRadius", 20f, new ConfigDescription(
                "Radius in metres repaired in one go when AreaRepairKey is held and the hammer's repair tool is selected. 0 turns it off. "
                + "Each piece costs its normal stamina and tool durability, and repairing stops early when either runs out.",
                new AcceptableValueRange<float>(0f, 64f)));
            AreaRepairKey = Config.Bind("Build", "AreaRepairKey", KeyCode.LeftShift,
                "Held while clicking with the repair tool to repair everything in range instead of only the piece under the cursor.");
            NoDropsHotkey = Config.Bind("Hotkeys", "NoDropsHotkey", new KeyboardShortcut(KeyCode.Alpha0),
                "Toggles drops off entirely for trees, rocks, world destructibles and creature kills, for clearing ground without filling your inventory with wood, stone and whatever attacked you while you worked. Overrides CustomResourceRate while it is on. Bosses still drop everything, since their trophies and summoning items are not replaceable. Pieces you built still drop their materials, and container loot, pickables and fishing are unaffected. Starts off each time the game launches, and is suspended in a shared session since the drops would be somebody else's.");
            FlyToggleHotkey = Config.Bind("Hotkeys", "FlyToggleHotkey", new KeyboardShortcut(KeyCode.F, KeyCode.LeftShift),
                "Toggles the same free-flight the console 'fly' command gives, without opening the console. "
                + "Vanilla reaches this through a cheat command, but the method behind it carries no such check, so this needs neither devcommands nor the console - which also means it does not mark the character as having used cheats. "
                + "Suspended in a shared session: a player flying is about as conspicuous as it gets.");
            FreeBuildHotkey = Config.Bind("Hotkeys", "FreeBuildHotkey", new KeyboardShortcut(KeyCode.Slash),
                "Toggles building and crafting without resources, for when you are far from base and need something you cannot carry the materials for. Crafting still needs its station, and a piece you place while this is on drops nothing when you take it back down. Starts off each time the game launches, and is suspended in a shared session since the pieces it leaves behind are part of the world everyone sees.");
            JumpBufferSeconds = Config.Bind("General", "JumpBufferSeconds", 0.3f, new ConfigDescription(
                "How long a jump press is remembered and retried when the game refuses it, in seconds. A press that lands a fraction too early - while still in the air from a stride or a step off a ledge - is otherwise dropped silently. Set to 0 to turn the buffer off.", new AcceptableValueRange<float>(0f, 1f)));
            LogJumpBlocks = Config.Bind("General", "LogJumpBlocks", false,
                "Diagnostic. On every jump press, log the state the game decides from: whether you are grounded, blocking, crouched, attacking and so on. Use it to find out what is actually swallowing a jump.");
            HealthFloorToggleHotkey = Config.Bind("Hotkeys", "HealthFloorToggleHotkey", new KeyboardShortcut(KeyCode.Minus),
                "Turns the MinHealthPercent damage floor on and off without editing the config. The floor keeps you alive through things like Ashlands lava, but it also makes it impossible to die on purpose - sailing off the edge of the world leaves no other way out. Starts on each time the game launches.");
            ReloadConfigHotkey = Config.Bind("Hotkeys", "ReloadConfigHotkey", new KeyboardShortcut(KeyCode.Equals),
                "Re-read this file from disk so edits take effect without relaunching. BepInEx has no file watcher, so nothing else notices the file changing - values parsed at startup are kept for the lifetime of the process. Settings read every frame apply at once; anything applied only at startup, such as which Harmony patches exist, still needs a relaunch.");
            AchievementProgressHotkey = Config.Bind("Hotkeys", "AchievementProgressHotkey", new KeyboardShortcut(KeyCode.Period),
                "Writes every unfinished achievement and the requirements still outstanding to a file in the BepInEx folder, next to LogOutput.log, so a long list like the craft-everything achievements can be worked through without reopening the panel after each item.");
            StatBucketRepairHotkey = Config.Bind("Hotkeys", "StatBucketRepairHotkey", new KeyboardShortcut(KeyCode.Backslash),
                "Reports achievement stats that the vanilla bucket-latch bug has frozen, and repairs them when StatBucketRepairApply is on. Must be pressed in a loaded world.");
            StatBucketRepairApply = Config.Bind("General", "StatBucketRepairApply", false,
                "Let the stat bucket repair hotkey actually write to the character profile. Off means it only reports what it would change. Back up the character .fch file before turning this on.");
            DumpItemListHotkey = Config.Bind("Hotkeys", "DumpItemListHotkey", new KeyboardShortcut(KeyCode.RightBracket), "Hotkey to dump reference files - items, locations, prefabs, terrain operations, map pins and nearby objects - into the BepInEx folder, next to LogOutput.log. Must be pressed in a loaded world.");

            FavoriteFoodList = Config.Bind("Inventory", "FavoriteFoods", "MooseKebab,SealSoup,SmokedMooseMeat,Pancakes,OatmealLingonberryJam,OatMilk,OvenPancake,MeatballsMashedPoteitr,FishSoup", "Comma-separated list of foods to spawn");
            FavoriteAmmoList = Config.Bind("Inventory", "FavoriteAmmo", "ArrowCarapace,BoltCarapace", "Comma-separated list of ammo to replenish when repairing gear");

            // After every Bind, since this reads the entries. Config.Reload swaps each ConfigEntry
            // value in place, but a cache parsed out of one of those strings has to be rebuilt to
            // match, so the same routine runs again on reload.
            RebuildDerivedConfig();
            Config.ConfigReloaded += (sender, args) => RebuildDerivedConfig();

            Game.isModded = true;

            harmony.PatchAll();
        }

        private void Update()
        {
            if (HotkeyDown(RepairHotkey))
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

            if (HotkeyDown(DumpItemListHotkey))
            {
                DumpItemList();
            }

            if (HotkeyDown(StatBucketRepairHotkey))
            {
                RepairStatBuckets();
            }

            if (HotkeyDown(AchievementProgressHotkey))
            {
                DumpAchievementProgress();
            }

            if (HotkeyDown(ReloadConfigHotkey))
            {
                ReloadModConfig();
            }

            if (HotkeyDown(HealthFloorToggleHotkey))
            {
                ToggleHealthFloor();
            }

            if (HotkeyDown(FreeBuildHotkey))
            {
                ToggleFreeBuild();
            }

            if (HotkeyDown(FlyToggleHotkey))
            {
                ToggleDebugFlight();
            }

            if (HotkeyDown(NoDropsHotkey))
            {
                ToggleNoDrops();
            }

            ApplyInventoryRows();
            UpdatePlayerLight();
            UpdateMassPlantPreview();
            ApplyResourceRate();
            RefreshStackSizeMultiplier();
            RefreshUpgradeChance();
            UpdateJumpBuffer();
            ProcessUnlearnRecipes();
            FlushPendingPins();
            UpdateWatchedPins();
            UpdatePassiveForsakenPowers();
            UpdatePassiveWisplight();
            UpdateScrollRadius();
            ApplyCameraDistance();
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

        // 0 is off; 1 is the source item unchanged, each level above scales by PlayerLightLevelStep
        private static int _playerLightLevel;
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

            int levels = Mathf.Clamp(PlayerLightLevels.Value, 1, 6);
            // Lowering the level count while a brighter level is active would otherwise strand the
            // light above the top of the cycle
            if (_playerLightLevel > levels)
            {
                _playerLightLevel = levels;
            }

            if (HotkeyDown(PlayerLightHotkey))
            {
                // One step past the brightest level wraps to off, so the cycle is levels + 1 long
                _playerLightLevel = (_playerLightLevel + 1) % (levels + 1);
            }

            if (_playerLightLevel <= 0)
            {
                if (HotkeyDown(PlayerLightHotkey))
                {
                    _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "Light off");
                }
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
            // Range only. Scaling intensity alongside it made each step blinding rather than
            // wider - brightness stays wherever PlayerLightIntensity puts it, and the levels just
            // reach further. Level 1 leaves the source item untouched, so the exponent is one less
            // than the level.
            range *= Mathf.Pow(Mathf.Max(1f, PlayerLightLevelStep.Value), _playerLightLevel - 1);
            if (HotkeyDown(PlayerLightHotkey))
            {
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                    $"Light {_playerLightLevel}/{levels} - range {range:0.#}");
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

        // ---------------- Wisplight range ----------------
        // The Wisplight clears mist with a ParticleSystemForceField on the wisp ball, and the field's
        // endRange is the radius. Demister.Awake caches that component, so scaling it in a postfix
        // catches each ball as it is created and never has to run again - the ball is spawned once
        // when the status effect starts.
        //
        // endRange is read and written by reflection rather than typed directly, because
        // ParticleSystemForceField lives in UnityEngine.ParticleSystemModule, which this project
        // would otherwise have to reference for one float. The PropertyInfo is cached on first use.
        //
        // The multiplier is applied against the value the prefab shipped with, remembered per field,
        // so changing the setting adjusts from the original rather than compounding on what was
        // already written.
        // Even assigning m_forceField to an object needs ParticleSystemForceField at compile time,
        // so the field is read reflectively as well
        private static readonly FieldInfo DemisterForceFieldField = AccessTools.Field(typeof(Demister), "m_forceField");
        private static PropertyInfo _forceFieldEndRangeProperty;
        private static float _wisplightLoggedMultiplier = float.NaN;

        private static void ApplyWisplightRange(Demister demister)
        {
            float multiplier = Mathf.Max(1f, WisplightRangeMultiplier.Value);
            if (multiplier == 1f || demister == null || DemisterForceFieldField == null)
            {
                return;
            }
            object field = DemisterForceFieldField.GetValue(demister);
            if (field == null)
            {
                return;
            }
            if (_forceFieldEndRangeProperty == null)
            {
                _forceFieldEndRangeProperty = AccessTools.Property(field.GetType(), "endRange");
                if (_forceFieldEndRangeProperty == null)
                {
                    Debug.LogWarning("Wisplight: could not find endRange on the force field");
                    return;
                }
            }
            if (!(_forceFieldEndRangeProperty.GetValue(field, null) is float current))
            {
                return;
            }
            // Scaled in place rather than against a remembered original. Awake runs once per
            // instance and the value it sees is the prefab's, so there is nothing to compound with -
            // and demisters turn out to be created often enough, with different base ranges per
            // instance, that keeping a table of originals was both pointless and unbounded.
            _forceFieldEndRangeProperty.SetValue(field, current * multiplier, null);
            // Once per multiplier value, not once per demister; the old line fired every time the
            // game spawned one, which is constantly
            if (_wisplightLoggedMultiplier != multiplier)
            {
                _wisplightLoggedMultiplier = multiplier;
                Debug.Log($"Wisplight demisting range set to {multiplier:0.##}x");
            }
        }

        /// <summary>
        /// Hides the wisp ball belonging to the player's own demister effect, and only that.
        ///
        /// The first attempt hooked Demister.Awake and hid every Demister in the world. The log
        /// showed what that actually caught: Ravens(Clone) - Hugin and Munin, 6 renderers and 2
        /// lights - plus dverger_demister, dverger_demister_ruins and the Wisplight item model.
        /// Demister is a shared component, so the player's wisp has to be identified by ownership
        /// rather than by type: SE_Demister holds the instance it created in m_ballInstance.
        ///
        /// Climbing to the transform root is gone with it. That was only needed because the
        /// component was the starting point; the ball instance is already the root of its own
        /// hierarchy, and climbing from a raven's Demister is how the ravens got hidden.
        ///
        /// Disabling the Light is not enough on its own. LightLod owns that flag - its UpdateLoop
        /// forces m_light.enabled = true and ramps range back toward m_baseRange, then waits
        /// s_waitFor1Sec - so a disabled light came back about once a second, which is the pulse
        /// the player saw. Disabling the LightLod component stops its coroutine via OnDisable, so
        /// nothing contests the flag afterwards.
        /// </summary>
        private static void ApplyWisplightVisibility(StatusEffect effect)
        {
            if (!(effect is SE_Demister) || BallInstanceField == null)
            {
                return;
            }
            var ball = BallInstanceField.GetValue(effect) as GameObject;
            if (ball == null)
            {
                return;   // SE_Demister has not spawned it yet; the next sweep will catch it
            }
            bool visible = !HideWisplightWisp.Value;

            // Order matters: stop LightLod before touching the light, or its loop can re-enable it
            foreach (LightLod lod in ball.GetComponentsInChildren<LightLod>(true))
            {
                lod.enabled = visible;
            }
            Renderer[] renderers = ball.GetComponentsInChildren<Renderer>(true);
            Light[] lights = ball.GetComponentsInChildren<Light>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
            foreach (Light light in lights)
            {
                light.enabled = visible;
            }

            if (HideWisplightWisp.Value && !_wisplightHideReported)
            {
                _wisplightHideReported = true;
                Debug.Log($"Wisplight hidden on '{ball.name}': {renderers.Length} renderer(s), "
                    + $"{lights.Length} light(s) disabled");
            }
        }

        private static bool _wisplightHideReported;
        // Non-public in the shipped assembly despite the publicized reference declaring it public.
        // The audit caught this one on the deploy gate rather than in the build.
        private static readonly FieldInfo BallInstanceField =
            AccessTools.Field(typeof(SE_Demister), "m_ballInstance");

        // Non-public in the shipped assembly, which only affects calling it
        [HarmonyPatch(typeof(Demister), "Awake")]
        class Demister_Awake_Patch
        {
            static void Postfix(Demister __instance)
            {
                ApplyWisplightRange(__instance);
            }
        }

        // ---------------- On-screen clock ----------------
        // Drawn with IMGUI rather than by adding a label to Valheim's HUD hierarchy, because other
        // mods here rearrange that hierarchy (ExtraSlots resizes the inventory panel, ImprovedBuildHud
        // rebuilds the piece info) and a transform added into it is easy to lose or fight over.
        // The text is plain: no rich-text markup, so nothing can leak a colour tag as visible glyphs.
        private void OnGUI()
        {
            if (Player.m_localPlayer == null || Hud.IsUserHidden())
            {
                return;
            }
            DrawHudLine();
            DrawMapCursorCoordinates();
            DrawMapSeed();
        }

        private static Font _hudFont;
        private static string _hudFontSource;
        private static bool _hudFontReported;

        /// <summary>
        /// Finds the game's own font among those already loaded.
        ///
        /// FindObjectsOfTypeAll reaches assets that are loaded but not attached to an active object,
        /// which is what a UI font is. Matching is by substring so "Norse" also finds a bold or
        /// otherwise suffixed variant. The result is cached because the search walks every loaded
        /// font, and is redone only when the configured name changes.
        /// </summary>
        private static Font ResolveHudFont()
        {
            string wanted = (HudFontName.Value ?? string.Empty).Trim();
            if (_hudFontSource == wanted)
            {
                return _hudFont;
            }
            _hudFontSource = wanted;
            _hudFont = null;
            _hudFontReported = false;
            if (wanted.Length == 0)
            {
                return null;
            }
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            // Exact before partial, so naming a specific variant picks it rather than whichever
            // partial match is enumerated first - "Norse" otherwise lands on Norsebold
            foreach (Font font in fonts)
            {
                if (font != null && string.Equals(font.name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    _hudFont = font;
                    Debug.Log($"HUD font: using '{font.name}' (exact match)");
                    return _hudFont;
                }
            }
            foreach (Font font in fonts)
            {
                if (font != null && font.name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _hudFont = font;
                    Debug.Log($"HUD font: using '{font.name}' for '{wanted}' (partial match)");
                    return _hudFont;
                }
            }
            if (!_hudFontReported)
            {
                // Only on a miss, so the name can be corrected without a separate diagnostic
                _hudFontReported = true;
                var names = fonts.Where(f => f != null).Select(f => f.name).Distinct().OrderBy(n => n).ToArray();
                Debug.LogWarning($"HUD font '{wanted}' not found, using the default. Loaded fonts: "
                    + (names.Length > 0 ? string.Join(", ", names) : "none"));
            }
            return null;
        }

        /// <summary>
        /// Built once and rebuilt only when the size or font changes, since OnGUI runs several times
        /// a frame and a GUIStyle per call would be wasteful.
        /// </summary>
        private static GUIStyle HudStyle()
        {
            Font font = ResolveHudFont();
            if (_clockStyle == null || _clockStyle.fontSize != ClockFontSize.Value || _clockStyle.font != font)
            {
                _clockStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = ClockFontSize.Value,
                    font = font,
                };
                _clockStyle.normal.textColor = Color.white;
            }
            return _clockStyle;
        }

        private static void DrawHudLine()
        {
            if (!ShowClock.Value && !ShowCoordinates.Value)
            {
                return;
            }
            string line = BuildHudLine();
            if (string.IsNullOrEmpty(line))
            {
                return;
            }
            // Wide enough for clock and position together; the label is left-aligned so extra
            // width simply goes unused when only one of them is on
            var area = new Rect(ClockPositionX.Value, ClockPositionY.Value, 900f, ClockFontSize.Value * 2f);
            GUI.Label(area, line, HudStyle());
        }

        /// <summary>
        /// Shows where the mouse is pointing on the open map, in world coordinates.
        ///
        /// Minimap.ScreenToWorldPoint is the game's own conversion, which means this stays correct
        /// however the map is panned or zoomed - no reimplementation of the projection to drift out
        /// of step with it. It is private in the shipped assembly, so it comes through AccessTools.
        /// </summary>
        /// <summary>
        /// Screen rect of the large map image, in GUI coordinates.
        ///
        /// GetWorldCorners gives the corners in world space, which for a Screen Space - Overlay
        /// canvas is already in pixels measured from the bottom-left. GUI rects are measured from
        /// the top-left, so the Y axis is flipped here. Corner 0 is bottom-left and corner 2 is
        /// top-right.
        /// </summary>
        private static bool TryGetMapScreenRect(Minimap map, out Rect rect)
        {
            rect = default;
            // Reached through the GameObject rather than m_mapImageLarge, whose RawImage type
            // would make UnityEngine.UI a project reference for one property
            GameObject panel = map.m_mapLarge != null ? map.m_mapLarge : map.m_largeRoot;
            RectTransform transform = panel != null ? panel.transform as RectTransform : null;
            if (transform == null)
            {
                return false;
            }
            transform.GetWorldCorners(_mapCorners);
            Vector3 bottomLeft = _mapCorners[0];
            Vector3 topRight = _mapCorners[2];
            float width = topRight.x - bottomLeft.x;
            float height = topRight.y - bottomLeft.y;
            if (width <= 0f || height <= 0f)
            {
                return false;
            }
            rect = new Rect(bottomLeft.x, Screen.height - topRight.y, width, height);
            return true;
        }

        private static readonly Vector3[] _mapCorners = new Vector3[4];

        /// <summary>
        /// Right-aligned twin of the HUD style, so the seed can be pinned to the right edge of the
        /// map panel without having to measure the string first.
        /// </summary>
        private static GUIStyle _seedStyle;

        private static GUIStyle MapSeedStyle()
        {
            GUIStyle baseStyle = HudStyle();
            if (_seedStyle == null || _seedStyle.fontSize != baseStyle.fontSize || _seedStyle.font != baseStyle.font)
            {
                _seedStyle = new GUIStyle(baseStyle)
                {
                    alignment = TextAnchor.UpperRight,
                };
                _seedStyle.normal.textColor = baseStyle.normal.textColor;
            }
            return _seedStyle;
        }

        private static void DrawMapSeed()
        {
            if (!ShowMapSeed.Value || !Minimap.IsOpen())
            {
                return;
            }
            Minimap map = Minimap.instance;
            World world = ZNet.World;
            if (map == null || world == null)
            {
                return;
            }
            // The seed the player typed, falling back to the number it hashes to for a world that
            // was created with an empty seed field
            string seed = !string.IsNullOrEmpty(world.m_seedName)
                ? world.m_seedName
                : world.m_seed.ToString();

            // Anchored to the map panel for the same reason as the cursor readout: the panel is a
            // letterboxed strip on an ultrawide, and a screen-relative label lands outside it
            if (!TryGetMapScreenRect(map, out Rect panel))
            {
                return;
            }
            GUIStyle style = MapSeedStyle();
            var content = new GUIContent($"Seed   {seed}");

            // Sized to the text rather than given a wide rect. As a label the extra width was
            // harmless, but it becomes the clickable area the moment this is a button, and a
            // 900px strip reaching back across the map would swallow clicks meant for the map.
            Vector2 size = style.CalcSize(content);
            var area = new Rect(
                panel.xMax - MapSeedPositionX.Value - size.x,
                panel.y + MapSeedPositionY.Value,
                size.x,
                size.y);

            if (!MapSeedClickToCopy.Value)
            {
                GUI.Label(area, content, style);
                return;
            }

            // A button drawn with the label style, so it looks identical - GUI.skin.label has no
            // background in any state, which is what makes this read as text rather than a control
            if (GUI.Button(area, content, style))
            {
                CopySeedToClipboard(seed);
            }
        }

        /// <summary>
        /// Unity keeps a bridge to the OS clipboard in GUIUtility.systemCopyBuffer, so this needs
        /// no platform code and no extra assembly - IMGUI is already referenced for the HUD.
        ///
        /// Only the seed itself is copied, not the "Seed" prefix, since the point is to paste it
        /// straight into a map generator.
        /// </summary>
        private static void CopySeedToClipboard(string seed)
        {
            try
            {
                GUIUtility.systemCopyBuffer = seed;
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, $"Copied seed '{seed}' to the clipboard");
            }
            catch (Exception ex)
            {
                // Clipboard access can fail outright on some systems; a copy that does not work is
                // not worth taking the frame down for
                Debug.LogWarning($"Could not copy the seed to the clipboard - {ex.Message}");
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "Could not copy the seed - see the log");
            }
        }

        private static void DrawMapCursorCoordinates()
        {
            if (!ShowMapCursorCoordinates.Value || !Minimap.IsOpen() || ScreenToWorldPointMethod == null)
            {
                return;
            }
            Minimap map = Minimap.instance;
            if (map == null)
            {
                return;
            }
            // Input.mousePosition is measured from the bottom-left, which is what the game's own
            // conversion expects; GUI rects are measured from the top-left, hence the two systems
            // sitting side by side here
            object world = ScreenToWorldPointMethod.Invoke(map, new object[] { Input.mousePosition });
            if (!(world is Vector3 point))
            {
                return;
            }
            // Anchored to the map panel rather than the screen, because the panel does not fill the
            // window - on an ultrawide it is a letterboxed strip, and a screen-relative label ends
            // up out in the surrounding HUD next to the clock
            if (!TryGetMapScreenRect(map, out Rect panel))
            {
                return;
            }
            var area = new Rect(
                panel.x + MapCursorPositionX.Value,
                panel.y + MapCursorPositionY.Value,
                900f,
                ClockFontSize.Value * 2f);
            // Labelled Y rather than Z: the game calls the north-south axis Z internally, but every
            // coordinate the player sees - including the clock line - calls it Y
            GUI.Label(area, $"Cursor   X {point.x:0}   Y {point.z:0}", HudStyle());
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
                ? $"Day {env.GetDay()}   {time}   {DayPhaseName(hour)}"
                : $"{time}   {DayPhaseName(hour)}";
        }

        // Six four-hour bands keyed off the displayed clock hour. This deliberately does not
        // follow EnvMan's day/night thresholds - those only know daytime from night, which is too
        // coarse to be worth reading - so the label describes the time on the clock rather than the
        // lighting state, and "Night" here will not line up exactly with when it gets dark.
        private static readonly string[] DayPhases =
        {
            "Late Night",     // 00:00 - 03:59
            "Early Morning",  // 04:00 - 07:59
            "Morning",        // 08:00 - 11:59
            "Afternoon",      // 12:00 - 15:59
            "Evening",        // 16:00 - 19:59
            "Night",          // 20:00 - 23:59
        };

        private static string DayPhaseName(int hour)
        {
            return DayPhases[Mathf.Clamp(hour, 0, 23) / 4];
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
        /// Where the dump files are written: the BepInEx root, alongside LogOutput.log, rather than
        /// the config folder they used to share with the .cfg files. They are diagnostic output and
        /// belong with the log, and BepInEx rewrites the config directory on every launch, which is
        /// no place to leave files it does not own.
        ///
        /// Falls back to the config folder if the root is somehow unavailable, so a dump still goes
        /// somewhere rather than throwing.
        /// </summary>
        private static string DumpOutputDirectory =>
            string.IsNullOrEmpty(Paths.BepInExRootPath) ? Paths.ConfigPath : Paths.BepInExRootPath;

        /// <summary>
        /// Writes every item prefab known to ObjectDB out to two files in the BepInEx folder:
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

            string dir = DumpOutputDirectory;

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

            int locations = DumpLocationList(dir);
            int prefabs = DumpPrefabList(dir);
            int nearby = DumpNearbyObjects(dir);
            int terrainOps = DumpTerrainOps(dir);
            int mapPins = DumpMapPins(dir);
            int spawnEntries = DumpSpawnEntries(dir);
            int effectAreas = DumpEffectAreas(dir);

            Debug.Log($"Dumped {rows.Count - skipped} items ({skipped} filtered out, {eligible.Count} auto-pickup), "
                + $"{locations} locations, {prefabs} prefabs, {terrainOps} terrain ops, {mapPins} map pins, "
                + $"{spawnEntries} spawn entries, {effectAreas} effect areas and {nearby} nearby objects to {dir}");
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                $"Dumped {rows.Count} items, {locations} locations, {prefabs} prefabs, {nearby} nearby to BepInEx/config");
        }

        /// <summary>
        /// Writes every location the world generator knows about. Unlike items, location prefab
        /// names appear nowhere in the managed assemblies - they live in Unity asset data - so
        /// there is no way to look up the name of a structure from outside the running game. That
        /// made every missing auto-pin a guessing game, or a wait until the player happened to walk
        /// past the thing again with diagnostics on. One keypress at any loaded world replaces both.
        ///
        /// PinExcludeNames and PinIncludeLocations both match on the prefab name in this file.
        /// m_interiorRadius is included because a location with no interior never reaches the pin
        /// hook on its own and has to be named in PinIncludeLocations to be pinned at all.
        /// </summary>
        /// <summary>
        /// Every prefab registered with ZNetScene - which is to say every object that can exist in
        /// the world, including the ore deposits, mineable rocks and roots that ResourcePinNames
        /// keys on.
        ///
        /// The item dump covers ObjectDB, which only holds things that can sit in an inventory, and
        /// the location dump covers whole structures. Neither lists a deposit you walk up to and
        /// mine, so the names in ResourcePinNames previously had to come from somewhere outside the
        /// game and be confirmed by going and standing next to one. A web source has already been
        /// wrong about prefab names in this project more than once.
        /// </summary>
        /// <summary>
        /// Every networked object within NearbyDumpRadius of the player, nearest first.
        ///
        /// The registry dump says which prefabs exist; this says which one you are standing on,
        /// which is the question that actually comes up. A resource that fails to pin logs nothing
        /// at all - TryMatchResource simply returns false and PinResource returns - so a wrong
        /// prefab name and a prefab none of the hooks can see are indistinguishable from the log.
        /// This separates them: if the object is listed here with its component, the name was wrong;
        /// if it is listed with no hookable component, the hooks are the problem.
        ///
        /// The scan is slow and would be the wrong thing to put on a per-frame path, but this runs
        /// once when a key is pressed, which is cheap enough to be worth the certainty. Sorted by
        /// distance here, so the unsorted overload is the right one to ask for.
        /// </summary>
        private const float NearbyDumpRadius = 25f;

        private static int DumpNearbyObjects(string dir)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return 0;
            }
            Vector3 origin = player.transform.position;
            var report = new StringBuilder();
            report.AppendLine($"Objects within {NearbyDumpRadius:0}m of X {origin.x:0.#} Z {origin.z:0.#}");
            report.AppendLine("Distance	PrefabName	HookableComponents");

            var found = new List<KeyValuePair<float, string>>();
            foreach (ZNetView view in UnityEngine.Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if (view == null)
                {
                    continue;
                }
                float distance = Vector3.Distance(origin, view.transform.position);
                if (distance > NearbyDumpRadius)
                {
                    continue;
                }
                // Exactly the component types that feed PinResource, so a blank column means the
                // object cannot currently be pinned no matter what ResourcePinNames says
                var hooks = new List<string>();
                if (view.GetComponent<Destructible>() != null) hooks.Add("Destructible");
                if (view.GetComponent<MineRock5>() != null) hooks.Add("MineRock5");
                if (view.GetComponent<MineRock>() != null) hooks.Add("MineRock");
                if (view.GetComponent<Pickable>() != null) hooks.Add("Pickable");
                if (view.GetComponent<Location>() != null) hooks.Add("Location");
                found.Add(new KeyValuePair<float, string>(distance,
                    $"{distance:0.#}	{StripClone(view.gameObject.name)}	{(hooks.Count > 0 ? string.Join("+", hooks) : "(none)")}"));
            }
            found.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var entry in found)
            {
                report.AppendLine(entry.Value);
            }
            File.WriteAllText(Path.Combine(dir, "PipsMod_NearbyObjects.tsv"), report.ToString());
            return found.Count;
        }

        /// <summary>
        /// Every terrain operation the hoe and cultivator can place, with the settings that decide
        /// what each one does to the ground.
        ///
        /// These are registered in ObjectDB rather than ZNetScene, so the prefab dump never showed
        /// them - which is why the hoe pieces appeared in that file as "mud_road" and "cultivate"
        /// while the ghosts the game actually builds are "mud_road_v2" and "cultivate_v2". This is
        /// the list that answers which operation levels and how wide it reaches.
        /// </summary>
        /// <summary>
        /// Every pin currently on the map, with the flags that decide whether it is drawn and
        /// whether it is written to the save.
        ///
        /// A pin that the log says was added but that is not visible has several possible fates and
        /// they are indistinguishable from the map itself: still present but filtered out by its
        /// icon type, present but never saved, or gone. Dumping the live list after a reload
        /// separates them - if it is here, it was saved and the problem is display; if it is not,
        /// it never survived the session.
        /// </summary>
        private static int DumpMapPins(string dir)
        {
            Minimap map = Minimap.instance;
            var pins = map != null ? PinsField?.GetValue(map) as List<Minimap.PinData> : null;
            if (pins == null)
            {
                return 0;
            }
            var visible = VisibleIconTypesField?.GetValue(map) as bool[];
            var table = new StringBuilder();
            table.AppendLine("Name	Type	IconShown	Saved	Checked	OwnerID	X	Z");
            int count = 0;
            foreach (Minimap.PinData pin in pins)
            {
                if (pin == null)
                {
                    continue;
                }
                int type = (int)pin.m_type;
                string shown = visible != null && type >= 0 && type < visible.Length
                    ? visible[type].ToString()
                    : "unknown";
                table.AppendLine($"{pin.m_name}	{pin.m_type}	{shown}	{pin.m_save}	{pin.m_checked}	"
                    + $"{pin.m_ownerID}	{pin.m_pos.x:0}	{pin.m_pos.z:0}");
                count++;
            }
            File.WriteAllText(Path.Combine(dir, "PipsMod_MapPins.tsv"), table.ToString());
            return count;
        }

        private static int DumpTerrainOps(string dir)
        {
            var ops = ObjectDB.instance?.m_terrainOps;
            if (ops == null)
            {
                return 0;
            }
            var table = new StringBuilder();
            table.AppendLine("Prefab	Level	LevelRadius	Square	LevelOffset	Raise	RaiseRadius	Smooth	SmoothRadius	SmoothPower	PaintRadius");
            int count = 0;
            foreach (TerrainOp op in ops)
            {
                var set = op != null ? op.m_settings : null;
                if (set == null)
                {
                    continue;
                }
                table.AppendLine($"{op.name}	{set.m_level}	{set.m_levelRadius:0.##}	{set.m_square}	{set.m_levelOffset:0.##}	"
                    + $"{set.m_raise}	{set.m_raiseRadius:0.##}	{set.m_smooth}	{set.m_smoothRadius:0.##}	"
                    + $"{set.m_smoothPower:0.##}	{set.m_paintRadius:0.##}");
                count++;
            }
            File.WriteAllText(Path.Combine(dir, "PipsMod_TerrainOps.tsv"), table.ToString());
            return count;
        }

        private static int DumpPrefabList(string dir)
        {
            var prefabs = ZNetScene.instance?.m_prefabs;
            if (prefabs == null)
            {
                return 0;
            }
            var names = prefabs
                .Where(prefab => prefab != null)
                .Select(prefab => prefab.name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            File.WriteAllText(Path.Combine(dir, "PipsMod_PrefabList.txt"), string.Join(Environment.NewLine, names));
            return names.Count;
        }

        private static int DumpLocationList(string dir)
        {
            var zoneLocations = ZoneSystem.instance?.m_locations;
            if (zoneLocations == null)
            {
                return 0;
            }
            var table = new StringBuilder();
            table.AppendLine("PrefabName	Name	Biome	Quantity	Unique	Group	InteriorRadius	ExteriorRadius	Enabled");
            int count = 0;
            foreach (ZoneSystem.ZoneLocation loc in zoneLocations)
            {
                if (loc == null)
                {
                    continue;
                }
                // m_prefabName is the authored id and is what the pin code compares against;
                // m_name is only a display label and is sometimes empty
                string prefabName = !string.IsNullOrEmpty(loc.m_prefabName) ? loc.m_prefabName : loc.m_name;
                if (string.IsNullOrEmpty(prefabName))
                {
                    continue;
                }
                table.AppendLine($"{prefabName}	{loc.m_name}	{loc.m_biome}	{loc.m_quantity}	{loc.m_unique}	"
                    + $"{loc.m_group}	{loc.m_interiorRadius:0.#}	{loc.m_exteriorRadius:0.#}	{loc.m_enable}");
                count++;
            }
            File.WriteAllText(Path.Combine(dir, "PipsMod_LocationList.tsv"), table.ToString());
            return count;
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
                GuardLoadPath("upgrade chance", () => ApplyUpgradeChance(__instance));
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

        private static readonly Dictionary<ItemDrop.ItemData.SharedData, float> _baseUpgradeChances =
            new Dictionary<ItemDrop.ItemData.SharedData, float>();
        private static float _appliedUpgradeChance = float.NaN;

        /// <summary>
        /// Sets the Forge of Potential success chance.
        ///
        /// InventoryGui.DoCrafting rolls it against the *upgrader resource*, not the station:
        ///
        ///     float roll = Random.Range(0f, 1f);
        ///     if (itemData2.m_shared.m_upgradeChance >= roll)            // success
        ///     else if (itemData2.m_shared.m_breakChance >= 1f - roll)    // item destroyed
        ///     else                                                       // level reduced
        ///
        /// so the value to change is on each upgrader item's SharedData. Only items that already
        /// have a chance are touched, which is what identifies an upgrader - everything else is
        /// left alone. At 1 the first branch always wins, so the break and downgrade outcomes
        /// become unreachable without having to touch m_breakChance as well.
        ///
        /// The original is remembered per SharedData so setting the config back to 0 restores it,
        /// rather than leaving whatever was last written baked in.
        /// </summary>
        private static void ApplyUpgradeChance(ObjectDB odb)
        {
            if (odb == null || odb.m_items == null)
            {
                return;
            }
            float configured = UpgradeSuccessChance.Value;
            _appliedUpgradeChance = configured;
            int changed = 0;
            foreach (var prefab in odb.m_items)
            {
                var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                var shared = drop?.m_itemData?.m_shared;
                if (shared == null)
                {
                    continue;
                }
                if (!_baseUpgradeChances.TryGetValue(shared, out float original))
                {
                    original = shared.m_upgradeChance;
                    // Only upgrader items carry a chance at all; anything at zero is not one
                    if (original <= 0f)
                    {
                        continue;
                    }
                    _baseUpgradeChances[shared] = original;
                }
                float target = configured > 0f ? configured : original;
                if (shared.m_upgradeChance != target)
                {
                    shared.m_upgradeChance = target;
                    changed++;
                }
            }
            if (changed > 0)
            {
                Debug.Log($"Upgrade success chance set to {(configured > 0f ? configured.ToString("0.##") : "vanilla")} on {changed} upgrader item(s)");
            }
        }

        private static void RefreshUpgradeChance()
        {
            if (UpgradeSuccessChance.Value == _appliedUpgradeChance)
            {
                return;
            }
            ObjectDB odb = ObjectDB.instance;
            if (odb != null)
            {
                ApplyUpgradeChance(odb);
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

        // GetFirstRequiredItem is the one requirement check whose count and fetch disagree. It
        // asks m_inventory.CountItems - which this mod widens to include nearby containers - and
        // then returns inventory.GetItem for the match, which only ever looks in the player's own
        // bag:
        //
        //     if (m_inventory.CountItems(name, j) >= num) {
        //         amount = num;
        //         return inventory.GetItem(name, j);     // null when the item is in a chest
        //     }
        //
        // Recipe.GetAmount then reads singleReqItem.m_quality with no null check, so the craft
        // button throws instead of crafting. It only bites recipes with m_requireOnlyOneIngredient,
        // where several interchangeable items each get their own requirement - the Food Prep Table
        // fish recipe is one. A fish sitting in a chest satisfies its requirement first, the fetch
        // returns null, and the click dies even though another fish is in the player's bag.
        //
        // The postfix finishes the job the widening started: when the bag cannot supply the match,
        // the same item is taken from a nearby container instead. Consumption already comes out of
        // containers via Player_ConsumeResources_Patch, so the returned item is honest about what
        // the craft will actually spend. The requirement walk mirrors vanilla's exactly, including
        // the upgrader filter and the quality loop, so the amount and extraAmount it reports match
        // what vanilla would have reported had the item been in the bag.
        [HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
        class Player_GetFirstRequiredItem_Patch
        {
            static void Prefix(out bool __state) { __state = BeginContainerScope(); }
            static void Finalizer(bool __state) { EndContainerScope(__state); }

            static void Postfix(Player __instance, Recipe recipe, int qualityLevel, ref int amount,
                ref int extraAmount, int craftMultiplier, ref ItemDrop.ItemData __result)
            {
                if (__result != null || !CraftFromContainers.Value || _nearbyInventories.Count == 0
                    || recipe == null || recipe.m_resources == null || __instance == null)
                {
                    return;
                }
                Inventory bag = __instance.GetInventory();
                if (bag == null)
                {
                    return;
                }
                CraftingStation station = __instance.GetCurrentCraftingStation();
                foreach (Piece.Requirement requirement in recipe.m_resources)
                {
                    // Same skip conditions as vanilla, so the same requirement is selected
                    if ((station != null && station.m_upgrader != requirement.m_upgraderResource)
                        || (station == null && requirement.m_upgraderResource)
                        || !requirement.m_resItem)
                    {
                        continue;
                    }
                    int needed = requirement.GetAmount(qualityLevel) * craftMultiplier;
                    string itemName = requirement.m_resItem.m_itemData.m_shared.m_name;
                    for (int quality = 0; quality <= requirement.m_resItem.m_itemData.m_shared.m_maxQuality; quality++)
                    {
                        int total = bag.CountItems(itemName, quality);
                        for (int i = 0; i < _nearbyInventories.Count; i++)
                        {
                            total += _nearbyInventories[i].CountItems(itemName, quality);
                        }
                        if (total < needed)
                        {
                            continue;
                        }
                        ItemDrop.ItemData found = bag.GetItem(itemName, quality);
                        for (int i = 0; found == null && i < _nearbyInventories.Count; i++)
                        {
                            found = _nearbyInventories[i].GetItem(itemName, quality);
                        }
                        if (found != null)
                        {
                            amount = needed;
                            extraAmount = requirement.m_extraAmountOnlyOneIngredient;
                            __result = found;
                            return;
                        }
                    }
                }
            }
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
                // Nothing is about to be consumed, so pulling materials out of chests would just
                // move them into the player's inventory and leave them there. Checked here rather
                // than relying on the free-build prefix returning false to suppress this one -
                // prefix ordering is not worth depending on when the two patches can simply agree.
                if (!CraftFromContainers.Value || requirements == null || FreeBuildActive)
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
        //
        // Scaling the fields is not enough on its own, because nothing refreshes the collider on a
        // schedule. CustomUpdate only advances m_updateExtensionTimer; the resize happens inside
        // GetExtensions, which is reached solely from HaveBuildStationInRange (the build HUD and
        // placement checks, so only with a hammer out), GetLevel/GetExtentionCount from the
        // crafting panel, and PokeInUse. So a freshly loaded station keeps the radius its prefab
        // was authored with until the player happens to build or craft.
        //
        // In vanilla that is harmless - the authored collider already matches the authored range,
        // so the refresh is a no-op. Scaling m_rangeBuild is what pulls the two apart, and the
        // collider is the one that matters beyond building: EffectArea.Type.PlayerBase is what
        // stops creatures spawning, SpawnSystem tests it every second from ten seconds after a zone
        // loads, and the test is a live Physics.OverlapSphere against this collider rather than a
        // comparison with any stored range. Left unsynced, a base relying on the widened radius is
        // only covered to vanilla size for the first stretch after every arrival.
        //
        // So the range is pushed into the collider here as well. Extensions are not attached yet at
        // Start, which is fine - this is the base range, and GetExtensions adds the per-level part
        // the first time anything queries the station.
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

                    // m_buildRange is deliberately left alone - it is non-public in the shipped
                    // assembly, and writing it would buy nothing: its only reader is
                    // GetStationBuildRange, which recomputes it through GetExtensions first.
                    // The collider is the part that has to be right before then.
                    float range = __instance.m_rangeBuild;

                    // Same two shapes vanilla handles, and no fallback for anything else: a
                    // collider type GetExtensions would not resize either is not ours to guess at
                    if (__instance.m_effectAreaCollider is SphereCollider sphere)
                    {
                        sphere.radius = range;
                    }
                    else if (__instance.m_effectAreaCollider is CapsuleCollider capsule)
                    {
                        capsule.radius = range;
                    }
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
                                            Piece piece, Plant plant, out Vector3 position)
        {
            position = centre + rotation * new Vector3(gx * spacing, 0f, gz * spacing);
            if (!Heightmap.GetHeight(position, out float groundHeight))
            {
                return false;
            }
            position.y = groundHeight;
            if (piece.m_cultivatedGroundOnly)
            {
                Heightmap heightmap = Heightmap.FindHeightmap(position);
                if (heightmap == null || !heightmap.IsCultivated(position))
                {
                    return false;
                }
            }
            return HasGrowSpaceAt(position, plant.m_growRadius);
        }

        private static void ClearPlantPreviews()
        {
            for (int i = 0; i < _plantPreviews.Count; i++)
            {
                if (_plantPreviews[i] != null)
                {
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
        private static GameObject ClonePlacementGhost(GameObject ghost)
        {
            bool previousInit = ZNetView.m_forceDisableInit;
            bool previousTerrain = TerrainOp.m_forceDisableTerrainOps;
            ZNetView.m_forceDisableInit = true;
            TerrainOp.m_forceDisableTerrainOps = true;
            try
            {
                GameObject clone = UnityEngine.Object.Instantiate(ghost);
                clone.name = ghost.name + "_PipsMassPlantPreview";

                // Plant derives from SlowUpdate, whose Awake registers every instance into a
                // static list that SlowUpdater walks - and SUpdate is the only thing that ever
                // grows a plant. Leaving the Plant component on a preview would put two dozen
                // throwaway objects into that registry and churn its index-based swap-removal
                // every time the previews are rebuilt. A preview only has to look like a plant,
                // so the component goes immediately, deregistering it in the same breath.
                Plant previewPlant = clone.GetComponent<Plant>();
                if (previewPlant != null)
                {
                    UnityEngine.Object.DestroyImmediate(previewPlant);
                }
                return clone;
            }
            finally
            {
                ZNetView.m_forceDisableInit = previousInit;
                TerrainOp.m_forceDisableTerrainOps = previousTerrain;
            }
        }

        private static void UpdateMassPlantPreview()
        {
            Player player = Player.m_localPlayer;
            if (player == null || MassPlantGridSize.Value <= 1 || !Input.GetKey(MassPlantKey.Value))
            {
                ClearPlantPreviews();
                return;
            }
            GameObject ghost = PlayerPlacementGhostField?.GetValue(player) as GameObject;
            if (ghost == null || !ghost.activeSelf)
            {
                ClearPlantPreviews();
                return;
            }
            Piece piece = ghost.GetComponent<Piece>();
            Plant plant = ghost.GetComponent<Plant>();
            if (piece == null || plant == null)
            {
                ClearPlantPreviews();
                return;
            }

            int needed = MassPlantGridSize.Value * MassPlantGridSize.Value - 1;
            if (_previewPieceName != ghost.name || _plantPreviews.Count != needed)
            {
                ClearPlantPreviews();
                _previewPieceName = ghost.name;
                for (int i = 0; i < needed; i++)
                {
                    _plantPreviews.Add(ClonePlacementGhost(ghost));
                }
            }

            Vector3 centre = ghost.transform.position;
            Quaternion rotation = ghost.transform.rotation;
            float spacing = Mathf.Max(0.1f, plant.m_growRadius * MassPlantSpacing.Value);
            int half = MassPlantGridSize.Value / 2;
            int index = 0;
            for (int gx = -half; gx <= half; gx++)
            {
                for (int gz = -half; gz <= half; gz++)
                {
                    if (gx == 0 && gz == 0)
                    {
                        continue;   // the real ghost already stands here
                    }
                    if (index >= _plantPreviews.Count)
                    {
                        return;
                    }
                    GameObject preview = _plantPreviews[index++];
                    if (preview == null)
                    {
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
            static void Postfix(Player __instance, Piece piece, bool __result)
            {
                if (!__result || piece == null || MassPlantGridSize.Value <= 1)
                {
                    return;
                }
                if (!ReferenceEquals(__instance, Player.m_localPlayer) || !Input.GetKey(MassPlantKey.Value))
                {
                    return;
                }
                Plant plant = piece.GetComponent<Plant>();
                if (plant != null)
                {
                    MassPlant(__instance, piece, plant);
                }
            }
        }

        private static bool PlayerHasFreeBuild(Player player)
        {
            return PlayerNoPlacementCostField != null
                && (bool)PlayerNoPlacementCostField.GetValue(player);
        }

        /// <summary>
        /// Replicates Plant.HaveGrowSpace for a point that has no Plant on it yet. Any non-plant
        /// collider inside the radius blocks, as does any healthy neighbouring plant - matching the
        /// game's own rule, so anything this accepts would also satisfy the plant once placed.
        /// </summary>
        private static bool HasGrowSpaceAt(Vector3 position, float growRadius)
        {
            if (_plantSpaceMask == 0)
            {
                _plantSpaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
            }
            int hits = Physics.OverlapSphereNonAlloc(position, growRadius, _plantColliders, _plantSpaceMask);
            for (int i = 0; i < hits; i++)
            {
                Plant other = _plantColliders[i].GetComponent<Plant>();
                if (other == null || other.GetStatus() == Plant.Status.Healthy)
                {
                    return false;
                }
            }
            return true;
        }

        private static void MassPlant(Player player, Piece piece, Plant plant)
        {
            GameObject ghost = PlayerPlacementGhostField?.GetValue(player) as GameObject;
            if (ghost == null)
            {
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
            for (int gx = -half; gx <= half; gx++)
            {
                for (int gz = -half; gz <= half; gz++)
                {
                    if (gx == 0 && gz == 0)
                    {
                        continue;   // vanilla already placed the centre
                    }
                    // Same cell logic the preview uses, so the red squares are exactly the ones skipped
                    if (!TryGetPlantCell(centre, rotation, spacing, gx, gz, piece, plant, out Vector3 position))
                    {
                        continue;
                    }
                    // Checked per seed so the batch stops cleanly when the last one is used
                    if (!freeBuild && !player.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                    {
                        break;
                    }

                    player.PlacePiece(piece, position, rotation, doAttack: false);
                    if (!freeBuild)
                    {
                        player.ConsumeResources(piece.m_resources, 0);
                    }
                    planted++;
                }
            }

            if (planted > 0 && _messageHud != null)
            {
                _messageHud.ShowMessage(MessageHud.MessageType.TopLeft, $"Planted {planted + 1}");
            }

            // Grow time is serialised per prefab and picked per plant by lerping between these two
            // with a seed, so the real window is only visible at runtime. Reported in days as well
            // as seconds because a Valheim day is m_dayLengthSec, not 24 hours of anything.
            if (planted > 0)
            {
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
        // Deliberately not persisted: the floor should be on after a relaunch, since the reason
        // to switch it off - dying on purpose - is a short, specific situation, and forgetting it
        // was off is how a real death happens by accident.
        private static bool _healthFloorEnabled = true;

        /// <summary>
        /// Whether the damage floor applies at all. Both the prefix that clamps a hit and the
        /// postfix that checks the result read this, so the hotkey and the config cannot disagree
        /// about whether the gate is live.
        /// </summary>
        private static bool HealthFloorActive => _healthFloorEnabled && MinHealthPercent.Value > 0f;

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
        private static void RefreshStackSizeMultiplier()
        {
            if (SoloOnlyRate(CustomStackSizeMultiplier) == _appliedStackMultiplier)
            {
                return;
            }
            ObjectDB odb = ObjectDB.instance;
            if (odb != null)
            {
                ApplyStackSizeMultiplier(odb);
            }
        }

        private static bool IsSharedSession()
        {
            if (!WorldEffectsSoloOnly.Value)
            {
                return false;
            }
            float now = Time.realtimeSinceStartup;
            if (now - _sharedSessionCheckedAt < SharedSessionCacheSeconds)
            {
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
        private static float SoloOnlyRate(ConfigEntry<float> setting)
        {
            float value = setting.Value;
            return (value > 0f && value != 1f && !IsSharedSession()) ? value : 1f;
        }

        /// <summary>
        /// The resource rate is a stored global rather than a value read at point of use, so it has
        /// to be re-asserted as the session changes - otherwise a boosted rate would linger after
        /// someone joined. _vanillaResourceRate holds whatever the world itself last set.
        /// </summary>
        private static void ApplyResourceRate()
        {
            float multiplier = SoloOnlyRate(CustomResourceRate);
            Game.m_resourceRate = multiplier != 1f ? multiplier : _vanillaResourceRate;
        }

        // ---------------- Death penalty negation ----------------
        // Vanilla splits the death cost across two places, so both have to be intercepted.
        //
        // Items: Player.CreateTombStone spawns the grave and calls MoveInventoryToGrave. The world
        // modifier keys only steer *which* items go in - DeathKeepEquip merely skips the
        // UnequipAllItems call, and MoveInventoryToGrave then passes over anything still flagged
        // m_equipped. Nothing short of DeathKeepInventory keeps unequipped items, which is why the
        // Casual preset still empties a backpack. Skipping CreateTombStone outright keeps
        // everything and leaves no grave to walk back to.
        //
        // Skills: Player.OnDeath calls m_skills.Clear() when the world sets DeathSkillsReset
        // (hardcore) and m_skills.OnDeath() otherwise, and only when HardDeath() is true - dying
        // twice inside m_hardDeathCooldown is already free in vanilla. Skills.OnDeath is blocked
        // directly. Skills.Clear is blocked only while a death is in progress, because it is also
        // the legitimate path for wiping a character, and a blanket prefix there would be a real
        // bug waiting to happen.
        private static bool _inPlayerDeath;

        private static bool DeathPenaltyNegated(Character character)
        {
            return NegateDeathPenalty.Value
                && ReferenceEquals(character, Player.m_localPlayer)
                && !IsSharedSession();
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        class Player_OnDeath_Patch
        {
            static void Prefix(Player __instance)
            {
                _inPlayerDeath = ReferenceEquals(__instance, Player.m_localPlayer);
            }

            // Finalizer rather than Postfix so the flag cannot be left set by an exception midway
            // through the death sequence, which would silently disable Skills.Clear from then on
            static void Finalizer()
            {
                _inPlayerDeath = false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
        class Player_CreateTombStone_Patch
        {
            static bool Prefix(Player __instance)
            {
                if (!DeathPenaltyNegated(__instance))
                {
                    return true;
                }
                Debug.Log("Death penalty negated: inventory kept, no tombstone created");
                return false;
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
        class Skills_OnDeath_Patch
        {
            static bool Prefix(Skills __instance)
            {
                if (SkillsPlayerField == null)
                {
                    return true;
                }
                if (!DeathPenaltyNegated(SkillsPlayerField.GetValue(__instance) as Character))
                {
                    return true;
                }
                Debug.Log("Death penalty negated: skill levels kept");
                return false;
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.Clear))]
        class Skills_Clear_Patch
        {
            static bool Prefix(Skills __instance)
            {
                // Only the death-time reset is suppressed; wiping a character by any other route
                // still works
                if (!_inPlayerDeath || SkillsPlayerField == null)
                {
                    return true;
                }
                if (!DeathPenaltyNegated(SkillsPlayerField.GetValue(__instance) as Character))
                {
                    return true;
                }
                Debug.Log("Death penalty negated: skill reset suppressed");
                return false;
            }
        }

        // ---------------- Secret achievement reveal ----------------
        // m_isSecret gates three things, all display, and all of them read the flag live:
        //
        //   InventoryGui.UpdateAchievementsList  - substitutes "$inventory_achievement_secret" for
        //                                          the name and description, and destroys the tile
        //                                          Button so it cannot even be opened
        //   AchievementsGui.OnOpenAchievementDetails - early-returns after one "???" row
        //
        // Rather than patch each display site, the flag is cleared for the duration of the call and
        // put back afterwards. The restore matters: m_isSecret lives on a shared ScriptableObject
        // asset, so leaving it cleared would be a persistent edit to game data for the rest of the
        // session, and the config toggle would stop taking effect until a restart. Finalizer rather
        // than Postfix so the flag is restored even if the vanilla method throws.
        //
        // Two patches are needed because the tile's click handler captures `clickable` when the
        // list is built but calls OnOpenAchievementDetails later, long after the flag went back.
        private static readonly List<Achievement> _unhiddenAchievements = new List<Achievement>();

        private static void UnhideSecretAchievements()
        {
            _unhiddenAchievements.Clear();
            Achievements achievements = Achievements.m_instance;
            if (achievements == null || achievements.m_achievementLists == null)
            {
                return;
            }
            foreach (AchievementList list in achievements.m_achievementLists)
            {
                if (list?.m_achievements == null)
                {
                    continue;
                }
                foreach (Achievement achievement in list.m_achievements)
                {
                    if (achievement != null && achievement.m_isSecret)
                    {
                        achievement.m_isSecret = false;
                        _unhiddenAchievements.Add(achievement);
                    }
                }
            }
        }

        private static void RestoreSecretAchievements()
        {
            for (int i = 0; i < _unhiddenAchievements.Count; i++)
            {
                if (_unhiddenAchievements[i] != null)
                {
                    _unhiddenAchievements[i].m_isSecret = true;
                }
            }
            _unhiddenAchievements.Clear();
        }

        // Non-public in the shipped assembly, so it is patched by name - Harmony can patch a
        // non-public method even though this mod could not call one
        [HarmonyPatch(typeof(InventoryGui), "UpdateAchievementsList")]
        class InventoryGui_UpdateAchievementsList_Patch
        {
            static void Prefix()
            {
                if (RevealSecretAchievements.Value)
                {
                    UnhideSecretAchievements();
                }
            }

            static void Finalizer()
            {
                RestoreSecretAchievements();
            }
        }

        [HarmonyPatch(typeof(AchievementsGui), nameof(AchievementsGui.OnOpenAchievementDetails))]
        class AchievementsGui_OnOpenAchievementDetails_Patch
        {
            static void Prefix(Achievement achievement, ref bool __state)
            {
                __state = false;
                if (!RevealSecretAchievements.Value || achievement == null || !achievement.m_isSecret)
                {
                    return;
                }
                achievement.m_isSecret = false;
                __state = true;
            }

            static void Finalizer(Achievement achievement, bool __state)
            {
                if (__state && achievement != null)
                {
                    achievement.m_isSecret = true;
                }
            }
        }

        // ---------------- Achievement progress display ----------------
        // AchievementsGui.CreateStatRow hides every requirement that is not already met:
        //
        //     if (currentAmount >= totalAmount) { ...real numbers, green... }
        //     else { StatName.text = "???"; Progress.text = "??? / ???"; }
        //
        // So the detail panel only becomes informative once there is nothing left to track. Every
        // requirement type funnels through this one method - PopulateEnemyStats and
        // PopulateDetailPanel both call it - so filling the row back in here covers all of them.
        //
        // The colour is deliberately left gray. Vanilla uses green for met and gray for unmet, and
        // that is still worth reading at a glance; only the text is restored.
        //
        // Two reflection details, both forced rather than chosen. m_achievementDetailsListRoot is
        // private in the shipped assembly even though the publicized reference shows it public, so
        // it needs an AccessTools handle. StatName and Progress are TextMeshProUGUI, which lives in
        // an assembly this project does not reference, so they are read as object and their text
        // set through a cached PropertyInfo instead of adding a dependency for two strings.
        private static readonly FieldInfo _detailsListRootField =
            AccessTools.Field(typeof(AchievementsGui), "m_achievementDetailsListRoot");
        private static PropertyInfo _statNameProperty;
        private static PropertyInfo _progressProperty;
        private static PropertyInfo _tmpTextProperty;

        private static void SetRowText(object label, string value)
        {
            if (label == null)
            {
                return;
            }
            if (_tmpTextProperty == null || !_tmpTextProperty.DeclaringType.IsInstanceOfType(label))
            {
                _tmpTextProperty = AccessTools.Property(label.GetType(), "text");
            }
            _tmpTextProperty?.SetValue(label, value, null);
        }

        [HarmonyPatch(typeof(AchievementsGui), nameof(AchievementsGui.CreateStatRow))]
        class AchievementsGui_CreateStatRow_Patch
        {
            static void Postfix(AchievementsGui __instance, string statKey, float currentAmount, float totalAmount)
            {
                // A met requirement already shows its numbers
                if (!ShowAchievementProgress.Value || currentAmount >= totalAmount)
                {
                    return;
                }
                Transform root = _detailsListRootField?.GetValue(__instance) as Transform;
                if (root == null || root.childCount == 0)
                {
                    return;
                }
                // CreateStatRow instantiates the row as the last child immediately before this runs
                var row = root.GetChild(root.childCount - 1).GetComponent<AchievementDetailUnlockCondition>();
                if (row == null)
                {
                    return;
                }
                if (_statNameProperty == null)
                {
                    _statNameProperty = AccessTools.Property(typeof(AchievementDetailUnlockCondition), "StatName");
                    _progressProperty = AccessTools.Property(typeof(AchievementDetailUnlockCondition), "Progress");
                }
                if (_statNameProperty == null || _progressProperty == null)
                {
                    return;
                }
                // Vanilla reassigns its own statKey parameter before branching, so it may already
                // carry the prefix by the time a postfix sees it
                string key = statKey;
                if (!string.IsNullOrEmpty(key) && key[0] != '$' && Enum.TryParse(key, out PlayerStatType _))
                {
                    key = "$stat_" + key;
                }
                string label = Localization.instance.Localize(key);
                SetRowText(_statNameProperty.GetValue(row, null), string.IsNullOrEmpty(label) ? statKey : label);
                SetRowText(_progressProperty.GetValue(row, null), $"{currentAmount:0.##} / {totalAmount:0.##}");
            }
        }

        // ---------------- Automatic dungeon map pins ----------------
        // DungeonGenerator.Spawn is the moment a dungeon interior is actually placed, which only
        // happens once its zone loads - that is to say, once the player is close enough. So the
        // hook already means "discovered by proximity" without any distance check of our own, and
        // it fires for every dungeon type: crypts, burial chambers, frost caves, mines.
        //
        // The generator component sits inside the placed location, so its transform is the dungeon
        // itself. Pins only care about X and Z, so the vertical offset between the generator and
        // the surface entrance does not matter.
        //
        // Spawn, AddPin and m_pins are all non-public in the shipped assembly even though the
        // publicized reference shows them public. Spawn is only *patched*, which is always allowed;
        // the other two are read or invoked, so they go through AccessTools handles.
        //
        // Duplicate suppression deliberately does not use Minimap.GetClosestPin - see
        // FindBlockingPin for why comparing distance alone was wrong here.
        private static readonly MethodInfo AddPinMethod = AccessTools.Method(typeof(Minimap), "AddPin");
        private static readonly FieldInfo PinsField = AccessTools.Field(typeof(Minimap), "m_pins");
        // Non-public in the shipped assembly even though the publicized reference declares it
        // "public bool[]" - the access audit caught this one before it shipped
        private static readonly FieldInfo VisibleIconTypesField =
            AccessTools.Field(typeof(Minimap), "m_visibleIconTypes");

        /// <summary>
        /// Calls Minimap.AddPin without referencing Splatform. Its last parameter is a
        /// PlatformUserID with a default value, and the compiler has to materialise that default at
        /// the call site, which drags in an assembly this project otherwise has no use for. Going
        /// through reflection and filling the trailing optional parameters here avoids adding a
        /// dependency for one call, and survives Iron Gate appending further optional parameters.
        /// </summary>
        private static Minimap.PinData AddMapPinAndReturn(Minimap map, Vector3 pos, Minimap.PinType type,
            string name, bool save, bool isChecked)
        {
            // Minimap.AddPin returns the PinData it created, which is the handle needed to see
            // whether UpdatePins ever builds a marker for it
            return AddMapPinCore(map, pos, type, name, save, isChecked) as Minimap.PinData;
        }

        private static bool AddMapPin(Minimap map, Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked)
        {
            return AddMapPinCore(map, pos, type, name, save, isChecked) != null;
        }

        private static object AddMapPinCore(Minimap map, Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked)
        {
            if (AddPinMethod == null)
            {
                return null;
            }
            ParameterInfo[] parameters = AddPinMethod.GetParameters();
            if (parameters.Length < 5)
            {
                return null;
            }
            object[] args = new object[parameters.Length];
            args[0] = pos;
            args[1] = type;
            args[2] = name;
            args[3] = save;
            args[4] = isChecked;
            for (int i = 5; i < parameters.Length; i++)
            {
                object fallback = parameters[i].ParameterType.IsValueType
                    ? Activator.CreateInstance(parameters[i].ParameterType)
                    : null;
                // A struct default compiles to no usable constant, so DefaultValue comes back null
                // and the zeroed struct is the right stand-in
                args[i] = parameters[i].HasDefaultValue && parameters[i].DefaultValue != null
                    ? parameters[i].DefaultValue
                    : fallback;
            }
            // Minimap.AddPin un-hides the icon type it is adding:
            //
            //     if ((int)type < m_visibleIconTypes.Length && !m_visibleIconTypes[(int)type])
            //         ToggleIconFilter(type);
            //
            // That is reasonable for a pin the player just placed by hand, but an automatic pin
            // turning the whole category back on undoes the filtering the player set up to keep the
            // map readable while exploring - and it happens again with every new discovery.
            //
            // Marking the type visible for the duration of the call is what stops it, rather than
            // calling ToggleIconFilter a second time to flip it back: the toggle also plays a
            // gamepad rumble and recolours the legend, so undoing it that way would be audible and
            // visible. This way the branch is simply never taken and the array ends up exactly as
            // the player left it. AddPin sets m_pinUpdateRequired itself, and the redraw already
            // skips hidden types, so the new pin stays hidden until the player unhides it.
            // Reflection hands back the array itself, so the elements can be set in place
            bool[] filters = AutoPinKeepIconFilter.Value
                ? VisibleIconTypesField?.GetValue(map) as bool[]
                : null;
            int filterIndex = (int)type;
            bool rehide = filters != null && filterIndex >= 0 && filterIndex < filters.Length
                && !filters[filterIndex];
            if (rehide)
            {
                filters[filterIndex] = true;
            }
            try
            {
                return AddPinMethod.Invoke(map, args);
            }
            finally
            {
                if (rehide)
                {
                    filters[filterIndex] = false;
                }
            }
        }

        private static Dictionary<string, string> ParsePairs(string raw)
        {
            var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pair in (raw ?? string.Empty).Split(','))
            {
                int split = pair.IndexOf('=');
                if (split <= 0)
                {
                    continue;
                }
                string key = pair.Substring(0, split).Trim();
                string value = pair.Substring(split + 1).Trim();
                if (key.Length > 0 && value.Length > 0)
                {
                    parsed[key] = value;
                }
            }
            return parsed;
        }

        private static Dictionary<string, string> _dungeonNameOverrides;
        private static string _dungeonNameSource;
        private static Dictionary<string, string> _dungeonIconOverrides;
        private static string _dungeonIconSource;

        // Both are parsed lazily and rebuilt whenever their config string changes, so edits to the
        // cfg apply without a restart
        private static Dictionary<string, string> DungeonNameOverrides()
        {
            string raw = DungeonPinNameOverrides.Value ?? string.Empty;
            if (_dungeonNameOverrides == null || _dungeonNameSource != raw)
            {
                _dungeonNameSource = raw;
                _dungeonNameOverrides = ParsePairs(raw);
            }
            return _dungeonNameOverrides;
        }

        private static Dictionary<string, string> DungeonIconOverrides()
        {
            string raw = DungeonPinIconOverrides.Value ?? string.Empty;
            if (_dungeonIconOverrides == null || _dungeonIconSource != raw)
            {
                _dungeonIconSource = raw;
                _dungeonIconOverrides = ParsePairs(raw);
            }
            return _dungeonIconOverrides;
        }

        /// <summary>
        /// Picks the pin icon for a dungeon, preferring an override on the resolved label, then one
        /// on the surface location name, then the configured default. An index outside PinType is
        /// ignored rather than cast blindly, so a typo in the cfg cannot produce a pin with no
        /// sprite.
        /// </summary>
        private static Minimap.PinType ResolveDungeonPinType(string label, string locationKey)
        {
            var overrides = DungeonIconOverrides();
            string raw = null;
            if (label == null || !overrides.TryGetValue(label, out raw))
            {
                if (locationKey != null)
                {
                    overrides.TryGetValue(locationKey, out raw);
                }
            }
            if (raw != null && int.TryParse(raw, out int index)
                && Enum.IsDefined(typeof(Minimap.PinType), index))
            {
                return (Minimap.PinType)index;
            }
            return Enum.IsDefined(typeof(Minimap.PinType), DungeonPinIcon.Value)
                ? (Minimap.PinType)DungeonPinIcon.Value
                : Minimap.PinType.Icon2;
        }

        /// <summary>
        /// Turns a prefab name into something readable for a dungeon nobody has catalogued:
        /// BearCave becomes "Bear Cave", PutridHole becomes "Putrid Hole". Only ever reached after
        /// the overrides, the discover label and both tables, so it can never override a known
        /// name - it just replaces the raw prefab name that would otherwise end up on the map.
        ///
        /// A trailing variant number is dropped so TrollCave02 does not read "Troll Cave 02", the
        /// DG_ prefix is dropped when a generator name is all that is available, and a run of
        /// capitals is kept together so ids like a hypothetical DvergrNPC stay intact.
        /// </summary>
        private static string Prettify(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return raw;
            }
            string name = raw;
            if (name.StartsWith("DG_", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(3);
            }
            name = name.Replace('_', ' ');
            int end = name.Length;
            while (end > 0 && char.IsDigit(name[end - 1]))
            {
                end--;
            }
            if (end > 0 && end < name.Length)
            {
                name = name.Substring(0, end);
            }
            var builder = new StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool startsWord = i > 0 && char.IsUpper(c)
                    && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));
                if (startsWord && builder.Length > 0 && builder[builder.Length - 1] != ' ')
                {
                    builder.Append(' ');
                }
                builder.Append(c);
            }
            string pretty = builder.ToString().Trim();
            return pretty.Length > 0 ? pretty : raw;
        }

        private static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
            return (clone >= 0 ? name.Substring(0, clone) : name).Trim();
        }

        /// <summary>
        /// Finds the surface location a dungeon interior belongs to.
        ///
        /// Interiors are not built inside the location prefab - they are generated on a separate
        /// layer above y 3000, which is all Character.InInterior tests. GetComponentInParent
        /// therefore finds nothing, which is why an unaided lookup only ever yields the generator's
        /// own DG_ name. Location.GetLocation handles it: for an interior point it defers to
        /// GetZoneLocation, which matches on zone, and since the interior keeps the surface
        /// location's X and Z and only lifts Y, that resolves to the right location. The same
        /// property is why the pin lands in the right place on the map, which only reads X and Z.
        /// </summary>
        private static Location DungeonLocation(DungeonGenerator generator)
        {
            return generator.GetComponentInParent<Location>()
                ?? Location.GetLocation(generator.transform.position, checkDungeons: true);
        }

        /// <summary>
        /// The names a dungeon can be keyed by, most specific first. The generator prefab is shared
        /// between dungeon types - DG_Cave backs both Troll Caves and Frost Caves - so it is no use
        /// on its own for either naming or overriding. The surface location prefab (TrollCave,
        /// MountainCave01) is distinct per type and is what overrides should key on.
        /// </summary>
        private static string DungeonLocationKey(Location location, string generatorName)
        {
            return StripClone(location?.gameObject.name) ?? generatorName;
        }

        /// <summary>
        /// Prefers the name the game itself puts on screen. Location.m_discoverLabel is the
        /// localisation token Player.UpdateBiome hands to MessageHud.ShowBiomeFoundMsg when you
        /// walk into a location, so localizing it gives exactly the wording the game shows -
        /// "Frost Cave" rather than DG_Cave - in whatever language is set, with no table to
        /// maintain and nothing to update when a dungeon type is added. It also settles the shared
        /// DG_Cave problem for free, because the label belongs to the location rather than the
        /// generator.
        /// </summary>
        /// <summary>
        /// Fallback names for locations that ship with no discover label, keyed by surface location
        /// prefab. Only consulted after m_discoverLabel, so the game's own wording always wins and
        /// this cannot override a correct name; DungeonPinNameOverrides beats both. Best effort
        /// rather than authoritative - the pin log prints the location key for anything not covered,
        /// which is what an override entry takes.
        /// </summary>
        private static readonly Dictionary<string, string> DungeonDefaultNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "MountainCave01", "Frost Cave" },
                { "MountainCave02", "Frost Cave" },
                { "TrollCave", "Troll Cave" },
                { "BearCave", "Bear Cave" },
                { "MorgenHole", "Putrid Hole" },
                { "TrollCave02", "Troll Cave" },
                { "Crypt2", "Burial Chamber" },
                { "Crypt3", "Burial Chamber" },
                { "Crypt4", "Burial Chamber" },
                { "SunkenCrypt1", "Sunken Crypt" },
                { "SunkenCrypt2", "Sunken Crypt" },
                { "SunkenCrypt3", "Sunken Crypt" },
                { "SunkenCrypt4", "Sunken Crypt" },
                { "Mistlands_DvergrTownEntrance1", "Infested Mine" },
                // Both entrance variants lead to the same dungeon - our log shows each running
                // DG_DvergrTown - so they have to share a label. Entrance2 was absent from this
                // table and fell through to a prettified prefab name, which both read wrong and,
                // because duplicate suppression matches on label, left a second pin roughly 30m
                // from the "Infested Mine" one at the same site.
                { "Mistlands_DvergrTownEntrance2", "Infested Mine" },
                // Dvergr excavation sites - a fenced camp with dvergr, extractor crates and a
                // part-dug Jotunn skeleton that yields black marble. Three numbered variants, all
                // covered by this one fragment. Surface-only, so they also have to be named in
                // PinIncludeLocations to reach the pin hook at all.
                { "Mistlands_Excavation", "Dvergr Camp" },
                { "Mistlands_Harbour", "Dvergr Harbor" },
                // The tall dvergr tower with dvergr and component crates inside is authored as
                // "Lighthouse", not as any of the GuardTower variants - the log settled it, at the
                // exact coordinates the player was standing on.
                { "Mistlands_Lighthouse", "Dvergr Tower" },
                // Kept distinct from "Dvergr Tower" rather than sharing it: duplicate suppression
                // matches on label within DungeonPinMergeRadius, so one label for two different
                // structures would let a guard tower hide a real tower 40m away.
                //
                // One fragment is enough for the label - only the variants named in
                // PinIncludeLocations ever get this far, so the ruined ones cannot pick it up
                { "Mistlands_GuardTower", "Dvergr Guard Tower" },
                { "GoblinCamp2", "Fuling Camp" },
                { "MeadowsVillage", "Draugr Village" },
                { "MeadowsFarm", "Meadows Farm" },
            };

        /// <summary>
        /// Fallback keyed on the dungeon generator rather than the surface location, with the biome
        /// breaking ties. There are only a handful of generators and they are reused across every
        /// location variant of a type, so this keeps naming a dungeon correct even for a location
        /// prefab nobody has catalogued - a hypothetical MountainCave03 still resolves through
        /// DG_Cave plus Mountain. The cost is that one generator can back two dungeon types, which
        /// is what the biome disambiguates: DG_Cave is a Troll Cave in the Black Forest and a Frost
        /// Cave in the Mountains.
        ///
        /// Keys are "generator|biome", or the bare generator where the type is unambiguous. The
        /// biome comes from WorldGenerator.GetBiome, which derives it from the world seed and X/Z
        /// alone - it does not need a loaded heightmap, which matters because dungeon interiors sit
        /// above y 3000 where there is no terrain.
        /// </summary>
        private static readonly Dictionary<string, string> DungeonGeneratorNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Our own log confirmed DG_Cave backs MountainCave02 in the Mountains, so the
                // claim elsewhere that Frost Caves use a separate DG_MountainCave is wrong for
                // this build. The alias is kept anyway - an unmatched key costs nothing.
                { "DG_Cave|Mountain", "Frost Cave" },
                { "DG_Cave|BlackForest", "Troll Cave" },
                { "DG_MountainCave", "Frost Cave" },
                { "DG_ForestCrypt", "Burial Chamber" },
                { "DG_SunkenCrypt", "Sunken Crypt" },
                { "DG_DvergrTown", "Infested Mine" },
                { "DG_Hildir_Cave", "Howling Cavern" },
                { "DG_Hildir_ForestCrypt", "Smouldering Tomb" },
                { "DG_GoblinCamp", "Fuling Camp" },
                { "DG_Hole", "Winding Tunnels" },
                { "DG_MorkHalla", "Mörkhalla" },
                { "DG_NorthVillage", "North Village" },
            };

        private static string GeneratorBiomeName(string generatorName, Vector3 position, out string how)
        {
            how = null;
            if (string.IsNullOrEmpty(generatorName))
            {
                return null;
            }
            WorldGenerator world = WorldGenerator.instance;
            if (world != null)
            {
                Heightmap.Biome biome = world.GetBiome(position.x, position.z);
                if (DungeonGeneratorNames.TryGetValue(generatorName + "|" + biome, out string byBiome))
                {
                    how = $"generator {generatorName} in {biome}";
                    return byBiome;
                }
            }
            if (DungeonGeneratorNames.TryGetValue(generatorName, out string byGenerator))
            {
                how = "generator " + generatorName;
                return byGenerator;
            }
            return null;
        }

        /// <summary>
        /// Resolution order: an explicit config override, the game's own on-screen wording, the
        /// location table, the generator-and-biome table, then the raw location prefab name. The
        /// step that supplied the answer is reported through <paramref name="reason"/> so the log
        /// can say where each label came from.
        /// </summary>
        private static string DungeonDisplayName(Location location, string generatorName, Vector3 position, out string reason)
        {
            string key = DungeonLocationKey(location, generatorName);
            string label = location != null ? location.m_discoverLabel : null;

            var overrides = DungeonNameOverrides();
            if (key != null && overrides.TryGetValue(key, out string renamed))
            {
                reason = "config override";
                return renamed;
            }
            if (!string.IsNullOrEmpty(label))
            {
                string localized = Localization.instance.Localize(label);
                // Valheim returns the token in brackets when a key is missing; fall through on that
                // rather than printing "[$location_x]" on the map
                if (!string.IsNullOrEmpty(localized) && localized[0] != '[')
                {
                    reason = "discover label " + label;
                    return localized;
                }
                reason = "discover label " + label + " did not localize";
            }
            else
            {
                reason = "location has no discover label";
            }
            if (key != null)
            {
                if (DungeonDefaultNames.TryGetValue(key, out string known))
                {
                    reason += "; named from location " + key;
                    return known;
                }
                // Fall back to a fragment match so one entry covers numbered variants - MorgenHole
                // catches MorgenHole1 through however many there turn out to be
                foreach (KeyValuePair<string, string> entry in DungeonDefaultNames)
                {
                    if (key.IndexOf(entry.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        reason += $"; named from location fragment {entry.Key}";
                        return entry.Value;
                    }
                }
            }
            string byGenerator = GeneratorBiomeName(generatorName, position, out string how);
            if (byGenerator != null)
            {
                reason += "; named from " + how;
                return byGenerator;
            }
            string pretty = Prettify(key);
            if (!string.IsNullOrEmpty(pretty))
            {
                reason += $"; name derived from prefab {key}";
                return pretty;
            }
            return key;
        }

        /// <summary>
        /// Adds the pin for one dungeon, if it is not already marked.
        /// </summary>
        private static void PinDungeon(DungeonGenerator generator)
        {
            if (generator == null)
            {
                return;
            }
            // CampGrid and CampRadial are surface settlements rather than dungeons
            if (!DungeonPinIncludeCamps.Value && generator.m_algorithm != DungeonGenerator.Algorithm.Dungeon)
            {
                return;
            }
            PinAt(generator.transform.position, DungeonLocation(generator), StripClone(generator.transform.root.name));
        }

        /// <summary>
        /// Adds one pin, if nothing is already marked nearby. Minimap.instance standing in for a
        /// "world is running" check rather than Player.m_localPlayer, because locations can Awake
        /// while the player is still being spawned.
        /// </summary>
        private static void PinAt(Vector3 position, Location location, string generatorName)
        {
            if (!AutoPinDungeons.Value
                || IsPinExcluded(StripClone(location?.gameObject.name), generatorName))
            {
                return;
            }
            string label = DungeonPinLabel.Value;
            string reason = "fixed label";
            if (DungeonPinUseLocationName.Value)
            {
                string specific = DungeonDisplayName(location, generatorName, position, out reason);
                if (!string.IsNullOrEmpty(specific))
                {
                    label = specific;
                }
            }
            string key = DungeonLocationKey(location, generatorName);
            Minimap.PinType type = ResolveDungeonPinType(label, key);
            // Report the location as "none" when the lookup failed, rather than echoing the
            // generator name back as though it were a location
            string locationName = StripClone(location?.gameObject.name) ?? "none";
            AddOrQueuePin(position, type, label,
                $"location {locationName}, generator {generatorName ?? "none"}, from {reason}",
                DungeonPinMergeRadius.Value);
        }

        // Two entry points, because they cover different dungeons.
        //
        // Spawn only runs once asynchronous room loading finishes, so it never fires for a dungeon
        // whose ZDO carries no room data - the hand-built interiors such as troll caves, which the
        // log reports as "Dungeon loaded with 0 rooms from old format". Those were silently never
        // pinned. Load runs for both kinds and is what gives full coverage; Spawn is kept for
        // anything that reaches room placement by some other route.
        //
        // Pinning twice is harmless - the second call finds the pin the first one made and stops at
        // the existing-pin check - so no coordination between the two is needed.
        // ---------------- Forgetting recipes ----------------
        // Repairs a character that learned pieces it should not have. The game logs every recipe as
        // it is learned - "Queue unlock msg:$msg_newpiece:$piece_x" - and the token in that line is
        // exactly the key held in m_knownRecipes, so a log from the session that went wrong is an
        // precise list of what to undo.
        //
        // Deliberately config-driven rather than clever: nothing here tries to work out which
        // recipes were legitimate, because the character records only that a recipe is known, never
        // how it was learned. The list has to come from outside.
        private static readonly FieldInfo KnownRecipesField = AccessTools.Field(typeof(Player), "m_knownRecipes");
        private static readonly MethodInfo UpdateAvailablePiecesListMethod =
            AccessTools.Method(typeof(Player), "UpdateAvailablePiecesList");
        private static bool _unlearnDone;
        private static string _unlearnSource;

        private static void ProcessUnlearnRecipes()
        {
            string raw = UnlearnRecipes.Value ?? string.Empty;
            if (_unlearnSource != raw)
            {
                _unlearnSource = raw;
                _unlearnDone = false;
            }
            if (_unlearnDone)
            {
                return;
            }
            string[] tokens = raw.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
            if (tokens.Length == 0)
            {
                _unlearnDone = true;
                return;
            }
            Player player = Player.m_localPlayer;
            if (player == null || KnownRecipesField == null)
            {
                return;
            }
            if (!(KnownRecipesField.GetValue(player) is HashSet<string> known))
            {
                _unlearnDone = true;
                Debug.LogWarning("Unlearn: could not read the known recipe list");
                return;
            }
            _unlearnDone = true;
            bool apply = UnlearnRecipesApply.Value;
            int found = 0;
            int missing = 0;
            foreach (string token in tokens)
            {
                if (!known.Contains(token))
                {
                    missing++;
                    continue;
                }
                found++;
                Debug.Log($"  {(apply ? "forgetting" : "would forget")} {token}");
                if (apply)
                {
                    known.Remove(token);
                }
            }
            if (apply && found > 0)
            {
                // The build menu is built from a cached list rather than read from m_knownRecipes
                // each time it opens, so removing a recipe leaves the panel showing it until
                // something rebuilds that list. Vanilla calls this after learning a piece for the
                // same reason.
                UpdateAvailablePiecesListMethod?.Invoke(player, null);
            }
            Debug.Log($"Unlearn: {found} known, {missing} not known, of {tokens.Length} listed. "
                + (apply
                    ? "Removed and build menu refreshed. Log out normally so the character is saved."
                    : "Dry run - set UnlearnRecipesApply to true to remove them."));
        }

        // ---------------- Free building ----------------
        // Narrower than the game's own nocost cheat, which is a single Player.NoCostCheat flag that
        // InventoryGui consults for crafting, upgrading, repairing and station requirements alike.
        // Only the two steps involved in placing a piece are touched: the check that decides whether
        // the piece can be placed, and the one that takes the materials afterwards. Crafting and
        // repair carry on costing exactly what they did.
        //
        // Deliberately not persisted. It is meant for the odd moment away from base, and leaving it
        // on by accident would quietly remove the cost of everything built afterwards.
        private static bool _freeBuildEnabled;

        private static bool FreeBuildActive => _freeBuildEnabled && !IsSharedSession();

        private static void ToggleFreeBuild()
        {
            _freeBuildEnabled = !_freeBuildEnabled;
            string message;
            if (!_freeBuildEnabled)
            {
                message = "Free building OFF";
            }
            else if (IsSharedSession())
            {
                // Saying nothing here would look like the toggle had simply failed
                message = "Free building ON, but suspended - someone else is in this session";
            }
            else
            {
                message = "Free building ON - building and crafting cost nothing";
            }
            Debug.Log(message);
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }

        // Removal is gated separately from placement, on the crafting station rather than the
        // materials:
        //
        //     if (!m_noPlacementCost && piece.m_craftingStation != null
        //         && !CraftingStation.HaveBuildStationInRange(...) && !NoWorkbench)
        //         -> "$msg_missingstation"
        //
        // Vanilla's own nocost cheat satisfies that through m_noPlacementCost, which this toggle
        // deliberately does not set - setting it would make crafting and repair free as well. So
        // without this the toggle could place a stone wall miles from a stonecutter and then refuse
        // to take it down again, which is worse than not being able to build it at all.
        //
        // Non-public in the shipped assembly, so patched by name.
        //
        // Limited to pieces this character placed. The station requirement on removal is a design
        // constraint rather than incidental friction: a dungeon cannot hold a workbench, so its
        // interior is meant to be hammer-proof and breakable only by force, and a piece someone
        // else built is meant to need the matching station rebuilt before it comes apart. Waiving
        // the check for everything would quietly remove both rules. World-generated pieces have no
        // creator, so they keep the vanilla behaviour untouched.
        //
        // A prefix rather than a postfix, because the original shows "$msg_missingstation" on screen
        // before returning false. Overriding the result afterwards still allowed the removal, but
        // the player had already been told it failed. Returning early keeps the message from ever
        // being queued - and for a piece that is not yours, vanilla runs in full and the message is
        // the correct outcome.
        [HarmonyPatch(typeof(Player), "CheckCanRemovePiece")]
        class Player_CheckCanRemovePiece_FreeBuild_Patch
        {
            static bool Prefix(Player __instance, Piece piece, ref bool __result)
            {
                if (!FreeBuildActive || piece == null || !piece.IsCreator()
                    || !ReferenceEquals(__instance, Player.m_localPlayer))
                {
                    return true;
                }
                __result = true;
                return false;
            }
        }

        // Closes the loop that free building would otherwise open: build for nothing, take it
        // straight back down, keep the materials. Piece.DropResources returns early for the same
        // reason when the NoBuildCost global key is set.
        //
        // Narrowed to pieces this character built, which the world modifier does not do. The point
        // of suppressing drops is that the materials were never paid for, and that is only ever
        // true of your own pieces - a crypt door or a Fuling hut was not built by you, so taking it
        // apart should pay out exactly as it always does. Piece.IsCreator compares the piece's
        // stored creator against the profile's player id, and world-generated pieces have no
        // creator at all, so they fall outside this by default.
        //
        // A piece you built legitimately before switching the toggle on is caught too. The game
        // keeps no record of what a piece cost, only who placed it, so there is no way to tell the
        // two apart - switch the toggle off before dismantling something you want back.
        [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
        class Piece_DropResources_FreeBuild_Patch
        {
            static bool Prefix(Piece __instance)
            {
                return !(FreeBuildActive && __instance != null && __instance.IsCreator());
            }
        }

        // The crafting equivalent of the piece check above, and it carries the same trap: the
        // recipe overload also serves discovery, distinguished only by its discover flag.
        //
        //     if (!m_knownRecipes.Contains(name) && HaveRequirements(recipe, discover: true, 0))
        //         -> learn it
        //
        // Every crafting UI caller passes discover: false, so only those are answered. Getting this
        // wrong on the piece overload taught a character 58 recipes it had not earned.
        //
        // The crafting station itself is deliberately still required. Valheim's own NoCostCheat
        // would have covered every cost path in one patch, but it also waives needing a station at
        // all, and its roof and fire requirements - the same kind of bypass that made hammer
        // removal a skeleton key.
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new Type[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
        class Player_HaveRequirementsRecipe_FreeBuild_Patch
        {
            static void Postfix(Player __instance, bool discover, ref bool __result)
            {
                if (!__result && !discover && FreeBuildActive && ReferenceEquals(__instance, Player.m_localPlayer))
                {
                    __result = true;
                }
            }
        }

        // Crafting takes its materials two ways. ConsumeResources covers the ordinary case and is
        // already skipped above, but a recipe marked m_requireOnlyOneIngredient instead calls
        // Inventory.RemoveItem(name, amount, quality) directly, which nothing else intercepts.
        //
        // RemoveItem is far too widely used to patch outright, so it is only skipped while
        // DoCrafting is on the stack. The flag is set in a prefix and cleared in a finalizer so an
        // exception mid-craft cannot leave item removal disabled for the rest of the session.
        //
        // Only the three-argument overload is suppressed. DoCrafting also calls
        // RemoveItem(m_craftUpgradeItem), a different overload, which removes the item being
        // upgraded rather than paying a cost - that still has to happen or the upgrade would
        // duplicate the item.
        private static bool _inCrafting;

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        class InventoryGui_DoCrafting_FreeBuild_Patch
        {
            static void Prefix()
            {
                _inCrafting = true;
            }

            static void Finalizer()
            {
                _inCrafting = false;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
        class Inventory_RemoveItem_FreeBuild_Patch
        {
            static bool Prefix()
            {
                return !(_inCrafting && FreeBuildActive);
            }
        }

        // The requirement check for placing a piece. The overload taking a Piece is the build path;
        // the recipe overloads used by crafting are left alone.
        //
        // Restricted to RequirementMode.CanBuild, which is the only mode that asks "can this be
        // placed right now". The same method also serves recipe discovery:
        //
        //     if (!m_knownRecipes.Contains(name) && HaveRequirements(piece, RequirementMode.IsKnown))
        //         -> learn it
        //
        // so answering yes regardless of mode told that sweep the player qualified for everything
        // and taught the character every build piece in the game, Deep North included, the first
        // time anything was built with the toggle on. CanAlmostBuild is left alone too - it decides
        // what the build menu bothers to show, not what can be placed.
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
        class Player_HaveRequirements_FreeBuild_Patch
        {
            static void Postfix(Player __instance, Player.RequirementMode mode, ref bool __result)
            {
                if (!__result && mode == Player.RequirementMode.CanBuild && FreeBuildActive
                    && ReferenceEquals(__instance, Player.m_localPlayer))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        class Player_ConsumeResources_FreeBuild_Patch
        {
            // Runs before the container-pulling prefix, so nothing is taken out of a chest either
            [HarmonyPriority(Priority.First)]
            static bool Prefix(Player __instance)
            {
                return !(FreeBuildActive && ReferenceEquals(__instance, Player.m_localPlayer));
            }
        }

        // ---------------- Input guards ----------------
        // Every hotkey here is read straight from the keyboard in Update, which the game never
        // sees and therefore never suppresses. With a sign, the chat box or the console open, a
        // typed character still reached the mod: typing an emote fired the free-build toggle on
        // the slash, and a space jumped.
        //
        // Two different guards, because they answer different questions.
        //
        // IsTypingText is for the hotkeys: it asks only whether a text field has focus. Using the
        // broader TakeInput would also disable them whenever the inventory or map is open, and the
        // achievement dump is most useful with the achievements panel in front of you.
        private static bool IsTypingText()
        {
            if (Console.IsVisible() || TextInput.IsVisible() || Minimap.InTextInput()
                || PlayerCustomizaton.IsBarberGuiVisible())
            {
                return true;
            }
            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return true;
            }
            // The build menu's search box takes typed input without any of the above being true
            BuildUi buildUi = Hud.instance != null ? Hud.instance.m_buildUi : null;
            return buildUi != null && buildUi.SearchFieldFocused;
        }

        /// <summary>
        /// Every mod hotkey is read through this one place, so the typing guard cannot be forgotten
        /// when a new hotkey is added. Cached per frame because Update polls several hotkeys and the
        /// check walks a handful of UI singletons.
        /// </summary>
        private static int _typingCheckedFrame = -1;
        private static bool _typingCached;

        private static bool HotkeyDown(ConfigEntry<KeyboardShortcut> hotkey)
        {
            if (hotkey == null || !hotkey.Value.IsDown())
            {
                return false;
            }
            if (_typingCheckedFrame != Time.frameCount)
            {
                _typingCheckedFrame = Time.frameCount;
                _typingCached = IsTypingText();
            }
            return !_typingCached;
        }

        // The jump buffer uses the game's own aggregate instead, so a buffered press can never
        // fire somewhere vanilla would have refused the original - it also covers menus, cutscenes,
        // death and teleporting. Non-public in the shipped assembly, hence the handle.
        private static readonly MethodInfo TakeInputMethod = AccessTools.Method(typeof(Player), "TakeInput");

        private static bool PlayerTakingInput(Player player)
        {
            if (TakeInputMethod == null)
            {
                return !IsTypingText();
            }
            return TakeInputMethod.Invoke(player, null) is bool taking && taking;
        }

        // ---------------- Jump leniency ----------------
        // Player.SetControls routes the jump key three ways, and only the last one jumps:
        //
        //     if (m_blocking)                                     Dodge(...)
        //     else if (IsCrouching() || m_crouchToggled || dodge)  Dodge(...)
        //     else                                                Jump()
        //
        // and Character.Jump then drops the call entirely unless IsOnGround(), which is
        // m_lastGroundTouch < 0.2f. So a press can be swallowed either by being routed to a dodge
        // or by landing during the fraction of a second a running stride leaves the ground.
        //
        // The buffer addresses the second case without changing any of vanilla's rules: the press
        // is remembered and retried for a short window, so one that arrived a little early takes
        // effect the moment the character can actually jump. Nothing is forced - Jump still decides
        // - so stamina, dodges, attacks and staggers all still block it exactly as before.
        private static readonly FieldInfo LastGroundTouchField = AccessTools.Field(typeof(Character), "m_lastGroundTouch");
        private static readonly FieldInfo JumpTimerField = AccessTools.Field(typeof(Character), "m_jumpTimer");
        private static readonly FieldInfo CrouchToggledField = AccessTools.Field(typeof(Player), "m_crouchToggled");
        private static float _jumpPressedAt = float.NegativeInfinity;

        private static float ReadFloat(FieldInfo field, object target, float fallback)
        {
            if (field == null || target == null)
            {
                return fallback;
            }
            object value = field.GetValue(target);
            return value is float f ? f : fallback;
        }

        private static void UpdateJumpBuffer()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            // Vanilla only reads the jump button through a path gated on TakeInput, so without
            // this the buffer jumps while the player is typing
            if (!PlayerTakingInput(player))
            {
                _jumpPressedAt = float.NegativeInfinity;
                return;
            }
            bool pressed = ZInput.GetButtonDown("Jump");
            if (pressed)
            {
                _jumpPressedAt = Time.time;
                if (LogJumpBlocks.Value)
                {
                    LogJumpState(player);
                }
            }
            float buffer = JumpBufferSeconds.Value;
            if (buffer <= 0f || _jumpPressedAt == float.NegativeInfinity)
            {
                return;
            }
            if (Time.time - _jumpPressedAt > buffer)
            {
                _jumpPressedAt = float.NegativeInfinity;
                return;
            }
            // ForceJump zeroes m_jumpTimer, so a small value means a jump has just happened and the
            // buffered press has been satisfied - this is what stops it firing twice
            if (ReadFloat(JumpTimerField, player, 1f) < 0.1f)
            {
                _jumpPressedAt = float.NegativeInfinity;
                return;
            }
            // Retrying on the frame it was pressed would double up with vanilla's own handling
            if (pressed)
            {
                return;
            }
            if (!player.IsOnGround() || player.InDodge() || player.InAttack() || player.IsStaggering())
            {
                return;
            }
            player.Jump();
        }

        private static void LogJumpState(Player player)
        {
            float groundTouch = ReadFloat(LastGroundTouchField, player, -1f);
            object crouchToggled = CrouchToggledField?.GetValue(player);
            Debug.Log("Jump pressed: "
                + $"onGround={player.IsOnGround()} lastGroundTouch={groundTouch:0.###} "
                + $"blocking={player.IsBlocking()} crouching={player.IsCrouching()} crouchToggled={crouchToggled} "
                + $"inAttack={player.InAttack()} inDodge={player.InDodge()} staggering={player.IsStaggering()} "
                + $"knockedBack={player.IsKnockedBack()} encumbered={player.IsEncumbered()} "
                + $"stamina={player.HaveStamina(player.m_jumpStaminaUsage)}");
        }

        // ---------------- Pin exclusions ----------------
        private static string[] _pinExclusions;
        private static string _pinExclusionsSource;
        private static readonly HashSet<string> _reportedExclusions = new HashSet<string>();

        /// <summary>
        /// An exclusion used to return before anything was logged, which made a wrongly excluded
        /// pin indistinguishable from one that was never reached. Each distinct name now reports
        /// itself once, so the log says what was skipped and why without repeating per instance.
        /// </summary>
        private static bool IsPinExcluded(params string[] names)
        {
            string raw = PinExcludeNames.Value ?? string.Empty;
            if (_pinExclusions == null || _pinExclusionsSource != raw)
            {
                _pinExclusionsSource = raw;
                _pinExclusions = raw.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToArray();
            }
            if (_pinExclusions.Length == 0)
            {
                return false;
            }
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                foreach (string fragment in _pinExclusions)
                {
                    if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (_reportedExclusions.Add(name))
                        {
                            Debug.Log($"Pin skipped: '{name}' matches exclusion '{fragment}' (PinExcludeNames)");
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private static string[] _pinIncludes;
        private static string _pinIncludesSource;

        private static bool IsIncludedLocation(string name)
        {
            string raw = PinIncludeLocations.Value ?? string.Empty;
            if (_pinIncludes == null || _pinIncludesSource != raw)
            {
                _pinIncludesSource = raw;
                _pinIncludes = raw.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToArray();
            }
            if (_pinIncludes.Length == 0 || string.IsNullOrEmpty(name))
            {
                return false;
            }
            foreach (string fragment in _pinIncludes)
            {
                if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        // ---------------- Pin queue ----------------
        // Everything that pins goes through here, because Minimap.instance does not exist yet while
        // the first zone loads. Anything standing where the player logs in runs Awake during that
        // load and was being dropped silently - the dragon egg underfoot went unpinned while one
        // further away, whose zone loaded later, worked fine. Requests made too early are held and
        // added once the minimap is up.
        //
        // The label and icon are resolved at queue time, so nothing holds a reference to a game
        // object that may be destroyed before the flush. The duplicate check happens at add time
        // instead, since it has to run against the real pin list.
        private struct PendingPin
        {
            public Vector3 Position;
            public Minimap.PinType Type;
            public string Label;
            public string Detail;
            public float MergeRadius;
        }

        private static readonly List<PendingPin> _pendingPins = new List<PendingPin>();
        private const int MaxPendingPins = 128;

        private static bool AddOrQueuePin(Vector3 position, Minimap.PinType type, string label, string detail, float mergeRadius)
        {
            Minimap map = Minimap.instance;
            if (map == null)
            {
                // Bounded so a world that never produces a minimap cannot grow this without limit
                if (_pendingPins.Count < MaxPendingPins)
                {
                    _pendingPins.Add(new PendingPin
                    {
                        Position = position,
                        Type = type,
                        Label = label,
                        Detail = detail,
                        MergeRadius = mergeRadius,
                    });
                }
                return false;
            }
            if (FindBlockingPin(map, position, Mathf.Max(0f, mergeRadius), label, out string blocker, out float blockerDistance))
            {
                if (PinDiagnostics.Value)
                {
                    Debug.Log($"Pin declined: '{label}' at X {position.x:0} Z {position.z:0} - "
                        + $"'{blocker}' already pinned {blockerDistance:0.#}m away ({detail})");
                }
                return false;
            }
            Minimap.PinData added = AddMapPinAndReturn(map, position, type, label, true, false);
            if (added == null)
            {
                // Reflection into AddPin failed, which is a mod problem rather than a map one, so
                // this is reported whether or not diagnostics are on
                Debug.LogWarning($"Pin failed: '{label}' at X {position.x:0} Z {position.z:0} - "
                    + $"could not call Minimap.AddPin ({detail})");
                return false;
            }
            // The icon-filter state is reported on the same line rather than as a second one: a pin
            // that is added but not drawn has been reported twice now, and the filter for its type
            // is the first thing worth ruling out. UpdatePins skips any pin whose type is hidden,
            // and AutoPinKeepIconFilter deliberately leaves a hidden type hidden.
            bool[] shownTypes = VisibleIconTypesField?.GetValue(map) as bool[];
            int typeIndex = (int)type;
            string iconShown = shownTypes != null && typeIndex >= 0 && typeIndex < shownTypes.Length
                ? shownTypes[typeIndex].ToString()
                : "unknown";
            Debug.Log($"Pin added: '{label}' as {type} at X {position.x:0} Z {position.z:0} "
                + $"(iconShown={iconShown}; {detail})");
            WatchPinMarker(added, label);
            return true;
        }

        /// <summary>
        /// Whether something with the same label is already pinned within the merge radius.
        ///
        /// This used to call Minimap.GetClosestPin, which compares nothing but distance, so any pin
        /// of any kind suppressed the new one. In Black Forest that is close to fatal: copper and
        /// tin deposits are auto-pinned too and cluster around exactly the places troll and bear
        /// caves sit, so a cave 30m from a Copper pin was silently declined. Comparing the label
        /// keeps the behaviour the radius is actually for - not pinning the same dungeon twice as
        /// its zone reloads - without letting unrelated pins mask a real discovery.
        ///
        /// GetClosestPin would also only have returned the single nearest pin, so a same-label pin
        /// slightly further out than an unrelated one could be missed entirely. Scanning handles
        /// both. Unsaved pins are skipped to match vanilla, which treats them as transient.
        /// </summary>
        private static bool FindBlockingPin(Minimap map, Vector3 position, float radius, string label,
            out string blocker, out float blockerDistance)
        {
            blocker = null;
            blockerDistance = 0f;
            var pins = PinsField?.GetValue(map) as List<Minimap.PinData>;
            if (pins == null)
            {
                // Without the pin list there is no way to tell a duplicate from a new find. Adding
                // is the recoverable mistake - a stray extra pin can be deleted, a dungeon that
                // never pins is one the player has to stumble on again.
                return false;
            }
            foreach (Minimap.PinData pin in pins)
            {
                if (pin == null || !pin.m_save || !string.Equals(pin.m_name, label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                float distance = Utils.DistanceXZ(position, pin.m_pos);
                if (distance < radius && (blocker == null || distance < blockerDistance))
                {
                    blocker = pin.m_name;
                    blockerDistance = distance;
                }
            }
            return blocker != null;
        }

        /// <summary>
        /// Checks, a moment after a pin is added, whether Minimap.UpdatePins actually built a
        /// marker for it.
        ///
        /// UpdatePins either instantiates m_pinPrefab into m_uiElement or calls DestroyPinMarker
        /// and moves on, so that one field is the difference between "the pin exists but is not
        /// drawn" and "the pin is drawn and was simply not spotted". Guessing between those from a
        /// screenshot has already wasted a round.
        ///
        /// Delayed rather than immediate because UpdatePins runs later in the frame loop, off the
        /// m_pinUpdateRequired flag that AddPin sets.
        /// </summary>
        private const float PinMarkerCheckSeconds = 2f;

        private class WatchedPin
        {
            public Minimap.PinData Pin;
            public string Label;
            public float DueAt;
        }

        private static readonly List<WatchedPin> _watchedPins = new List<WatchedPin>();

        private static void WatchPinMarker(Minimap.PinData pin, string label)
        {
            if (pin == null || _watchedPins.Count > 32)
            {
                return;
            }
            _watchedPins.Add(new WatchedPin
            {
                Pin = pin,
                Label = label,
                DueAt = Time.realtimeSinceStartup + PinMarkerCheckSeconds,
            });
        }

        private static void UpdateWatchedPins()
        {
            if (_watchedPins.Count == 0)
            {
                return;
            }
            float now = Time.realtimeSinceStartup;
            for (int i = _watchedPins.Count - 1; i >= 0; i--)
            {
                WatchedPin watched = _watchedPins[i];
                if (now < watched.DueAt)
                {
                    continue;
                }
                _watchedPins.RemoveAt(i);
                Minimap.PinData pin = watched.Pin;
                if (pin == null)
                {
                    Debug.LogWarning($"Pin marker check: '{watched.Label}' - the PinData itself is gone");
                    continue;
                }
                bool inList = false;
                Minimap map = Minimap.instance;
                var pins = map != null ? PinsField?.GetValue(map) as List<Minimap.PinData> : null;
                if (pins != null)
                {
                    inList = pins.Contains(pin);
                }
                bool built = pin.m_uiElement != null;
                // A pin with no marker is ordinary: UpdatePins only builds one for a pin inside the
                // displayed area, so everything off-screen reports NONE. What is not ordinary is a
                // pin with no marker that the player is standing next to, which is the case that
                // has gone unexplained twice. Only that combination is reported when diagnostics
                // are off, so the tripwire can stay armed without filling the log.
                Player player = Player.m_localPlayer;
                float distance = player != null
                    ? Vector3.Distance(player.transform.position, pin.m_pos)
                    : float.MaxValue;
                bool suspicious = !built && inList && distance < 64f;

                if (suspicious)
                {
                    Debug.LogWarning($"Pin marker missing: '{watched.Label}' is {distance:0}m away and in the "
                        + $"pin list, but UpdatePins built no marker for it (mapMode="
                        + $"{(map != null ? map.m_mode.ToString() : "no map")}). This is the case where a pin "
                        + "exists but never appears until the world is reloaded.");
                }
                else if (PinDiagnostics.Value)
                {
                    Debug.Log($"Pin marker check: '{watched.Label}' after {PinMarkerCheckSeconds:0.#}s - "
                        + $"marker={(built ? "built" : "NONE")}, stillInPinList={inList}, {distance:0}m away, "
                        + $"mapMode={(map != null ? map.m_mode.ToString() : "no map")}");
                }
            }
        }

        private static void FlushPendingPins()
        {
            if (_pendingPins.Count == 0 || Minimap.instance == null)
            {
                return;
            }
            var queued = new List<PendingPin>(_pendingPins);
            _pendingPins.Clear();
            int added = 0;
            foreach (PendingPin pin in queued)
            {
                if (AddOrQueuePin(pin.Position, pin.Type, pin.Label, pin.Detail + ", queued during load", pin.MergeRadius))
                {
                    added++;
                }
            }
            Debug.Log($"Flushed {queued.Count} pin(s) queued before the minimap existed, {added} added");
        }

        // ---------------- Resource node pins ----------------
        // Matched on a fragment of the prefab name rather than an exact list, because the deposits
        // are named inconsistently and a substring survives that: "Copper" catches the copper
        // deposit whatever its full prefab name turns out to be. Every pin logs the prefab it
        // matched, so the list can be tightened once the real names are known.
        //
        // Two sources, for the same reason the dungeon pins need two: tar pits are placed as
        // Locations, while ore deposits are MineRock objects that simply load with their zone.
        // Each rule is a prefab fragment plus the label to show. A rule written without a label
        // keeps working and falls back to a name derived from the prefab.
        private static List<KeyValuePair<string, string>> _resourceRules;
        private static string _resourceRulesSource;

        private static List<KeyValuePair<string, string>> ResourceRules()
        {
            string raw = ResourcePinNames.Value ?? string.Empty;
            if (_resourceRules != null && _resourceRulesSource == raw)
            {
                return _resourceRules;
            }
            _resourceRulesSource = raw;
            _resourceRules = new List<KeyValuePair<string, string>>();
            foreach (string entry in raw.Split(','))
            {
                string trimmed = entry.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                int split = trimmed.IndexOf('=');
                if (split > 0)
                {
                    _resourceRules.Add(new KeyValuePair<string, string>(
                        trimmed.Substring(0, split).Trim(), trimmed.Substring(split + 1).Trim()));
                }
                else
                {
                    _resourceRules.Add(new KeyValuePair<string, string>(trimmed, null));
                }
            }
            return _resourceRules;
        }

        /// <summary>
        /// True when the prefab matches a rule. <paramref name="label"/> is the configured label, or
        /// null when the rule carries none and the name should be derived instead.
        /// </summary>
        private static bool TryMatchResource(string prefabName, out string label)
        {
            label = null;
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }
            foreach (KeyValuePair<string, string> rule in ResourceRules())
            {
                if (prefabName.IndexOf(rule.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    label = rule.Value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Names every location this hook sees, once per distinct prefab, while PinDiagnostics is on.
        ///
        /// The gap this fills: a location with no interior that is not in PinIncludeLocations is
        /// dropped here, before any pin is attempted, so it produced no log line of any kind. An
        /// expected pin that never appeared was therefore indistinguishable from a location the hook
        /// never ran for at all, and the only way to tell them apart was to add the thing to the
        /// include list and go back out to look.
        ///
        /// Once per name rather than per instance because a location re-Awakes every time its zone
        /// reloads, which for something common would be thousands of lines a session.
        /// </summary>
        private static readonly HashSet<string> _reportedLocations =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static void ReportLocationSeen(string name, bool hasInterior, bool willPin, Vector3 position)
        {
            if (!PinDiagnostics.Value || string.IsNullOrEmpty(name) || !_reportedLocations.Add(name))
            {
                return;
            }
            string verdict = willPin
                ? "will pin"
                : hasInterior
                    ? "no pin - has an interior but PinInteriorLocations is off"
                    : "no pin - surface only and not matched by PinIncludeLocations";
            Debug.Log($"Location seen: {name} at X {position.x:0} Z {position.z:0} "
                + $"(hasInterior={hasInterior}) - {verdict}");
        }

        // The six component hooks below cover anything mineable, pickable or structural, which was
        // every resource until the Yggdrasil root. The root is a sap-extractor attachment point
        // rather than a deposit: it carries a ZNetView and nothing else, so none of them ever saw
        // it, and because a name that matches no rule returns silently it looked identical to a
        // wrong prefab name. The nearby-object dump is what separated the two - "YggdrasilRoot
        // (none)" at 5.4m.
        //
        // ZNetView is the common denominator of everything that exists in the world, so this closes
        // the gap for good rather than for one prefab. Awake runs for every networked object
        // including projectiles and effects, so the order inside matters: the name test comes first
        // because it is cheap and almost always fails, and the component checks - which cost a
        // GetComponent each - only run for something that already matched a rule.
        //
        // Those checks exist to avoid pinning twice. A copper deposit is a Destructible and is
        // already handled by its own hook; without this it would be pinned here as well and the
        // second attempt would show up as a declined duplicate.
        [HarmonyPatch(typeof(ZNetView), "Awake")]
        class ZNetView_Awake_ResourcePin_Patch
        {
            static void Postfix(ZNetView __instance)
            {
                if (!AutoPinResources.Value || __instance == null)
                {
                    return;
                }
                string prefabName = __instance.gameObject.name;
                if (!TryMatchResource(prefabName, out _))
                {
                    return;
                }
                if (__instance.GetComponent<Destructible>() != null
                    || __instance.GetComponent<MineRock5>() != null
                    || __instance.GetComponent<MineRock>() != null
                    || __instance.GetComponent<Pickable>() != null
                    || __instance.GetComponent<Leviathan>() != null
                    || __instance.GetComponent<Location>() != null)
                {
                    return;
                }
                PinResource(__instance.transform.position, prefabName);
            }
        }

        private static void PinResource(Vector3 position, string prefabName)
        {
            // Reached for every destructible that loads, so the cheap rejections come first and the
            // string work only happens for something that is actually going to be pinned
            if (!AutoPinResources.Value || string.IsNullOrEmpty(prefabName)
                || !TryMatchResource(prefabName, out string configuredLabel)
                || IsPinExcluded(prefabName))
            {
                return;
            }
            string name = StripClone(prefabName);
            string label = !string.IsNullOrEmpty(configuredLabel) ? configuredLabel : Prettify(name);
            Minimap.PinType type = Enum.IsDefined(typeof(Minimap.PinType), ResourcePinIcon.Value)
                ? (Minimap.PinType)ResourcePinIcon.Value
                : Minimap.PinType.Icon3;
            AddOrQueuePin(position, type, label, "prefab " + name, ResourcePinMergeRadius.Value);
        }

        // The intact deposit in the world is a Destructible - rock4_copper, silvervein, mudpile,
        // FlametalRockstand - and only becomes a MineRock5 once mined, when the _frac version
        // replaces it. Hooking the mineable types alone therefore saw nothing until a node had
        // already been broken into.
        //
        // Destructible covers trees, rocks and every other breakable, so this runs often. The name
        // test is kept first and cheap for that reason, and nothing else is touched until it
        // passes. Awake is non-public in the shipped assembly, which only affects calling it.
        [HarmonyPatch(typeof(Destructible), "Awake")]
        class Destructible_Awake_Patch
        {
            static void Postfix(Destructible __instance)
            {
                if (__instance != null)
                {
                    PinResource(__instance.transform.position, __instance.gameObject.name);
                }
            }
        }

        // Leviathans. Hooked on their own component rather than by prefab name, because the thing
        // the player sees is the Leviathan while the mineable part is a child MineRock whose name
        // says nothing useful. The rule still has to be present in ResourcePinNames for it to pin,
        // so it stays configurable like the rest.
        //
        // The component is reused for the Ashlands flametal spire, prefab LeviathanLava, which
        // floats in lava the same way. Rules are matched in order, so LeviathanLava has to sit
        // ahead of Leviathan in ResourcePinNames or the spire is labelled as a leviathan.
        [HarmonyPatch(typeof(Leviathan), "Awake")]
        class Leviathan_Awake_Patch
        {
            static void Postfix(Leviathan __instance)
            {
                if (__instance != null)
                {
                    PinResource(__instance.transform.position, __instance.gameObject.name);
                }
            }
        }

        // Dragon eggs and anything else picked up rather than mined. Pickable also covers berries
        // and mushrooms, so like the Destructible hook this leans on the name test rejecting early.
        [HarmonyPatch(typeof(Pickable), "Awake")]
        class Pickable_Awake_Patch
        {
            static void Postfix(Pickable __instance)
            {
                if (__instance != null)
                {
                    PinResource(__instance.transform.position, __instance.gameObject.name);
                }
            }
        }

        // Kept so a node already mined into its fractured form still pins
        [HarmonyPatch(typeof(MineRock5), "Awake")]
        class MineRock5_Awake_Patch
        {
            static void Postfix(MineRock5 __instance)
            {
                if (__instance != null)
                {
                    PinResource(__instance.transform.position, __instance.gameObject.name);
                }
            }
        }

        [HarmonyPatch(typeof(MineRock), "Start")]
        class MineRock_Start_Patch
        {
            static void Postfix(MineRock __instance)
            {
                if (__instance != null)
                {
                    PinResource(__instance.transform.position, __instance.gameObject.name);
                }
            }
        }

        // Covers interiors that are one fixed space rather than a set of rooms. They still have a
        // Location with m_hasInterior, and Awake runs when the zone loads, so the proximity
        // behaviour matches the generator hooks. A dungeon that has both paths is pinned once - the
        // second attempt stops at the existing-pin check.
        [HarmonyPatch(typeof(Location), "Awake")]
        class Location_Awake_Patch
        {
            static void Postfix(Location __instance)
            {
                if (__instance == null)
                {
                    return;
                }
                // Tar pits and similar arrive as locations rather than mineable rocks
                PinResource(__instance.transform.position, __instance.gameObject.name);
                // A surface structure has neither an interior nor a generator, so it only pins when
                // it has been named explicitly
                string locationName = StripClone(__instance.gameObject.name);
                bool interior = PinInteriorLocations.Value && __instance.m_hasInterior;
                bool included = IsIncludedLocation(locationName);
                ReportLocationSeen(locationName, __instance.m_hasInterior, interior || included,
                    __instance.transform.position);
                if (!interior && !included)
                {
                    return;
                }
                PinAt(__instance.transform.position, __instance, null);
            }
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Load")]
        class DungeonGenerator_Load_Patch
        {
            static void Postfix(DungeonGenerator __instance) { PinDungeon(__instance); }
        }

        [HarmonyPatch(typeof(DungeonGenerator), "Spawn")]
        class DungeonGenerator_Spawn_Patch
        {
            static void Postfix(DungeonGenerator __instance) { PinDungeon(__instance); }
        }

        // ---------------- Guaranteed first trophy ----------------
        // The game already records every item the character has ever picked up, and
        // IncrementStatItemPickup writes bucket 0 unconditionally - outside the CanGetAchievements
        // gate that governs the other buckets. That makes m_playerStats[0].m_itemPickupStats a
        // complete record regardless of mods or cheat state, and it is an increment rather than a
        // max guard, so it is not exposed to the bucket latch bug that LatchedStats repairs.
        // Achievements.FindTrophiesForAchievements identifies trophies the same way this does, by
        // ItemType.Trophy, so "collected" here means what the game means by it.
        //
        // This postfixes GenerateDropList rather than altering Drop.m_chance. The chance field
        // feeds a pseudo-random counter in s_pseudoCounter that deliberately smooths rare drops
        // over successive kills; writing to it would corrupt that state for every later kill, and
        // it is shared static data. Appending to the finished list leaves all of it alone.
        private static readonly Dictionary<string, bool> _trophyCollectedCache = new Dictionary<string, bool>();
        private static float _trophyCacheClearedAt = float.NegativeInfinity;
        private const float TrophyCacheSeconds = 30f;

        /// <summary>
        /// True when this character has picked up the named trophy at least once. Cached briefly
        /// because a death can generate several drops at once and the lookup walks a dictionary per
        /// drop; the cache is dropped periodically so a trophy picked up mid-session is noticed.
        /// </summary>
        private static bool HasCollectedTrophy(PlayerProfile profile, string itemName)
        {
            float now = Time.realtimeSinceStartup;
            if (now - _trophyCacheClearedAt > TrophyCacheSeconds)
            {
                _trophyCacheClearedAt = now;
                _trophyCollectedCache.Clear();
            }
            if (_trophyCollectedCache.TryGetValue(itemName, out bool cached))
            {
                return cached;
            }
            bool collected = false;
            var stats = profile.m_playerStats;
            for (int i = 0; i < stats.Length && !collected; i++)
            {
                var bucket = stats[i];
                if (bucket?.m_itemPickupStats != null
                    && bucket.m_itemPickupStats.TryGetValue(itemName, out float count)
                    && count > 0f)
                {
                    collected = true;
                }
            }
            _trophyCollectedCache[itemName] = collected;
            return collected;
        }

        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        class CharacterDrop_GenerateDropList_Patch
        {
            static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
            {
                if (!GuaranteeFirstTrophy.Value || __result == null || __instance == null || __instance.m_drops == null)
                {
                    return;
                }
                // Loot is rolled by whoever owns the creature, so on a shared world this would
                // change what everyone sees based on one player's collection
                if (IsSharedSession())
                {
                    return;
                }
                Game game = Game.instance;
                PlayerProfile profile = game != null ? game.GetPlayerProfile() : null;
                if (profile == null || profile.m_playerStats == null)
                {
                    return;
                }
                foreach (CharacterDrop.Drop drop in __instance.m_drops)
                {
                    GameObject prefab = drop?.m_prefab;
                    if (prefab == null)
                    {
                        continue;
                    }
                    ItemDrop item = prefab.GetComponent<ItemDrop>();
                    if (item == null || item.m_itemData == null || item.m_itemData.m_shared == null)
                    {
                        continue;
                    }
                    if (item.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                    {
                        continue;
                    }
                    if (HasCollectedTrophy(profile, item.m_itemData.m_shared.m_name))
                    {
                        continue;
                    }
                    bool alreadyRolled = false;
                    for (int i = 0; i < __result.Count; i++)
                    {
                        if (__result[i].Key == prefab)
                        {
                            alreadyRolled = true;
                            break;
                        }
                    }
                    if (alreadyRolled)
                    {
                        continue;
                    }
                    __result.Add(new KeyValuePair<GameObject, int>(prefab, 1));
                    Debug.Log($"First trophy guaranteed: {prefab.name}");
                }
            }
        }

        // ---------------- Log noise filter ----------------
        // Dropped at ZLog rather than further down, because that is where the game writes and it
        // keeps the line out of the BepInEx file, the console mirror and the in-game console at
        // once. PlayerClothWindShelter is the motivating case: it logs an error every time a
        // prefab without its MagicaCloth component wakes up, which is constant, and the message is
        // Iron Gate telling themselves to fix prefab authoring.
        //
        // Only string messages are tested. ZLog takes object, and calling ToString on everything
        // passing through the logger would allocate on a path that runs constantly.
        private static string[] _suppressedLogFragments;
        private static string _suppressedLogSource;

        private static bool ShouldSuppressLog(object message)
        {
            if (SuppressLogMessages == null)
            {
                // Reached for log events raised before Awake binds the config
                return false;
            }
            string raw = SuppressLogMessages.Value ?? string.Empty;
            if (_suppressedLogFragments == null || _suppressedLogSource != raw)
            {
                _suppressedLogSource = raw;
                _suppressedLogFragments = raw.Split(',')
                    .Select(f => f.Trim())
                    .Where(f => f.Length > 0)
                    .ToArray();
            }
            if (_suppressedLogFragments.Length == 0 || message == null)
            {
                return false;
            }
            // ZLog hands us a string, but a BepInEx log event's Data can be any object - Unity's
            // messages arrive as strings, while a plugin may pass something else. ToString is only
            // reached once a fragment is actually configured, so the common no-filter case still
            // allocates nothing.
            string text = message as string ?? message.ToString();
            if (text == null)
            {
                return false;
            }
            foreach (string fragment in _suppressedLogFragments)
            {
                if (text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(ZLog), nameof(ZLog.LogError))]
        class ZLog_LogError_Patch
        {
            static bool Prefix(object __0) => !ShouldSuppressLog(__0);
        }

        [HarmonyPatch(typeof(ZLog), nameof(ZLog.LogWarning))]
        class ZLog_LogWarning_Patch
        {
            static bool Prefix(object __0) => !ShouldSuppressLog(__0);
        }

        [HarmonyPatch(typeof(ZLog), nameof(ZLog.Log))]
        class ZLog_Log_Patch
        {
            static bool Prefix(object __0) => !ShouldSuppressLog(__0);
        }

        // The ZLog patches above only catch what the game logs through its own logger. Unity's own
        // messages - the ones BepInEx files under "[Unity Log]" - never pass through ZLog, so they
        // need a second interception point. "Rotation quaternions must be unit length." comes from
        // native Unity code reacting to a non-normalised rotation and is raised through the log
        // callback; devcommands fly triggers it several times a second.
        //
        // Logger.InternalLogEvent is the funnel every BepInEx log event passes through, whatever
        // raised it, so one prefix here covers Unity, other plugins and anything the game forwards.
        // Suppressing at this level keeps the line out of the BepInEx log file and the console
        // mirror; the ZLog patches are still worth keeping because they stop a game-raised message
        // earlier, before it reaches the in-game console as well.
        //
        // It is private, which is fine - Harmony patches non-public methods through reflection.
        // Matched on LogEventArgs.Data rather than the formatted line so the test is the same one
        // the ZLog patches use.
        [HarmonyPatch(typeof(BepInEx.Logging.Logger), "InternalLogEvent")]
        class Logger_InternalLogEvent_Patch
        {
            // This is the one patch in the mod aimed at BepInEx rather than the game, so a BepInEx
            // update could rename it out from under us. Harmony throws from PatchAll when a target
            // cannot be resolved, which would take the whole plugin down over a log filter, so the
            // class opts itself out instead. Prepare runs before Harmony resolves the target.
            static bool Prepare()
            {
                if (AccessTools.Method(typeof(BepInEx.Logging.Logger), "InternalLogEvent") != null)
                {
                    return true;
                }
                Debug.LogWarning("PipsMod: BepInEx.Logging.Logger.InternalLogEvent not found - "
                    + "SuppressLogMessages will still filter the game's own messages, but not Unity's.");
                return false;
            }

            static bool Prefix(BepInEx.Logging.LogEventArgs __1)
            {
                return __1 == null || !ShouldSuppressLog(__1.Data);
            }
        }

        // ---------------- Achievement progress dump ----------------
        // The achievements panel shows one achievement at a time and has to be reopened after every
        // craft, which makes a list like "craft one of everything" painful to work through. This
        // writes the outstanding requirements to a file instead.
        //
        // Requirements come in three shapes and each knows how to test itself, so IsMet is called
        // rather than the operator logic being reimplemented - RequirementOperator has four values
        // and getting one wrong would quietly mislabel entries. The stats are read from the bucket
        // the achievement itself declares, the same one AchievementsGui reads.
        /// <summary>
        /// Where a craftable item sits in the crafting UI: which station lists it, and the stable
        /// part of that list's ordering.
        ///
        /// InventoryGui sorts by craftable-first, then m_listSortWeight, then the raw name token.
        /// Only the last two are reproducible here - craftable-first depends on what is in the
        /// inventory at that moment, and would reshuffle the file every time something is crafted,
        /// which is the opposite of useful for a checklist.
        /// </summary>
        private struct RecipePlace
        {
            public string Station;
            public int SortWeight;
        }

        private static Dictionary<string, RecipePlace> BuildRecipeIndex()
        {
            var index = new Dictionary<string, RecipePlace>();
            ObjectDB odb = ObjectDB.instance;
            if (odb?.m_recipes == null)
            {
                return index;
            }
            foreach (Recipe recipe in odb.m_recipes)
            {
                var shared = recipe?.m_item?.m_itemData?.m_shared;
                if (shared == null || index.ContainsKey(shared.m_name))
                {
                    continue;
                }
                index[shared.m_name] = new RecipePlace
                {
                    Station = recipe.m_craftingStation != null
                        ? Localization.instance.Localize(recipe.m_craftingStation.m_name)
                        : "No station",
                    SortWeight = recipe.m_listSortWeight,
                };
            }
            return index;
        }

        private static string FormatRequirement(string label, float have, float need, bool met)
        {
            string name = Localization.instance.Localize(label);
            if (string.IsNullOrEmpty(name) || name[0] == '[')
            {
                name = label;
            }
            return $"    [{(met ? "x" : " ")}] {name}  {have:0.##}/{need:0.##}";
        }

        private static void DumpAchievementProgress()
        {
            Achievements achievements = Achievements.m_instance;
            Game game = Game.instance;
            PlayerProfile profile = game != null ? game.GetPlayerProfile() : null;
            if (achievements?.m_achievementLists == null || profile?.m_playerStats == null)
            {
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "Achievement dump needs a loaded world");
                return;
            }

            _recipeIndex = null;
            var report = new StringBuilder();
            int incomplete = 0;
            int outstanding = 0;
            foreach (AchievementList list in achievements.m_achievementLists)
            {
                if (list?.m_achievements == null)
                {
                    continue;
                }
                foreach (Achievement achievement in list.m_achievements)
                {
                    if (achievement == null || achievement.m_unlocked)
                    {
                        continue;
                    }
                    int bucketIndex = (int)achievement.m_difficultyRequirement;
                    if (bucketIndex < 0 || bucketIndex >= profile.m_playerStats.Length)
                    {
                        continue;
                    }
                    PlayerProfile.PlayerStats bucket = profile.m_playerStats[bucketIndex];
                    if (bucket == null)
                    {
                        continue;
                    }

                    var lines = new List<string>();

                    foreach (Achievement.PlayerStatRequirement req in achievement.m_statTrigger)
                    {
                        bucket.m_stats.TryGetValue(req.m_stat, out float have);
                        bool met = req.IsMet(profile, achievement.m_difficultyRequirement);
                        if (!met)
                        {
                            lines.Add(FormatRequirement("$stat_" + req.m_stat, have, req.m_amountAboveEquals, met));
                        }
                    }

                    AddCraftRequirements(lines, achievement.m_itemCraftTriggers, bucket.m_itemCraftStats, achievement.m_difficultyRequirement);
                    AddDictRequirements(lines, achievement.m_foodEatenTriggers, bucket.m_foodEatenStats, achievement.m_difficultyRequirement);
                    AddDictRequirements(lines, achievement.m_itemPickupTriggers, bucket.m_itemPickupStats, achievement.m_difficultyRequirement);
                    AddDictRequirements(lines, achievement.m_pickableTriggers, bucket.m_pickableStats, achievement.m_difficultyRequirement);
                    AddDictRequirements(lines, achievement.m_piecePlacedTriggers, bucket.m_piecesPlacedStats, achievement.m_difficultyRequirement);

                    foreach (Achievement.EnemyStatRequirement req in achievement.m_enemyStatsTriggers)
                    {
                        int modifier = (int)req.m_modifier;
                        var dict = (bucket.m_enemyStats != null && modifier >= 0 && modifier < bucket.m_enemyStats.Length)
                            ? bucket.m_enemyStats[modifier]
                            : null;
                        if (dict == null)
                        {
                            continue;
                        }
                        dict.TryGetValue(req.m_stat, out float have);
                        if (!req.IsMet(dict, achievement.m_difficultyRequirement))
                        {
                            lines.Add(FormatRequirement(req.m_stat, have, req.m_amount, met: false));
                        }
                    }

                    incomplete++;
                    outstanding += lines.Count;
                    string title = Localization.instance.Localize(achievement.m_name);
                    report.AppendLine($"{title}  ({achievement.m_id}, {achievement.m_difficultyRequirement})");
                    if (lines.Count == 0)
                    {
                        // Every listed requirement is already met - the lenient build achievements
                        // are scored in aggregate rather than per requirement, so this is expected
                        // for them rather than a sign the dump missed something
                        report.AppendLine("    (no outstanding per-requirement entries)");
                    }
                    else
                    {
                        foreach (string line in lines)
                        {
                            report.AppendLine(line);
                        }
                    }
                    report.AppendLine();
                }
            }

            string path = Path.Combine(DumpOutputDirectory, "PipsMod_AchievementProgress.txt");
            File.WriteAllText(path, report.ToString());
            string summary = $"Achievement progress: {incomplete} unfinished, {outstanding} requirement(s) outstanding";
            Debug.Log($"{summary} -> {path}");
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, summary);
        }

        /// <summary>
        /// Craft requirements grouped by the station that makes them, each group in the crafting
        /// list's own stable order, so the file can be worked through one station at a time instead
        /// of walking back and forth between benches.
        /// </summary>
        private static void AddCraftRequirements(List<string> lines, List<Achievement.DictStatRequirement> triggers,
            Dictionary<string, float> stats, DifficultyRequirement difficulty)
        {
            if (triggers == null || stats == null)
            {
                return;
            }
            Dictionary<string, RecipePlace> index = _recipeIndex ?? (_recipeIndex = BuildRecipeIndex());
            var outstanding = new List<Achievement.DictStatRequirement>();
            foreach (Achievement.DictStatRequirement req in triggers)
            {
                if (!req.IsMet(stats, difficulty))
                {
                    outstanding.Add(req);
                }
            }
            if (outstanding.Count == 0)
            {
                return;
            }
            var grouped = outstanding
                .GroupBy(r => index.TryGetValue(r.m_stat, out RecipePlace place) ? place.Station : "Unknown station")
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var group in grouped)
            {
                lines.Add($"    {group.Key}:");
                var ordered = group
                    .OrderBy(r => index.TryGetValue(r.m_stat, out RecipePlace place) ? place.SortWeight : int.MaxValue)
                    .ThenBy(r => r.m_stat, StringComparer.Ordinal);
                foreach (Achievement.DictStatRequirement req in ordered)
                {
                    stats.TryGetValue(req.m_stat, out float have);
                    lines.Add("  " + FormatRequirement(req.m_stat, have, req.m_amount, met: false));
                }
            }
        }

        private static Dictionary<string, RecipePlace> _recipeIndex;

        private static void AddDictRequirements(List<string> lines, List<Achievement.DictStatRequirement> triggers,
            Dictionary<string, float> stats, DifficultyRequirement difficulty)
        {
            if (triggers == null || stats == null)
            {
                return;
            }
            foreach (Achievement.DictStatRequirement req in triggers)
            {
                if (req.IsMet(stats, difficulty))
                {
                    continue;
                }
                stats.TryGetValue(req.m_stat, out float have);
                lines.Add(FormatRequirement(req.m_stat, have, req.m_amount, met: false));
            }
        }

        // ---------------- Achievement stat bucket repair ----------------
        // Vanilla keeps achievement stats in one bucket per DifficultyRequirement. Writes go to
        // bucket 0 (RawStats) unconditionally and to bucket 1 (Any) plus the bucket for the world's
        // current combat difficulty when CanGetAchievements() passes - see PlayerProfile.SetStat.
        //
        // Stats that record a maximum rather than a running total are updated with a read-modify-
        // write that reads *one* bucket and writes *three*:
        //
        //     float stat  = GetStat(ConsecutiveDaysSurvived);       // reads the CURRENT bucket
        //     float stat2 = GetStat(ConsecutiveDaysSurvivedMax);    // reads the CURRENT bucket
        //     if (stat > stat2) SetStat(ConsecutiveDaysSurvivedMax, stat);   // writes 0, 1 and current
        //
        // Once the current difficulty bucket runs ahead of buckets 0 and 1, the guard keeps
        // comparing against that larger value, the write never fires again, and the other buckets
        // stay frozen for good. Achievements declaring DifficultyRequirement.Any read bucket 1, so
        // they report as locked no matter how much more the player does. This is what un-learns
        // The Survivor, Comfort is King, Mighty Halls and The Architect after a 1.0 relog, and
        // playing more cannot undo it.
        //
        // Only buckets 0 and 1 are touched here. They are the difficulty agnostic ones that vanilla
        // already intends to hold the unconditional totals, so raising them to the best value the
        // game itself recorded is a repair. The per-difficulty buckets are deliberately left alone:
        // those encode that something was achieved *on that difficulty*, and writing them would be
        // granting an achievement rather than restoring one.
        private const int RawStatsBucket = 0;
        private const int AnyBucket = 1;

        /// <summary>
        /// The stats maintained by the read-one-bucket, write-three-buckets idiom, and therefore the
        /// only ones that can latch. Running totals go through IncrementStat, which writes every
        /// bucket it should unconditionally, so they stay consistent and are left out.
        /// </summary>
        private static IEnumerable<PlayerStatType> LatchedStats()
        {
            yield return PlayerStatType.ConsecutiveDaysSurvivedMax;
            yield return PlayerStatType.MaxComfort;
            yield return PlayerStatType.MaxBuildingHeight;
            yield return PlayerStatType.MaxBuildingHeightWorld;
            // Piece.CheckClusteredBuildPieceStats guards every build tag the same way. Walking the
            // enum by name rather than the literal 171 the game uses keeps this correct if Iron
            // Gate inserts entries ahead of the block.
            for (int i = (int)PlayerStatType.BuildClusterMisc; i <= (int)PlayerStatType.BuildClusterSeasonal; i++)
            {
                yield return (PlayerStatType)i;
            }
        }

        /// <summary>
        /// Re-derives everything parsed out of a config string into a cache. Called from Awake and
        /// again whenever the config is reloaded, so a hotkey reload leaves no stale cache behind.
        /// The per-call parsers used by the dungeon pin overrides are not here: they already
        /// re-parse whenever their source string changes.
        /// </summary>
        private static void RebuildDerivedConfig()
        {
            _smelterPriority = SmelterInputPriority.Value.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            _cookingPriority = CookingStationInputPriority.Value.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            _favoriteFoods = FavoriteFoodList.Value.Split(',').ToList();
            _favoriteAmmo = FavoriteAmmoList.Value.Split(',').ToList();
        }

        private static void ToggleHealthFloor()
        {
            _healthFloorEnabled = !_healthFloorEnabled;
            string message;
            if (!_healthFloorEnabled)
            {
                message = "Health floor OFF - you can die";
            }
            else if (MinHealthPercent.Value > 0f)
            {
                message = $"Health floor ON - {MinHealthPercent.Value:P0} of max health";
            }
            else
            {
                // Toggling on means nothing while the configured floor is zero, and silently doing
                // nothing is exactly the case where a surprise death happens
                message = "Health floor ON, but MinHealthPercent is 0 - no protection";
            }
            Debug.Log(message);
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }

        private void ReloadModConfig()
        {
            // Suppression only prefixes Character.ShowPickupMessage and friends, so a direct
            // ShowMessage is not affected by those settings
            try
            {
                Config.Reload();
                // Diagnostics that report once per thing are re-armed here, so a report can be
                // retriggered from in-game instead of by restarting
                _reportedGhosts.Clear();
                _ghostParticlesReported = false;
                Debug.Log($"PipsMod config reloaded from {Config.ConfigFilePath}");
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "PipsMod config reloaded");
            }
            catch (Exception ex)
            {
                // A malformed edit should report itself rather than take the frame down
                Debug.LogError($"PipsMod config reload failed, keeping the values already loaded - {ex.Message}");
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "PipsMod config reload FAILED - see log");
            }
        }

        private static void RepairStatBuckets()
        {
            Game game = Game.instance;
            PlayerProfile profile = game != null ? game.GetPlayerProfile() : null;
            if (profile == null || profile.m_playerStats == null)
            {
                Debug.LogWarning("Stat bucket repair needs a loaded world - no player profile available");
                return;
            }
            bool apply = StatBucketRepairApply.Value;
            Debug.Log(apply
                ? "Stat bucket repair: APPLYING changes to the character profile"
                : "Stat bucket repair: dry run, nothing will be written (set StatBucketRepairApply to true to apply)");
            int changes = 0;
            foreach (PlayerStatType stat in LatchedStats())
            {
                // The best value the game itself banked anywhere, including the per-difficulty
                // buckets - that is the number the player actually reached
                float best = 0f;
                bool found = false;
                for (int b = 0; b < profile.m_playerStats.Length; b++)
                {
                    var bucket = profile.m_playerStats[b];
                    if (bucket?.m_stats != null && bucket.m_stats.TryGetValue(stat, out float value) && (!found || value > best))
                    {
                        best = value;
                        found = true;
                    }
                }
                if (!found)
                {
                    continue;
                }
                foreach (int target in new[] { RawStatsBucket, AnyBucket })
                {
                    if (target >= profile.m_playerStats.Length)
                    {
                        continue;
                    }
                    var bucket = profile.m_playerStats[target];
                    if (bucket?.m_stats == null)
                    {
                        continue;
                    }
                    bucket.m_stats.TryGetValue(stat, out float current);
                    if (current >= best)
                    {
                        continue;
                    }
                    Debug.Log($"  {(apply ? "repair" : "would repair")} {stat} in {(DifficultyRequirement)target}: {current:0.##} -> {best:0.##}");
                    if (apply)
                    {
                        bucket.m_stats[stat] = best;
                    }
                    changes++;
                }
            }
            if (changes == 0)
            {
                Debug.Log("Stat bucket repair: nothing to do, buckets 0 and 1 already hold the best recorded values");
            }
            else if (apply)
            {
                Debug.Log($"Stat bucket repair: {changes} value(s) raised. Log out normally so the profile is saved, then the achievement panel recomputes on next load.");
            }
            else
            {
                Debug.Log($"Stat bucket repair: {changes} value(s) would be raised.");
            }
        }

        private static HashSet<PlayerStatType> _latchedStatSet;

        private static bool IsLatchedStat(PlayerStatType stat)
        {
            if (_latchedStatSet == null)
            {
                _latchedStatSet = new HashSet<PlayerStatType>(LatchedStats());
            }
            return _latchedStatSet.Contains(stat);
        }

        /// <summary>
        /// Keeps the vanilla bucket bug from destroying a high-water mark.
        ///
        /// PlayerProfile.SetStat assigns rather than raises, and writes buckets 0, 1 and the one for
        /// the world's current combat difficulty. The callers that maintain a maximum decide whether
        /// to write by reading GetStat, which only looks at the *current* bucket:
        ///
        ///     if (GetStat(ConsecutiveDaysSurvived) > GetStat(ConsecutiveDaysSurvivedMax))
        ///         SetStat(ConsecutiveDaysSurvivedMax, ...);
        ///
        /// Change the world's combat difficulty and the current bucket becomes one with little or
        /// nothing in it, so that comparison starts passing against a value of zero and the small
        /// new number is written over the real one in buckets 0 and 1 - the buckets achievements
        /// actually read. A 124 day record is replaced by 1 the first day after a death.
        ///
        /// Only the stats in LatchedStats are guarded, and only against going *down*. The streak
        /// counter itself is not in that set, so Player.OnDeath zeroing ConsecutiveDaysSurvived
        /// still works exactly as the game intends.
        ///
        /// The write is skipped rather than rewritten to the larger value: skipping leaves buckets
        /// 0 and 1 intact without putting anything into a per-difficulty bucket the player has not
        /// earned it on.
        /// </summary>
        [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.SetStat), new[] { typeof(PlayerStatType), typeof(float), typeof(bool) })]
        class PlayerProfile_SetStat_Patch
        {
            static bool Prefix(PlayerProfile __instance, PlayerStatType stat, float amount)
            {
                if (!PreventStatRegression.Value || __instance?.m_playerStats == null || !IsLatchedStat(stat))
                {
                    return true;
                }
                float best = 0f;
                bool found = false;
                for (int i = 0; i < __instance.m_playerStats.Length; i++)
                {
                    var bucket = __instance.m_playerStats[i];
                    if (bucket?.m_stats != null && bucket.m_stats.TryGetValue(stat, out float value) && (!found || value > best))
                    {
                        best = value;
                        found = true;
                    }
                }
                if (!found || amount >= best)
                {
                    return true;
                }
                ReportStatRegression(stat, amount, best);
                return false;
            }
        }

        /// <summary>
        /// One line per stat per in-game day, with however many were swallowed in between.
        ///
        /// The block itself fires as often as the game writes the stat, which for something written
        /// every few seconds filled the log and buried everything else. The first occurrence of a
        /// given stat on a given day is the informative one; the rest only matter as a count.
        /// </summary>
        private static readonly Dictionary<PlayerStatType, int> _statRegressionDay =
            new Dictionary<PlayerStatType, int>();
        private static readonly Dictionary<PlayerStatType, int> _statRegressionSuppressed =
            new Dictionary<PlayerStatType, int>();

        private static void ReportStatRegression(PlayerStatType stat, float amount, float best)
        {
            // Falls back to a single bucket before the world clock exists, which only means the
            // first few lines of a session share a day number
            int day = EnvMan.instance != null && ZNet.instance != null ? EnvMan.instance.GetDay() : -1;
            if (_statRegressionDay.TryGetValue(stat, out int lastDay) && lastDay == day)
            {
                _statRegressionSuppressed.TryGetValue(stat, out int count);
                _statRegressionSuppressed[stat] = count + 1;
                return;
            }
            _statRegressionSuppressed.TryGetValue(stat, out int suppressed);
            _statRegressionSuppressed[stat] = 0;
            _statRegressionDay[stat] = day;
            string tail = suppressed > 0 ? $" ({suppressed} more since the last report)" : string.Empty;
            Debug.Log($"Blocked stat regression on day {day}: {stat} would have been set to "
                + $"{amount:0.##}, keeping {best:0.##}{tail}");
        }

        // The in-game console keeps only a handful of lines and cannot be scrolled, so any command
        // that prints more than that - 'achievements' lists every unlocked and locked entry - is
        // unreadable. Mirroring it into the BepInEx log makes the whole output recoverable.
        //
        // The guard is load bearing. Terminal routes Unity log messages into the console itself
        // (Console.instance.AddString("Log", ...)), and that four argument overload finishes by
        // calling this one argument overload. Logging from here unguarded would therefore be
        // Debug.Log -> Terminal's log hook -> AddString(4) -> AddString(1) -> Debug.Log, forever.
        private static bool _mirroringConsole;

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), new[] { typeof(string) })]
        class Terminal_AddString_Patch
        {
            static void Postfix(string text)
            {
                if (_mirroringConsole || !LogConsoleOutput.Value || string.IsNullOrEmpty(text))
                {
                    return;
                }
                _mirroringConsole = true;
                try
                {
                    Debug.Log("[console] " + text);
                }
                finally
                {
                    _mirroringConsole = false;
                }
            }
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
        private static bool _lockedPowersReported;
        private static List<BossPower> _bossPowers;

        private class BossPower
        {
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
        private static void ScanBossPowers()
        {
            if (_bossPowers != null)
            {
                return;
            }
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || scene.m_prefabs == null)
            {
                return;
            }
            var found = new List<BossPower>();
            foreach (GameObject prefab in scene.m_prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }
                // Shipped prefabs carry BossStone on the root, but search children as well so a
                // nested or modded layout still resolves
                BossStone stone = prefab.GetComponentInChildren<BossStone>(true);
                if (stone == null || stone.m_itemStand == null)
                {
                    continue;
                }
                StatusEffect power = stone.m_itemStand.m_guardianPower;
                if (power == null)
                {
                    continue;
                }
                found.Add(new BossPower
                {
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

        // ---------------- Hoe: clearing rock ----------------
        // The hoe only ever edits the heightmap. A boulder is a separate object standing on that
        // terrain, so levelling under it changes nothing about the boulder and it ends up poking
        // out of flat ground. Removing it is therefore a different operation, not a tweak to the
        // levelling maths, which is why this is kept entirely separate from the terrain patches.
        //
        // Destructible.m_destructibleType would have been the obvious filter, but the enum only has
        // None/Default/Tree/Character - a rock is Default, exactly like a crate or a barrel. So the
        // filter is by name, and the guards around it matter more than the list does:
        //
        //   - must have a Destructible, so it is something the game already considers breakable
        //   - must NOT have a Piece, which is what every player-built object carries
        //   - must NOT be a Character
        //   - MineRock5 is deliberately not handled: that is the mineable-deposit type, and a tool
        //     aimed at scenery should not be able to delete an ore vein
        //
        // DestroyNow is vanilla's own entry point. It checks ownership and then runs the normal
        // destruction path, so effects and drops happen exactly as if the rock had been mined.
        private static string[] _rockNames;
        private static string _rockNamesSource;
        private static string[] _rockExcludes;
        private static string _rockExcludesSource;
        private static readonly HashSet<string> _clearedRockNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _reportedTerrainOps =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static string[] ParseFragments(string raw, ref string[] cache, ref string cacheSource)
        {
            raw = raw ?? string.Empty;
            if (cache == null || cacheSource != raw)
            {
                cacheSource = raw;
                cache = raw.Split(',')
                    .Select(f => f.Trim())
                    .Where(f => f.Length > 0)
                    .ToArray();
            }
            return cache;
        }

        /// <summary>
        /// The prefab a world object was spawned from, which is not always what its GameObject is
        /// called. A MineRock5 renames itself to "___MineRock5 m_meshFilter" once it has merged its
        /// meshes, so matching on the GameObject name silently failed for every mineable rock.
        ///
        /// The ZDO still carries the prefab hash, and ZNetScene can turn that back into the real
        /// prefab, which is the name the config is written against.
        /// </summary>
        private static string ResolvePrefabName(GameObject candidate)
        {
            string fallback = StripClone(candidate.name);
            ZNetView view = candidate.GetComponentInParent<ZNetView>();
            ZDO zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            if (zdo == null || ZNetScene.instance == null)
            {
                return fallback;
            }
            GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
            return prefab != null ? StripClone(prefab.name) : fallback;
        }

        private static bool IsClearableRock(GameObject candidate)
        {
            string name = ResolvePrefabName(candidate);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            string[] excludes = ParseFragments(HoeLevelRockExclude.Value, ref _rockExcludes, ref _rockExcludesSource);
            foreach (string fragment in excludes)
            {
                if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }
            string[] includes = ParseFragments(HoeLevelRockNames.Value, ref _rockNames, ref _rockNamesSource);
            foreach (string fragment in includes)
            {
                if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Walks the full ancestor chain of the collider nearest the cursor and names every
        /// component on each level.
        ///
        /// Three rounds have gone to guessing which component makes a rock breakable - Destructible,
        /// then MineRock5, then MineRock - and each guess produced a diagnostic that could only see
        /// the type it was already looking for. This asks the object what it is instead.
        /// </summary>
        private static string DescribeNearestHierarchy(Collider[] hits, Vector3 centre)
        {
            Collider closest = null;
            float bestDistance = float.MaxValue;
            foreach (Collider collider in hits)
            {
                if (collider == null)
                {
                    continue;
                }
                Vector3 flat = collider.bounds.ClosestPoint(centre) - centre;
                flat.y = 0f;
                if (flat.magnitude < bestDistance)
                {
                    bestDistance = flat.magnitude;
                    closest = collider;
                }
            }
            if (closest == null)
            {
                return "Nearest collider: none";
            }

            var chain = new List<string>();
            Transform node = closest.transform;
            int depth = 0;
            while (node != null && depth < 8)
            {
                var components = new List<string>();
                foreach (Component component in node.GetComponents<Component>())
                {
                    if (component != null)
                    {
                        components.Add(component.GetType().Name);
                    }
                }
                chain.Add($"{StripClone(node.name)}({string.Join("+", components.Distinct().ToArray())})");
                node = node.parent;
                depth++;
            }
            return $"Nearest collider at {bestDistance:0.#}m, hierarchy child->root: "
                + string.Join(" <- ", chain.ToArray());
        }

        // Reused between swings. OverlapSphere allocates a fresh array every call, and with a 4x
        // radius this scan sees several hundred colliders each time the hoe is used.
        private static readonly Collider[] _rockScanBuffer = new Collider[1024];

        private sealed class RockCandidate
        {
            public GameObject Owner;
            public float Distance;
            public Destructible AsDestructible;
            public MineRock5 AsMineable;
            public MineRock AsLegacy;
            public List<Collider> LegacyAreas;
        }

        private static void ClearRocksAround(Vector3 centre, float radius)
        {
            if (radius <= 0f)
            {
                return;
            }
            bool verbose = PinDiagnostics.Value;

            // The sphere is widened because a rock is judged by its extent, not its origin, and a
            // large one can reach into the circle from well outside it
            int hitCount = Physics.OverlapSphereNonAlloc(centre, radius + 16f, _rockScanBuffer);

            // One upward walk per collider, not three. A rock's components all sit on the same
            // object as its ZNetView, so finding that once and reading the three types off it
            // replaces three separate GetComponentInParent searches - and a big rock contributes
            // a dozen or more colliders, all of which resolve to the same object.
            var byOwner = new Dictionary<GameObject, RockCandidate>();
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _rockScanBuffer[i];
                if (collider == null)
                {
                    continue;
                }
                Vector3 flat = collider.bounds.ClosestPoint(centre) - centre;
                flat.y = 0f;
                float distance = flat.magnitude;

                ZNetView view = collider.GetComponentInParent<ZNetView>();
                GameObject owner = view != null ? view.gameObject : null;
                if (owner == null)
                {
                    // Static scenery and terrain have no ZNetView and cannot be broken at all
                    continue;
                }

                if (!byOwner.TryGetValue(owner, out RockCandidate candidate))
                {
                    candidate = new RockCandidate
                    {
                        Owner = owner,
                        Distance = distance,
                        AsDestructible = owner.GetComponent<Destructible>(),
                        AsMineable = owner.GetComponent<MineRock5>(),
                        AsLegacy = owner.GetComponent<MineRock>(),
                    };
                    byOwner[owner] = candidate;
                }
                else if (distance < candidate.Distance)
                {
                    candidate.Distance = distance;
                }

                // The legacy MineRock has no area-hit path: Damage resolves which area was struck
                // from hit.m_hitCollider and refuses the hit without one, so its colliders are kept
                if (candidate.AsLegacy != null && distance <= radius)
                {
                    if (candidate.LegacyAreas == null)
                    {
                        candidate.LegacyAreas = new List<Collider>();
                    }
                    candidate.LegacyAreas.Add(collider);
                }
            }

            var destroyed = new List<string>();
            var reported = new List<KeyValuePair<float, string>>();

            foreach (RockCandidate candidate in byOwner.Values)
            {
                GameObject go = candidate.Owner;
                if (go == null)
                {
                    continue;
                }
                bool breakable = candidate.AsDestructible != null || candidate.AsMineable != null
                    || candidate.AsLegacy != null;
                if (!breakable)
                {
                    continue;
                }

                // The ZDO's prefab hash, not the GameObject name: a MineRock5 renames itself to
                // "___MineRock5 m_meshFilter" once it has merged its meshes
                string name = ResolvePrefabName(go);
                bool matches = IsClearableRock(go);
                bool inRange = candidate.Distance <= radius;
                bool isPiece = go.GetComponentInParent<Piece>() != null;
                bool isCharacter = go.GetComponentInParent<Character>() != null;

                if (verbose)
                {
                    string kind = candidate.AsMineable != null ? "mineable"
                        : candidate.AsLegacy != null ? $"minerock,{(candidate.LegacyAreas?.Count ?? 0)} area(s)"
                        : "destructible";
                    reported.Add(new KeyValuePair<float, string>(candidate.Distance,
                        $"{name}<{kind}>[{candidate.Distance:0.#}m"
                        + (isPiece ? ",piece" : "") + (isCharacter ? ",character" : "")
                        + (matches ? ",matches" : ",noMatch") + (inRange ? "" : ",outOfRange") + "]"));
                }

                if (!matches || !inRange || isPiece || isCharacter)
                {
                    continue;
                }

                if (candidate.AsDestructible != null)
                {
                    candidate.AsDestructible.DestroyNow();
                }
                else if (candidate.AsMineable != null)
                {
                    // Vanilla's own recipe for forcing a mined area to give way, from
                    // MineRock5.CheckSupport: tool tier 100 clears m_minToolTier, and Structural is
                    // the hit type it uses for rock collapsing under its own weight. m_radius makes
                    // it an area hit, so only the part standing in the circle is removed.
                    var hit = new HitData();
                    hit.m_damage.m_damage = 1E+09f;
                    hit.m_point = centre;
                    hit.m_radius = radius;
                    hit.m_toolTier = 100;
                    hit.m_hitType = HitData.HitType.Structural;
                    candidate.AsMineable.Damage(hit);
                }
                else if (candidate.LegacyAreas != null)
                {
                    foreach (Collider area in candidate.LegacyAreas)
                    {
                        if (area == null)
                        {
                            continue;
                        }
                        var legacyHit = new HitData();
                        legacyHit.m_damage.m_damage = 1E+09f;
                        legacyHit.m_point = area.bounds.center;
                        legacyHit.m_hitCollider = area;
                        legacyHit.m_toolTier = 100;
                        legacyHit.m_hitType = HitData.HitType.Structural;
                        candidate.AsLegacy.Damage(legacyHit);
                    }
                }
                else
                {
                    continue;
                }

                destroyed.Add(name);
                _clearedRockNames.Add(name);
            }

            if (destroyed.Count > 0)
            {
                // Named rather than counted, because a wrong fragment in the config shows up here as
                // something unexpected being broken, and that is worth noticing immediately
                Debug.Log($"Hoe cleared {destroyed.Count} object(s) while levelling: "
                    + string.Join(", ", destroyed.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray()));
            }
            else if (verbose)
            {
                // Nearest first: this list is truncated, and sorting it any other way has twice
                // hidden the one object the whole scan was about
                Debug.Log($"Hoe cleared nothing at X {centre.x:0} Z {centre.z:0} radius {radius:0.##}. "
                    + $"Colliders scanned: {hitCount}. Breakables seen: "
                    + (reported.Count == 0
                        ? "none"
                        : string.Join(", ", reported.OrderBy(e => e.Key).Select(e => e.Value).Distinct().Take(12).ToArray())));
            }
        }

        // TerrainOp.Awake is the whole operation - it finds the heightmaps, applies itself and then
        // destroys its own GameObject - so a postfix here runs once per placed operation with the
        // final position and radius already settled.
        //
        // Identified by prefab name rather than by comparing settings: the placed instance carries
        // its own copy of Settings, not the prefab's, so a reference comparison would never match.
        [HarmonyPatch(typeof(TerrainOp), "Awake")]
        class TerrainOp_Awake_ClearRocks_Patch
        {
            static void Postfix(TerrainOp __instance)
            {
                if (__instance == null)
                {
                    return;
                }
                string prefabName = Utils.GetPrefabName(__instance.gameObject.name);
                bool wanted = string.Equals(prefabName, HoeLevelOpPrefab.Value, StringComparison.OrdinalIgnoreCase);

                // Reported before every bailout and once per distinct operation, so "nothing
                // happened" can be told apart from "this never ran". Two rounds have already gone
                // to a rock-clearing report that only printed on success.
                if (_reportedTerrainOps.Add(prefabName))
                {
                    Debug.Log($"Terrain op placed: '{prefabName}' radius={__instance.GetRadius():0.##} "
                        + $"(wantedOp={wanted}, clearsRocks={HoeLevelClearsRocks.Value}, "
                        + $"shared={IsSharedSession()}, ghostFlag={TerrainOp.m_forceDisableTerrainOps})");
                }

                if (!HoeLevelClearsRocks.Value || !wanted || IsSharedSession()
                    || TerrainOp.m_forceDisableTerrainOps)
                {
                    return;   // the last check keeps the placement ghost from clearing anything
                }
                ClearRocksAround(__instance.transform.position, __instance.GetRadius());
            }
        }

        // ---------------- Sneak ----------------
        // Vanilla's whole stealth calculation is these few lines in Player.UpdateStealth:
        //
        //     float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Sneak);
        //     float lightFactor = StealthSystem.instance.GetLightFactor(GetCenterPoint());
        //     m_stealthFactorTarget = Mathf.Lerp(0.5f + lightFactor * 0.5f, 0.2f + lightFactor * 0.4f, skillFactor);
        //     ...
        //     float num = Mathf.MoveTowards(m_stealthFactor, m_stealthFactorTarget, dt / 4f);
        //
        // The result multiplies the creature's view range in BaseAI.CanSeeTarget and its alert
        // range in MonsterAI, so it is a straight scale on how close you can get.
        //
        // Three things change here, and the method is reimplemented rather than transpiled: all
        // three live in different parts of one short method, and an IL rewrite of three separate
        // constants would be far harder to read than the method itself.
        //
        // Everything not being changed is kept exactly, including m_lastStealthPosition, the
        // SEMan.ModifyStealth hook that lets status effects adjust stealth, both Clamp01 calls, the
        // 0.5s target recalculation interval, and only writing the ZDO when the value actually
        // moved. That last one matters - the ZDO is what every other client and the AI read.
        // Non-public in the shipped assembly; shared by the sneak, run and jump tweaks
        private static readonly FieldInfo PlayerSkillsField = AccessTools.Field(typeof(Player), "m_skills");

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateStealth))]
        class Player_UpdateStealth_Patch
        {
            static bool Prefix(Player __instance, float dt,
                ref float ___m_stealthFactorUpdateTimer, ref float ___m_stealthFactorTarget,
                ref float ___m_stealthFactor, ref Vector3 ___m_lastStealthPosition,
                ZNetView ___m_nview, SEMan ___m_seman)
            {
                if (!SneakTweaks.Value || __instance == null || ___m_nview == null
                    || !ReferenceEquals(__instance, Player.m_localPlayer) || IsSharedSession())
                {
                    return true;   // vanilla, including its light handling
                }
                Skills skills = PlayerSkillsField?.GetValue(__instance) as Skills;
                if (skills == null)
                {
                    return true;
                }

                ___m_stealthFactorUpdateTimer += dt;
                if (___m_stealthFactorUpdateTimer > 0.5f)
                {
                    ___m_stealthFactorUpdateTimer = 0f;
                    ___m_stealthFactorTarget = 0f;
                    if (__instance.IsCrouching())
                    {
                        ___m_lastStealthPosition = __instance.transform.position;
                        float skillFactor = Mathf.Clamp01(skills.GetSkillFactor(Skills.SkillType.Sneak));
                        // No light term. Vanilla adds up to lightFactor * 0.5 here, which is what
                        // makes a torch - or a passive Wisplight - undo sneaking at any skill.
                        ___m_stealthFactorTarget = Mathf.Lerp(
                            SneakStealthAtMinSkill.Value, SneakStealthAtMaxSkill.Value, skillFactor);
                        ___m_stealthFactorTarget = Mathf.Clamp01(___m_stealthFactorTarget);
                        if (___m_seman != null)
                        {
                            ___m_seman.ModifyStealth(___m_stealthFactorTarget, ref ___m_stealthFactorTarget);
                        }
                        ___m_stealthFactorTarget = Mathf.Clamp01(___m_stealthFactorTarget);
                    }
                    else
                    {
                        ___m_stealthFactorTarget = 1f;
                    }
                }

                // Vanilla's dt / 4f is a rate of one unit per four seconds; this is the same
                // expressed as a configurable duration for the full range
                float seconds = Mathf.Max(0.1f, SneakTransitionSeconds.Value);
                float moved = Mathf.MoveTowards(___m_stealthFactor, ___m_stealthFactorTarget, dt / seconds);
                if (!___m_stealthFactor.Equals(moved))
                {
                    ___m_nview.GetZDO().Set(ZDOVars.s_stealth, moved);
                }
                ___m_stealthFactor = moved;
                return false;
            }
        }

        // Crouching sets a flat speed in Character.UpdateWalking:
        //
        //     else if (flag || IsEncumbered()) { speed = m_crouchSpeed; }
        //
        // where flag is IsCrouching(). m_crouchSpeed has exactly one reader - that line - so it can
        // be scaled in place for the duration of the call without anything else observing the
        // altered value, the same approach the swim speed change uses. A finalizer restores it, so
        // an exception mid-frame cannot bake the change into the character.
        //
        // The value is interpolated towards the jog speed that the uncrouched branch would have
        // used, which is what makes 0 mean "no penalty" rather than some arbitrary multiple.
        //
        // Only applied while actually crouching. That same branch also catches being encumbered
        // while standing, and an over-encumbered player is meant to be slow.
        private static readonly MethodInfo JogSpeedFactorMethod =
            AccessTools.Method(typeof(Character), "GetJogSpeedFactor");

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateWalking))]
        class Character_UpdateWalking_SneakSpeed_Patch
        {
            static void Prefix(Character __instance, ref float __state)
            {
                __state = 0f;
                if (__instance == null || !ReferenceEquals(__instance, Player.m_localPlayer)
                    || SneakSpeedPenalty.Value >= 1f || !__instance.IsCrouching() || IsSharedSession())
                {
                    return;
                }
                // GetJogSpeedFactor is non-public in the shipped assembly; a failed lookup falls
                // back to the unmodified jog speed rather than skipping the feature
                float jogFactor = JogSpeedFactorMethod != null
                    ? (float)JogSpeedFactorMethod.Invoke(__instance, null)
                    : 1f;
                float jogSpeed = __instance.m_speed * jogFactor;
                if (jogSpeed <= 0f)
                {
                    return;
                }
                __state = __instance.m_crouchSpeed;
                __instance.m_crouchSpeed = Mathf.Lerp(jogSpeed, __state, Mathf.Clamp01(SneakSpeedPenalty.Value));
            }

            static void Finalizer(Character __instance, float __state)
            {
                if (__state > 0f && __instance != null)
                {
                    __instance.m_crouchSpeed = __state;
                }
            }
        }

        // ---------------- Run and jump skill scaling ----------------
        // Both of these are a single constant in vanilla.
        //
        //     Player.GetRunSpeedFactor:  (1f + skillFactor * 0.25f) * (1f + equipment * 1.5f)
        //     Character.Jump:            float num2 = 1f + num * 0.4f;
        //
        // Neither is reimplemented. The run one is rescaled by the ratio between the wanted bonus
        // and vanilla's, which leaves the equipment term - and anything Iron Gate adds to that
        // expression later - exactly as it was. The jump one multiplies the two force fields by the
        // same ratio, which reproduces the effect precisely because num2 is applied to both and to
        // nothing else.
        private static float SkillBonusRatio(float wanted, float vanilla, float skillFactor)
        {
            float from = 1f + skillFactor * vanilla;
            float to = 1f + skillFactor * wanted;
            return from > 0f ? to / from : 1f;
        }

        private static float LocalPlayerSkillFactor(Character character, Skills.SkillType skill)
        {
            if (character == null || !ReferenceEquals(character, Player.m_localPlayer) || IsSharedSession())
            {
                return -1f;   // not ours to touch
            }
            var skills = PlayerSkillsField?.GetValue(character) as Skills;
            return skills == null ? -1f : Mathf.Clamp01(skills.GetSkillFactor(skill));
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetRunSpeedFactor))]
        class Player_GetRunSpeedFactor_Patch
        {
            static void Postfix(Player __instance, ref float __result)
            {
                if (Mathf.Approximately(RunSpeedSkillBonus.Value, 0.25f))
                {
                    return;
                }
                float skillFactor = LocalPlayerSkillFactor(__instance, Skills.SkillType.Run);
                if (skillFactor < 0f)
                {
                    return;
                }
                __result *= SkillBonusRatio(RunSpeedSkillBonus.Value, 0.25f, skillFactor);
            }
        }

        // m_jumpForce and m_jumpForceForward are read in Character.Jump and nowhere else, so
        // scaling them for the duration of the call is safe. A finalizer restores them, so a throw
        // mid-jump cannot leave a character permanently springy.
        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        class Character_Jump_SkillBonus_Patch
        {
            static void Prefix(Character __instance, ref float[] __state)
            {
                __state = null;
                if (Mathf.Approximately(JumpPowerSkillBonus.Value, 0.4f))
                {
                    return;
                }
                float skillFactor = LocalPlayerSkillFactor(__instance, Skills.SkillType.Jump);
                if (skillFactor < 0f)
                {
                    return;
                }
                float ratio = SkillBonusRatio(JumpPowerSkillBonus.Value, 0.4f, skillFactor);
                if (Mathf.Approximately(ratio, 1f))
                {
                    return;
                }
                __state = new[] { __instance.m_jumpForce, __instance.m_jumpForceForward };
                __instance.m_jumpForce *= ratio;
                __instance.m_jumpForceForward *= ratio;
            }

            static void Finalizer(Character __instance, float[] __state)
            {
                if (__state != null && __instance != null)
                {
                    __instance.m_jumpForce = __state[0];
                    __instance.m_jumpForceForward = __state[1];
                }
            }
        }

        // ---------------- Area repair ----------------
        // Player.Repair only ever touches GetHoveringPiece(), so this runs afterwards and repeats
        // the same work for everything else in range. Every rule vanilla applies to a single repair
        // is applied here too, piece by piece, rather than reimplemented loosely:
        //
        //   CheckCanRemovePiece    the crafting-station requirement, which is what keeps a dungeon
        //                          hammer-proof and another player's work theirs
        //   PrivateArea.CheckAccess ward permission
        //   HaveStamina/UseStamina  the same per-piece cost GetBuildStamina gives the single case
        //   durability drain        the same per-piece drain
        //
        // A postfix rather than a prefix, so the piece under the cursor is handled by vanilla with
        // its normal message and effects. WearNTear.Repair refuses twice within a second, so that
        // piece cannot be charged for again when the sweep reaches it.
        //
        // Not suspended in a shared session: this spends real stamina and durability and obeys
        // ward and station rules, so it is a convenience rather than an advantage - the only thing
        // it changes is how many clicks the same work takes.
        // All three are non-public in the shipped assembly despite the publicized reference
        // showing them public - the access audit caught every one
        private static readonly FieldInfo WearInstancesField =
            AccessTools.Field(typeof(WearNTear), "s_allInstances");
        private static readonly MethodInfo BuildStaminaMethod =
            AccessTools.Method(typeof(Player), "GetBuildStamina");
        private static readonly MethodInfo CanRemovePieceMethod =
            AccessTools.Method(typeof(Player), "CheckCanRemovePiece");

        private static void AreaRepair(Player player, ItemDrop.ItemData toolItem)
        {
            float radius = AreaRepairRadius.Value;
            var instances = WearInstancesField?.GetValue(null) as List<WearNTear>;
            if (BuildStaminaMethod == null || CanRemovePieceMethod == null)
            {
                return;
            }
            if (radius <= 0f || toolItem == null || instances == null)
            {
                return;
            }

            Vector3 origin = player.transform.position;
            float staminaPerPiece = (float)BuildStaminaMethod.Invoke(player, null);
            float drain = toolItem.m_shared.m_useDurability
                ? toolItem.m_shared.m_useDurabilityDrain * Game.m_durabilityRate
                : 0f;

            int repaired = 0;
            int blocked = 0;
            bool ranOut = false;

            // Indexed rather than foreach: Repair sends an RPC and a piece could in principle leave
            // the list while this runs, and the count is re-read each step
            for (int i = 0; i < instances.Count; i++)
            {
                WearNTear wear = instances[i];
                if (wear == null)
                {
                    continue;
                }
                Vector3 position = wear.transform.position;
                if (Vector3.Distance(origin, position) > radius)
                {
                    continue;
                }
                Piece piece = wear.GetComponent<Piece>();
                if (piece == null)
                {
                    continue;
                }
                bool canRemove = CanRemovePieceMethod.Invoke(player, new object[] { piece }) is bool ok && ok;
                if (!canRemove || !PrivateArea.CheckAccess(position))
                {
                    blocked++;
                    continue;
                }
                if (!player.HaveStamina(staminaPerPiece))
                {
                    ranOut = true;
                    break;
                }
                if (!wear.Repair())
                {
                    continue;   // already at full health, or repaired within the last second
                }

                player.UseStamina(staminaPerPiece);
                repaired++;
                if (drain > 0f)
                {
                    toolItem.m_durability -= drain;
                    if (toolItem.m_durability <= 0f)
                    {
                        ranOut = true;
                        break;
                    }
                }
            }

            // One summary rather than vanilla's per-piece message and effect. A sweep after a raid
            // can touch dozens of pieces, and that many stacked messages and place effects would
            // bury everything else on screen.
            if (repaired > 0)
            {
                string tail = ranOut ? " (stopped - out of stamina or durability)" : string.Empty;
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                    $"Repaired {repaired} piece(s) within {radius:0}m{tail}");
            }
            else if (blocked > 0)
            {
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                    $"Nothing repaired - {blocked} piece(s) need a crafting station or are warded");
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
        class Player_Repair_AreaPatch
        {
            static void Postfix(Player __instance, ItemDrop.ItemData toolItem)
            {
                if (__instance == null || !ReferenceEquals(__instance, Player.m_localPlayer)
                    || AreaRepairRadius.Value <= 0f || !ZInput.GetKey(AreaRepairKey.Value))
                {
                    return;
                }
                AreaRepair(__instance, toolItem);
            }
        }

        // ---------------- Wards as no-spawn areas ----------------
        // Two different things get called a ward's radius and only one of them matters here.
        //
        // PrivateArea.m_radius - the one the hover text and the projected circle show - governs who
        // may build and what may be damaged. No spawn system reads it; grep SpawnSystem, SpawnArea
        // and CreatureSpawner for PrivateArea and the result is empty. Scaling it would widen build
        // protection and nothing else.
        //
        // What suppresses a spawn is an EffectArea carrying the PlayerBase flag, on a collider on
        // the character_trigger layer. That lives on the prefab as Unity asset data, invisible to
        // the decompiled source, which is why this is written to find out at runtime rather than to
        // assume: it resizes the area the ward already has, and only builds one if there is none.
        // The one-shot log line says which happened, so the assumption is never load-bearing.
        //
        // The whole blast radius of that flag is three call sites: SpawnSystem.IsSpawnPointGood,
        // CreatureSpawner (its m_spawnInPlayerBase check) and ItemDrop.IsInsideBase, which keeps
        // dropped items from timing out indoors. The first two are the point; the third is a small
        // bonus and no kind of hazard.
        private static bool _wardAreaReported;

        /// <summary>
        /// Points an existing PlayerBase effect area at a new radius, or builds one if the prefab
        /// carries none. Returns a description of what it did, for the one-shot log.
        /// </summary>
        private static string SetPlayerBaseRadius(GameObject host, float radius)
        {
            var resized = new List<string>();
            foreach (EffectArea existing in host.GetComponentsInChildren<EffectArea>(includeInactive: true))
            {
                if (existing == null || (existing.m_type & EffectArea.Type.PlayerBase) == EffectArea.Type.None)
                {
                    continue;
                }
                // Not EffectArea.GetRadius(), and not m_collider either. Both read a field that
                // Awake fills in, and component Awake order within one Instantiate is not ours to
                // rely on: a child whose Awake has not run yet still has null there, and GetRadius
                // dereferences it unconditionally in its fallback branch. That threw an NRE per
                // ward, out through Instantiate and into ZNetScene.CreateObject.
                Collider collider = existing.GetComponent<Collider>();

                // The same two shapes CraftingStation handles, and no fallback: a collider neither
                // branch fits is not one to guess the right dimension for.
                //
                // Note this scales PlayerBase only even when the area is flagged NoMonsters too.
                // Those bounds were cached in EffectArea.Awake and are not re-read, so the monster
                // herding stays at its authored size while the spawn suppression grows. That split
                // is wanted here - a preserve should stop spawns without shoving tame animals out.
                if (collider is SphereCollider sphere)
                {
                    resized.Add($"{existing.name} sphere {sphere.radius:0.#}m -> {radius:0.#}m");
                    sphere.radius = radius;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    resized.Add($"{existing.name} capsule {capsule.radius:0.#}m -> {radius:0.#}m");
                    capsule.radius = radius;
                }
                else
                {
                    resized.Add($"{existing.name} has a "
                        + $"{(collider != null ? collider.GetType().Name : "missing")} collider - left alone");
                }
            }
            // Every match rather than the first: with Awake order unknown and inactive children in
            // the list, choosing one would be choosing blind, and a prefab carrying two PlayerBase
            // areas wants both moved anyway.
            if (resized.Count > 0)
            {
                return "resized the prefab's own PlayerBase area - " + string.Join("; ", resized.ToArray());
            }

            // Nothing there, so build one. Its own child object on its own layer rather than
            // components on the ward itself: EffectArea claims the collider GetComponent finds and
            // forces isTrigger on it, which on the ward's body would stop it being solid.
            var areaObject = new GameObject("PipModNoSpawnArea");
            areaObject.layer = LayerMask.NameToLayer("character_trigger");
            areaObject.transform.SetParent(host.transform, worldPositionStays: false);
            areaObject.transform.localPosition = Vector3.zero;
            areaObject.transform.localScale = Vector3.one;

            // Collider first: EffectArea.Awake runs the moment the component is added and claims
            // whatever collider is already on the object
            SphereCollider added = areaObject.AddComponent<SphereCollider>();
            added.radius = radius;
            added.isTrigger = true;

            // PlayerBase alone, NoMonsters deliberately left out, for the reason given above
            EffectArea area = areaObject.AddComponent<EffectArea>();
            area.m_type = EffectArea.Type.PlayerBase;
            return $"found no PlayerBase area on the prefab, so added one at {radius:0.#}m";
        }

        [HarmonyPatch(typeof(PrivateArea), "Awake")]
        class PrivateArea_Awake_NoSpawnPatch
        {
            static void Postfix(PrivateArea __instance, ZNetView ___m_nview)
            {
                float radius = WardSpawnBlockRadius.Value;
                if (radius <= 0f || __instance == null)
                {
                    return;
                }
                // The build placement ghost runs Awake with an invalid ZNetView. Without this the
                // ghost would carry a no-spawn sphere around on the cursor, and EffectArea.Awake
                // would have registered it in the global area list to boot.
                if (___m_nview == null || !___m_nview.IsValid())
                {
                    return;
                }
                // Dvergr and Fuling camps use PrivateArea too, with their own faction. Handing
                // those an 80m no-spawn bubble would quietly defuse their camps.
                if (__instance.m_ownerFaction != Character.Faction.Players)
                {
                    return;
                }
                // Caught rather than allowed to propagate: this runs inside PrivateArea.Awake,
                // which runs inside Instantiate, which runs inside ZNetScene.CreateObject's loop
                // over the objects a zone is bringing in. An exception here does not just lose the
                // ward's area, it unwinds out through that loop.
                try
                {
                    string what = SetPlayerBaseRadius(__instance.gameObject, radius);
                    if (!_wardAreaReported)
                    {
                        _wardAreaReported = true;
                        Debug.Log($"Ward no-spawn area ({__instance.gameObject.name}): {what}");
                    }
                }
                catch (Exception e)
                {
                    // Once, not per ward, or a broken assumption buries the log
                    if (!_wardAreaReported)
                    {
                        _wardAreaReported = true;
                        Debug.LogError($"Ward no-spawn area failed on {__instance.gameObject.name}: {e}");
                    }
                }
            }
        }

        /// <summary>
        /// Every loaded effect area with the PlayerBase flag, nearest first, with the radius each
        /// one actually ended up with. This is the coverage map: a gap in it is a place something
        /// can spawn, and the radius column is the only honest way to confirm a scaled station or a
        /// ward is projecting what it was meant to rather than its authored size.
        /// </summary>
        private static int DumpEffectAreas(string dir)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return 0;
            }
            Vector3 origin = player.transform.position;
            var rows = new List<KeyValuePair<float, string>>();
            foreach (EffectArea area in EffectArea.GetAllAreas())
            {
                if (area == null || (area.m_type & EffectArea.Type.PlayerBase) == EffectArea.Type.None)
                {
                    continue;
                }
                Transform root = area.transform.root;
                string name = root != null ? root.name : area.name;
                Vector3 position = area.transform.position;
                float radius = area.GetRadius();
                float distance = Vector3.Distance(origin, position);
                // The area's own object name as well as its root, because that is what separates an
                // area the prefab shipped with from one this mod built - PipModNoSpawnArea
                rows.Add(new KeyValuePair<float, string>(distance,
                    $"{name}\t{area.name}\t{area.m_type}\t{radius:0.#}\t{position.x:0.#}\t{position.z:0.#}\t{position.y:0.#}"
                        + $"\t{distance:0.#}\t{distance - radius:0.#}"));
            }
            var table = new StringBuilder();
            table.AppendLine("Root\tAreaObject\tType\tRadius\tX\tZ\tY\tDistanceFromYou\tYourDistanceBeyondEdge");
            foreach (var row in rows.OrderBy(r => r.Key))
            {
                table.AppendLine(row.Value);
            }
            string areaPath = Path.Combine(dir, $"PipsMod_EffectAreas{PositionSuffix(origin)}.tsv");
            File.WriteAllText(areaPath, table.ToString());
            Debug.Log($"Effect areas -> {Path.GetFileName(areaPath)}");
            return rows.Count;
        }

        // ---------------- Spawn diagnostics ----------------
        // Why a hostile turned up inside what looked like full workbench coverage has exactly two
        // possible answers, and after the fact they are indistinguishable. SpawnSystem's gate is
        //
        //     if (!spawn.m_insidePlayerBase && EffectArea.IsPointInsideArea(spawnPoint, PlayerBase))
        //         return false;
        //
        // so either that entry is exempt by its own flag, or the point it used was genuinely outside
        // every station's sphere. Nothing else gets past it - the gate is tested on every candidate
        // point, including each member of a group spawn.
        //
        // The point it tests is the GROUND point. GetGroundData snaps spawnPoint.y to a terrain
        // raycast, and m_groundOffset - the altitude that puts a deathsquito in the air rather than
        // standing on the dirt - is added afterwards, at the Spawn call. So height never enters into
        // it: a flyer is judged by the ground, or the seabed, underneath it. Which also means
        // FindBaseSpawnPoint's 40-80m ring around the player reaches out to sea from anywhere on a
        // small island, and the seabed out there is under nobody's workbench.
        //
        // Logged from Spawn rather than from the gate because the gate runs up to twenty times per
        // attempt and nearly always refuses; one line per creature that actually appeared is
        // something a person can read.
        private static readonly FieldInfo SpawnSystemInstancesField =
            AccessTools.Field(typeof(SpawnSystem), "m_instances");

        /// <summary>
        /// Signed distance from a point to the nearest PlayerBase effect area - negative inside,
        /// positive outside. Names the piece too, because "32m short of coverage" is only actionable
        /// once you know which workbench to walk over to.
        /// </summary>
        private static float NearestPlayerBaseEdge(Vector3 point, out string areaName, out float areaRadius)
        {
            areaName = null;
            areaRadius = 0f;
            float best = float.MaxValue;
            foreach (EffectArea area in EffectArea.GetAllAreas())
            {
                if (area == null || (area.m_type & EffectArea.Type.PlayerBase) == EffectArea.Type.None)
                {
                    continue;
                }
                float radius = area.GetRadius();
                float edge = Vector3.Distance(area.transform.position, point) - radius;
                if (edge < best)
                {
                    best = edge;
                    Transform root = area.transform.root;
                    areaName = root != null ? root.name : area.name;
                    areaRadius = radius;
                }
            }
            return best;
        }

        [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
        class SpawnSystem_Spawn_DiagnosticsPatch
        {
            static void Postfix(SpawnSystem.SpawnData critter, Vector3 spawnPoint)
            {
                // m_nospawn makes Spawn a no-op and a postfix still runs, so reporting a creature
                // that was never instantiated would be the one way this log could lie
                if (!SpawnDiagnostics.Value || critter == null || critter.m_prefab == null || SpawnSystem.m_nospawn)
                {
                    return;
                }

                // Re-raycast rather than subtracting m_groundOffset back off: the offset carries a
                // random component, so the arithmetic would be close but never exactly the point the
                // gate judged. This is the same call the gate made, at the same XZ.
                Vector3 ground = spawnPoint;
                string biome = "unknown";
                string oceanDepth = "n/a";
                ZoneSystem zones = ZoneSystem.instance;
                if (zones != null)
                {
                    zones.GetGroundData(ref ground, out Vector3 _, out Heightmap.Biome groundBiome,
                        out Heightmap.BiomeArea _, out Heightmap hmap);
                    biome = groundBiome.ToString();
                    if (hmap != null)
                    {
                        oceanDepth = hmap.GetOceanDepth(ground).ToString("0.#");
                    }
                }

                EffectArea inside = EffectArea.IsPointInsideArea(ground, EffectArea.Type.PlayerBase);
                float edge = NearestPlayerBaseEdge(ground, out string nearestName, out float nearestRadius);
                string coverage = nearestName == null
                    ? "no player base loaded"
                    : $"nearest base {nearestName} (radius {nearestRadius:0.#}) "
                        + (edge >= 0f ? $"{edge:0.#}m beyond its edge" : $"{-edge:0.#}m inside it");

                Player player = Player.m_localPlayer;
                string fromPlayer = player != null
                    ? $"{Vector3.Distance(player.transform.position, spawnPoint):0}m from you"
                    : "no local player";

                // Altitude the way the spawn gate measures it: world y minus the 30m water line, so
                // a negative number means the ground point it used is under water
                Debug.Log($"Spawn {critter.m_prefab.name} at X {spawnPoint.x:0} Z {spawnPoint.z:0}"
                    + $" | ground y {ground.y:0.#}, altitude {ground.y - 30f:0.#}, ocean depth {oceanDepth}, biome {biome}"
                    + $" | exempt from player base: {critter.m_insidePlayerBase}"
                    + $" | player base at that point: {(inside != null ? "YES" : "no")}"
                    + $" | {coverage}"
                    + $" | ground offset {critter.m_groundOffset:0.#}+{critter.m_groundOffsetRandom:0.#}"
                    + $" | {fromPlayer}");
            }
        }

        /// <summary>
        /// Every spawn entry the zone the player is standing in can draw from. These live in Unity
        /// asset data, not in the managed assemblies, so there is no way to read them short of asking
        /// the running game - and they are where the answer to "why that creature and not the others"
        /// is written: which entries are exempt from player bases, and which have a minimum altitude
        /// low enough to use an offshore seabed as their ground point.
        /// </summary>
        /// <summary>
        /// Stamps a dump filename with the position it was taken from. Both of these dumps only
        /// see what is loaded, so surveying a large base means several presses from different
        /// spots - and a fixed filename makes each press destroy the last one's evidence.
        /// </summary>
        private static string PositionSuffix(Vector3 position)
        {
            return $"_X{Mathf.RoundToInt(position.x)}_Z{Mathf.RoundToInt(position.z)}";
        }

        private static int DumpSpawnEntries(string dir)
        {
            Player player = Player.m_localPlayer;
            var instances = SpawnSystemInstancesField?.GetValue(null) as List<SpawnSystem>;
            if (player == null || instances == null)
            {
                return 0;
            }

            SpawnSystem nearest = null;
            float bestDistance = float.MaxValue;
            foreach (SpawnSystem system in instances)
            {
                if (system == null)
                {
                    continue;
                }
                float distance = Vector3.Distance(system.transform.position, player.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = system;
                }
            }
            if (nearest == null)
            {
                return 0;
            }

            // The zone's own heightmap, which is what HaveBiome is asked about before an entry is
            // even considered. Reached through FindHeightmap because SpawnSystem.m_heightmap is
            // non-public in the shipped assembly.
            Heightmap here = Heightmap.FindHeightmap(nearest.transform.position);

            var table = new StringBuilder();
            table.AppendLine("Source\tName\tPrefab\tBiome\tBiomeHere\tEnabled\tChance\tInterval\tMaxSpawned"
                + "\tNight\tDay\tExemptFromPlayerBase\tCanSpawnCloseToPlayer\tMinAlt\tMaxAlt\tMinTilt\tMaxTilt"
                + "\tGroundOffset\tGroundOffsetRandom\tMinOceanDepth\tMaxOceanDepth\tInForest\tOutsideForest"
                + "\tInLava\tOutsideLava\tRadiusMin\tRadiusMax\tSpawnDistance\tGroupMin\tGroupMax\tHunt"
                + "\tMinLevel\tMaxLevel\tRequiredKey\tRequiredEvent\tRequiredEnvironments");

            int count = 0;
            foreach (SpawnSystemList list in nearest.m_spawnLists)
            {
                if (list != null)
                {
                    count += AppendSpawnRows(table, list.name, list.m_spawners, here);
                }
            }
            if (here != null)
            {
                foreach (AltBiome alt in here.m_cornerAltBiomes)
                {
                    if (alt != null && alt.m_spawn.Count > 0)
                    {
                        count += AppendSpawnRows(table, "alt:" + alt.m_name, alt.m_spawn, here);
                    }
                }
            }
            // Event spawners only exist while an event is running, so this is usually empty - and
            // when it is not, that is itself the answer to where something came from
            var eventSpawners = RandEventSystem.instance?.GetCurrentSpawners();
            if (eventSpawners != null)
            {
                count += AppendSpawnRows(table, "event", eventSpawners, here);
            }

            string spawnPath = Path.Combine(dir,
                $"PipsMod_SpawnEntries{PositionSuffix(player.transform.position)}.tsv");
            File.WriteAllText(spawnPath, table.ToString());
            Debug.Log($"Spawn entries -> {Path.GetFileName(spawnPath)}");
            return count;
        }

        private static int AppendSpawnRows(StringBuilder table, string source,
            List<SpawnSystem.SpawnData> spawners, Heightmap here)
        {
            int count = 0;
            foreach (SpawnSystem.SpawnData spawn in spawners)
            {
                if (spawn == null || spawn.m_prefab == null)
                {
                    continue;
                }
                table.AppendLine($"{source}\t{spawn.m_name}\t{spawn.m_prefab.name}\t{spawn.m_biome}"
                    + $"\t{(here != null ? here.HaveBiome(spawn.m_biome).ToString() : "unknown")}"
                    + $"\t{spawn.m_enabled && !spawn.m_devDisabled}\t{spawn.m_spawnChance:0.#}\t{spawn.m_spawnInterval:0.#}\t{spawn.m_maxSpawned}"
                    + $"\t{spawn.m_spawnAtNight}\t{spawn.m_spawnAtDay}\t{spawn.m_insidePlayerBase}\t{spawn.m_canSpawnCloseToPlayer}"
                    + $"\t{spawn.m_minAltitude:0.#}\t{spawn.m_maxAltitude:0.#}\t{spawn.m_minTilt:0.#}\t{spawn.m_maxTilt:0.#}"
                    + $"\t{spawn.m_groundOffset:0.#}\t{spawn.m_groundOffsetRandom:0.#}\t{spawn.m_minOceanDepth:0.#}\t{spawn.m_maxOceanDepth:0.#}"
                    + $"\t{spawn.m_inForest}\t{spawn.m_outsideForest}\t{spawn.m_inLava}\t{spawn.m_outsideLava}"
                    + $"\t{spawn.m_spawnRadiusMin:0.#}\t{spawn.m_spawnRadiusMax:0.#}\t{spawn.m_spawnDistance:0.#}"
                    + $"\t{spawn.m_groupSizeMin}\t{spawn.m_groupSizeMax}\t{spawn.m_huntPlayer}"
                    + $"\t{spawn.m_minLevel}\t{spawn.m_maxLevel}\t{spawn.m_requiredGlobalKey}\t{spawn.m_requiredPersistentEvent}"
                    + $"\t{string.Join(",", spawn.m_requiredEnvironments.ToArray())}");
                count++;
            }
            return count;
        }

        // ---------------- No-drops toggle ----------------
        // Not done by setting Game.m_resourceRate to 0, which looks like the obvious lever and is
        // not one: every path that reads it floors the result at one item -
        //
        //     amount = (int)Mathf.Clamp(Mathf.Round(amount * m_resourceRate), 1f, maxStack);
        //
        // so a rate of zero still drops one of everything. The drops have to be suppressed where
        // they are generated instead.
        //
        // DropTable.GetDropList is the single funnel for every destruction drop in the game, but it
        // is emphatically not only that: Container fills a chest from a drop table, and LootSpawner
        // stamps its ZDO with the spawn time whether or not anything came out - so a blanket patch
        // there would quietly empty an unopened crypt chest and burn a loot spawner's timer with
        // nothing to show for it. Permanently, in both cases.
        //
        // So the destruction call sites arm it and the rest do not, the same "identify the owner one
        // level up" shape the perfect-levelling patch uses for SmoothTerrain. A depth counter rather
        // than a bool, because one destruction can cascade into another and a bool would be cleared
        // by the inner finalizer while the outer call was still running.
        //
        // Suspended in a shared session, and this one matters: these RPCs run on whoever owns the
        // object's ZDO, which can be this client while somebody else is swinging the axe. Their wood.
        private static bool _noDropsEnabled;
        private static int _destructionDropDepth;

        private static bool NoDropsActive => _noDropsEnabled && !IsSharedSession();

        private static void ToggleNoDrops()
        {
            _noDropsEnabled = !_noDropsEnabled;
            string message;
            if (!_noDropsEnabled)
            {
                message = "Drops ON";
            }
            else if (IsSharedSession())
            {
                // Toggling something on and having it silently do nothing is worth a word
                message = "No drops ON - but suspended while anyone else is in the world";
            }
            else
            {
                message = "No drops ON - trees, rocks, destructibles and kills leave nothing behind";
            }
            Debug.Log(message);
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, message);
        }

        [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), new[] { typeof(int) })]
        class DropTable_GetDropList_NoDropsPatch
        {
            // The parameterless overload delegates to this one, so patching it covers both
            static void Postfix(List<GameObject> __result)
            {
                if (_destructionDropDepth > 0 && NoDropsActive)
                {
                    __result?.Clear();
                }
            }
        }

        // Trees: the canopy drops here, and the trunk it leaves behind drops its wood from
        // TreeLog.Destroy, so both are needed to make a felled tree leave nothing.
        [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
        class TreeBase_RPC_Damage_NoDropsPatch
        {
            static void Prefix() => _destructionDropDepth++;

            // Finalizers throughout: an exception midway through a destruction must not leave the
            // counter raised, which would suppress every unrelated drop for the rest of the session
            static void Finalizer() => _destructionDropDepth--;
        }

        [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Destroy))]
        class TreeLog_Destroy_NoDropsPatch
        {
            static void Prefix() => _destructionDropDepth++;

            static void Finalizer() => _destructionDropDepth--;
        }

        [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
        class MineRock_RPC_Hit_NoDropsPatch
        {
            static void Prefix() => _destructionDropDepth++;

            static void Finalizer() => _destructionDropDepth--;
        }

        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.DamageArea))]
        class MineRock5_DamageArea_NoDropsPatch
        {
            static void Prefix() => _destructionDropDepth++;

            static void Finalizer() => _destructionDropDepth--;
        }

        // Creatures do not use a DropTable at all - CharacterDrop keeps its own list - so they need
        // separate hooks, and there are two of them because a creature's loot takes one of two
        // routes depending on whether it leaves a corpse:
        //
        //     Character.OnDeath -> m_deathEffects -> Ragdoll.Setup -> SaveLootList   (has a ragdoll)
        //     Character.OnDeath -> m_onDeath      -> CharacterDrop.OnDeath           (does not)
        //
        // and they are mutually exclusive by design: Character.OnDeath calls
        // SetDropsEnabled(false) on the CharacterDrop as soon as it hands a ragdoll the loot, so
        // the m_onDeath path that runs moments later finds m_dropsEnabled already false. Patching
        // only CharacterDrop.OnDeath therefore covers just the minority of creatures that vanish
        // without a corpse - which is exactly the "destructibles work, mobs do not" split.
        //
        // The ragdoll route also explains why the loot is not where the kill was: SaveLootList
        // writes the rolled drops into the ragdoll's own ZDO, and SpawnLoot pays them out later,
        // wherever the body came to rest. Suppressing at SaveLootList means nothing is written in
        // the first place, so there is nothing for SpawnLoot to find.
        //
        // Both hooks sit ahead of GenerateDropList rather than filtering what it returns. That
        // matters: it also advances the pseudo-random counter that gives low-chance drops their
        // bad-luck protection, keyed per prefab in s_pseudoCounter. Letting it run and discarding
        // the result would spend accumulated pity on a kill that dropped nothing. It also means the
        // first-trophy guarantee, which postfixes GenerateDropList, does not fire for a suppressed
        // kill instead of marking a trophy granted while the drop is thrown away.
        private static bool _noDropsCreatureLogged;

        private static bool CreatureDropsSuppressed(CharacterDrop drop, string route)
        {
            if (!NoDropsActive || drop == null)
            {
                return false;
            }
            // RequireComponent(typeof(Character)) puts the character on the same object, and asking
            // for it this way avoids CharacterDrop.m_character - non-public in the shipped assembly,
            // and not assigned until Start has run
            Character character = drop.GetComponent<Character>();

            // The one exception. A boss trophy is a one-per-kill item needed to summon the next
            // boss, and the altar offerings are not farmable - losing those to a toggle left on is
            // a different order of mistake from losing a stack of wood.
            if (character != null && character.IsBoss())
            {
                return false;
            }
            // Once per session, naming the route that actually fired. The first attempt at this
            // hooked the path that almost never runs, and a silent miss looks identical to a
            // feature that is working.
            if (!_noDropsCreatureLogged)
            {
                _noDropsCreatureLogged = true;
                Debug.Log($"No drops: suppressed creature loot via {route} "
                    + $"({(character != null ? character.name : "unknown")})");
            }
            return true;
        }

        [HarmonyPatch(typeof(Ragdoll), "SaveLootList")]
        class Ragdoll_SaveLootList_NoDropsPatch
        {
            static bool Prefix(CharacterDrop characterDrop)
            {
                return !CreatureDropsSuppressed(characterDrop, "Ragdoll.SaveLootList");
            }
        }

        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
        class CharacterDrop_OnDeath_NoDropsPatch
        {
            static bool Prefix(CharacterDrop __instance)
            {
                return !CreatureDropsSuppressed(__instance, "CharacterDrop.OnDeath");
            }
        }

        [HarmonyPatch(typeof(DropOnDestroyed), nameof(DropOnDestroyed.OnDestroyed))]
        class DropOnDestroyed_OnDestroyed_NoDropsPatch
        {
            static void Prefix(DropOnDestroyed __instance, out bool __state)
            {
                // This component sits on player-built pieces as well as on world destructibles -
                // DropOnDestroyed.Awake hooks WearNTear.m_onDestroyed as readily as Destructible's.
                // A raid eating the materials out of your own walls is not what this toggle is for,
                // so anything carrying a Piece keeps its drops.
                __state = __instance != null && __instance.GetComponent<Piece>() == null;
                if (__state)
                {
                    _destructionDropDepth++;
                }
            }

            static void Finalizer(bool __state)
            {
                if (__state)
                {
                    _destructionDropDepth--;
                }
            }
        }

        // ---------------- Destroy item from inventory ----------------
        // InventoryGui.OnSelectedItem is the single funnel every inventory left-click arrives at,
        // including the gamepad and touch paths, so one prefix covers them all.
        //
        // The combo is read from the keyboard here rather than from the Modifier argument the grid
        // works out, because that argument cannot express this: InventoryGrid.OnLeftDown tests
        // Shift before Ctrl and stops at the first hit, so Shift+Ctrl+click arrives indistinguishable
        // from a plain Shift+click - as Modifier.Split. Which is also the reason the confirmation
        // step below is on by default.
        //
        // Scoped to the player's own inventory on purpose. A container is world state that belongs
        // to whatever the chest is attached to, and silently destroying from one would be a real
        // side effect in a shared session; your own pack is nobody else's business.
        private static ItemDrop.ItemData _destroyArmedItem;
        private static float _destroyArmedAt;

        // Non-public in the shipped assembly even though the publicized reference shows it public,
        // so it cannot be called directly without bricking the JIT
        private static readonly MethodInfo UpdateCraftingPanelMethod =
            AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });

        private static bool DestroyComboHeld()
        {
            KeyboardShortcut combo = DestroyItemCombo.Value;
            if (combo.MainKey == KeyCode.None || !ZInput.GetKey(combo.MainKey))
            {
                return false;
            }
            foreach (KeyCode modifier in combo.Modifiers)
            {
                if (!ZInput.GetKey(modifier))
                {
                    return false;
                }
            }
            return true;
        }

        private static string DescribeItem(ItemDrop.ItemData item)
        {
            string name = item.m_shared.m_name;
            if (Localization.instance != null)
            {
                name = Localization.instance.Localize(name);
            }
            return item.m_stack > 1 ? $"{item.m_stack} {name}" : name;
        }

        private static void DestroyInventoryItem(InventoryGui gui, Inventory inventory, ItemDrop.ItemData item)
        {
            Player player = Player.m_localPlayer;

            // Vanilla blocks quest items from being dropped or moved out of the player's inventory,
            // and destroying one would be a harsher version of the same thing
            if (item.m_shared.m_questItem)
            {
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, "That item cannot be destroyed");
                return;
            }

            float confirmWindow = DestroyItemConfirmSeconds.Value;
            if (confirmWindow > 0f
                && (!ReferenceEquals(_destroyArmedItem, item) || Time.unscaledTime - _destroyArmedAt > confirmWindow))
            {
                _destroyArmedItem = item;
                _destroyArmedAt = Time.unscaledTime;
                _messageHud?.ShowMessage(MessageHud.MessageType.Center,
                    $"Destroy {DescribeItem(item)}?\nClick again to confirm");
                return;
            }
            _destroyArmedItem = null;

            // Describe it before it is gone, and unequip the way the drop path does so the model
            // does not stay in hand after the item behind it stops existing
            string description = DescribeItem(item);
            int quality = item.m_quality;
            player.RemoveEquipAction(item);
            player.UnequipItem(item, triggerEquipEffects: false);

            if (!inventory.RemoveItem(item))
            {
                return;
            }

            // Logged as well as shown, so a stack lost to a mis-click can at least be identified
            // afterwards from the log rather than guessed at
            Debug.Log($"[PipMod] Destroyed {description} (quality {quality}) from the inventory");
            _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft, $"Destroyed {description}");
            gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);

            // The crafting panel does not re-read requirements on its own; vanilla refreshes it by
            // hand after every inventory move, so this does too
            UpdateCraftingPanelMethod?.Invoke(gui, new object[] { false });
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
        class InventoryGui_OnSelectedItem_DestroyPatch
        {
            static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item,
                GameObject ___m_dragGo)
            {
                if (__instance == null || grid == null || item == null || !DestroyComboHeld())
                {
                    return true;
                }
                // A drag is in flight, so this click is where it lands - vanilla's business
                if (___m_dragGo != null)
                {
                    return true;
                }
                Player player = Player.m_localPlayer;
                if (player == null || player.IsTeleporting())
                {
                    return true;
                }
                Inventory inventory = grid.GetInventory();
                if (inventory == null || inventory != player.GetInventory())
                {
                    return true;
                }
                DestroyInventoryItem(__instance, inventory, item);
                return false;
            }
        }

        // ---------------- Fly toggle ----------------
        // The console command is nothing but a wrapper:
        //
        //     new ConsoleCommand("fly", "fly mode", delegate { Player.m_localPlayer.ToggleDebugFly(); },
        //         isCheat: true, ...);
        //
        // The isCheat flag belongs to the command, not to ToggleDebugFly, which is public and does
        // the whole job itself - flips m_debugFly, writes it to the player's ZDO so the server and
        // other clients agree, and posts its own HUD message. So there is nothing to reimplement
        // and no message to add here.
        //
        // Worth knowing: going through the method rather than the command means devcommands never
        // has to be enabled, and it is enabling devcommands - Terminal setting m_usedCheats - that
        // marks a character as cheated, not flying itself.
        //
        // Vanilla also binds this to Z, but only while m_debugMode is on and cheats are enabled,
        // so the default LeftShift+F does not collide with it.
        // Valheim reads the guardian power as a bare button:
        //
        //     if (!Hud.InRadial() && ... && (ZInput.GetButtonDown("GP") || ...)) StartGuardianPower();
        //
        // It never looks at modifiers, so binding the fly toggle to Shift+F fires the power as well
        // as flying. This declines the power for the one frame the fly shortcut is down.
        //
        // Order does not matter: KeyboardShortcut.IsDown reads Unity's per-frame key state, so it
        // answers the same whether vanilla's Update runs before or after ours.
        //
        // Only suppressed when the shortcut actually carries a modifier. If the fly toggle were
        // ever bound to a bare key, this would otherwise silently disable the power on that key
        // rather than merely disambiguating it.
        [HarmonyPatch(typeof(Player), nameof(Player.StartGuardianPower))]
        class Player_StartGuardianPower_Patch
        {
            static bool Prefix(ref bool __result)
            {
                if (FlyToggleHotkey == null)
                {
                    return true;
                }
                KeyboardShortcut shortcut = FlyToggleHotkey.Value;
                if (shortcut.MainKey == KeyCode.None || !shortcut.Modifiers.Any() || !shortcut.IsDown())
                {
                    return true;
                }
                __result = false;
                return false;
            }
        }

        private static void ToggleDebugFlight()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            if (IsSharedSession())
            {
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                    "Flight is off in a shared session");
                return;
            }
            player.ToggleDebugFly();
        }

        // ---------------- Camera zoom distance ----------------
        // GameCamera keeps two limits and chooses between them every frame:
        //
        //     float max = ((localPlayer.GetControlledShip() != null) ? m_maxDistanceBoat : m_maxDistance);
        //     m_distance = Mathf.Clamp(m_distance, minDistance, max);
        //
        // So raising the on-foot limit is enough; there is no need to touch the clamp itself.
        //
        // m_maxDistance is the field that moves, never m_maxDistanceBoat. The boat value is also
        // used as the denominator of a separate normalisation further down the same method
        // (Mathf.Clamp01((m_distance - m_minDistance) / (m_maxDistanceBoat - m_minDistance))), so
        // editing it would quietly change something else as well.
        //
        // Applied from Update rather than once at Awake, so a config reload takes effect without a
        // restart, and so a camera rebuilt mid-session is caught. The original is captured the
        // first time this runs and restored when the setting is turned off.
        private static GameCamera _cameraTracked;
        private static float _vanillaMaxDistance;
        private static bool _cameraLimitsLogged;

        private static void ApplyCameraDistance()
        {
            GameCamera camera = GameCamera.instance;
            if (camera == null)
            {
                return;
            }
            if (!ReferenceEquals(_cameraTracked, camera))
            {
                _cameraTracked = camera;
                _vanillaMaxDistance = camera.m_maxDistance;
                if (!_cameraLimitsLogged)
                {
                    _cameraLimitsLogged = true;
                    Debug.Log($"Camera zoom limits: on foot {camera.m_maxDistance:0.##}m, "
                        + $"at the helm {camera.m_maxDistanceBoat:0.##}m");
                }
            }

            float wanted = _vanillaMaxDistance;
            if (MaxCameraDistance.Value > 0f)
            {
                wanted = MaxCameraDistance.Value;
            }
            else if (MatchBoatCameraZoom.Value)
            {
                wanted = Mathf.Max(_vanillaMaxDistance, camera.m_maxDistanceBoat);
            }

            if (!Mathf.Approximately(camera.m_maxDistance, wanted))
            {
                camera.m_maxDistance = wanted;
            }
        }

        // ---------------- Swim speed from swim skill ----------------
        // Vanilla never ties swim speed to the swim skill. UpdateSwimming computes
        //
        //     float speed = m_swimSpeed * GetAttackSpeedFactorMovement();
        //
        // and the skill is read only in Player.OnSwimming, where it lerps the stamina drain between
        // m_swimStaminaDrainMinSkill and MaxSkill. So a skill-100 swimmer is no faster than a
        // beginner, only cheaper. This adds the speed scaling.
        //
        // m_swimSpeed is scaled in place for the duration of the call rather than reimplementing
        // UpdateSwimming, which is long and also handles liquid level, landing callbacks, stamina
        // and ragdolling. It has exactly one reader - the line above - so nothing else can observe
        // the altered value. A finalizer restores it, so an exception mid-swim cannot leave the
        // multiplier baked into the character.
        private static float SwimSkillSpeedMultiplier(Character character)
        {
            float atMax = SwimSpeedAtMaxSkill.Value;
            if (atMax <= 1f || character == null || !ReferenceEquals(character, Player.m_localPlayer)
                || IsSharedSession())
            {
                return 1f;
            }
            Skills skills = character.GetSkills();
            if (skills == null)
            {
                return 1f;
            }
            // GetSkillFactor is the 0-1 form of the level, so this is 1x untrained rising to
            // atMax at skill 100. Strictly that is 0.09x per point for a 10x cap rather than
            // 0.1x, because the curve starts at 1x instead of 0x - swimming at zero speed on a
            // fresh character being the alternative.
            float factor = Mathf.Clamp01(skills.GetSkillFactor(Skills.SkillType.Swim));
            return Mathf.Lerp(1f, atMax, factor);
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateSwimming))]
        class Character_UpdateSwimming_Patch
        {
            static void Prefix(Character __instance, ref float __state)
            {
                __state = 0f;
                float multiplier = SwimSkillSpeedMultiplier(__instance);
                if (multiplier <= 1f)
                {
                    return;
                }
                __state = __instance.m_swimSpeed;
                __instance.m_swimSpeed *= multiplier;
            }

            static void Finalizer(Character __instance, float __state)
            {
                if (__state > 0f && __instance != null)
                {
                    __instance.m_swimSpeed = __state;
                }
            }
        }

        // ---------------- Hoe: level ground ----------------
        // Two separate jobs, and the game fights us on each in a different way.
        //
        // RADIUS. The wheel is genuinely free while a level-ground ghost is up: Player only reads
        // it when the ghost's Piece.m_canRotate is set, which a terrain operation does not have.
        // It is not unclaimed, though - GameCamera zooms on it, and its guard explicitly allows
        // that while placing something unrotatable (!localPlayer.CanRotatePiece()). So the wheel is
        // intercepted at ZInput.GetMouseScrollWheel rather than merely read, otherwise resizing the
        // area would zoom the camera at the same time.
        //
        // The size itself has to be changed on the prefab's Settings, not the ghost's. A TerrainOp
        // serialises as nothing but a prefab hash, and the receiving side looks the settings back
        // up with ObjectDB.TryGetTerrainOp - so the ghost's copy is never consulted, and the change
        // is inherently client-local. That is also why this is suspended in a shared session: the
        // other side would apply the vanilla radius and the terrain would disagree.
        // Non-public in the shipped assembly, like most of what this feature touches
        private static readonly FieldInfo PlacementGhostField =
            AccessTools.Field(typeof(Player), "m_placementGhost");

        /// <summary>
        /// One of these per operation the wheel can resize, remembered for the session so switching
        /// from Cultivate back to Level ground finds the size it was left at.
        ///
        /// Every radius is captured, not just the one that happens to matter for a given op, and
        /// all of them are scaled together. Which radius does the work differs per operation -
        /// Level ground works through smoothing, Cultivate and Plant grass through painting - and
        /// scaling a radius whose flag is off costs nothing, since Settings.GetRadius only ever
        /// looks at the enabled ones.
        /// </summary>
        private class ScrollableOp
        {
            public string Label;
            public TerrainOp.Settings Settings;
            public float BaseLevel;
            public float BaseRaise;
            public float BaseSmooth;
            public float BasePaint;
            public float Multiplier = 1f;

            public void ApplyScale(float scale)
            {
                Settings.m_levelRadius = BaseLevel * scale;
                Settings.m_raiseRadius = BaseRaise * scale;
                Settings.m_smoothRadius = BaseSmooth * scale;
                Settings.m_paintRadius = BasePaint * scale;
            }
        }

        private static readonly Dictionary<string, ScrollableOp> _scrollableOps =
            new Dictionary<string, ScrollableOp>(StringComparer.OrdinalIgnoreCase);
        private static ScrollableOp _activeScrollOp;

        // Separate from the active op on purpose. This one identifies the levelling operation for
        // the perfect-flatten patch, and only ever holds the op named by HoeLevelOpPrefab - if it
        // were simply "whatever is selected", cultivating would flatten the ground it smoothed.
        // Sticky once found, because the operation is applied a frame or two after the click and
        // the selection may already have moved on.
        private static TerrainOp.Settings _perfectLevelSettings;

        // Raw wheel movement for the frame, captured by the patch below. Stored per frame rather
        // than accumulated because GameCamera alone calls the getter several times a frame.
        private static bool _hoeCaptureScroll;
        private static int _hoeScrollFrame = -1;
        private static float _hoeScrollValue;

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
        class ZInput_GetMouseScrollWheel_Patch
        {
            static void Postfix(ref float __result)
            {
                if (!_hoeCaptureScroll)
                {
                    return;
                }
                if (_hoeScrollFrame != Time.frameCount)
                {
                    _hoeScrollFrame = Time.frameCount;
                    _hoeScrollValue = __result;
                }
                // Everyone else sees nothing, which is what stops the camera zooming
                __result = 0f;
            }
        }

        /// <summary>
        /// Whether the player currently has a levelling operation selected, and which of the two
        /// implementations it uses.
        ///
        /// Valheim has two parallel terrain systems. TerrainOp goes through TerrainComp and is the
        /// one the newer pieces use; TerrainModifier is the older one and applies through
        /// Heightmap.ApplyModifier. Both declare m_level and m_levelRadius and neither is a subclass
        /// of the other. The first version of this looked for TerrainOp only, found nothing, and
        /// logged nothing at all - because the report sat behind the very checks that were failing.
        /// It reports first now.
        /// </summary>
        // Cleared on the config-reload hotkey so a ghost can be re-reported without restarting
        private static readonly HashSet<string> _reportedGhosts =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static GameObject _reportGhostObject;

        /// <summary>
        /// The name the build menu shows, so a prefab can be matched to the entry the player picked
        /// without having to guess from the id - "mud_road_v2" does not obviously mean Level ground.
        /// </summary>
        private static string DescribePiece()
        {
            Piece piece = _reportGhostObject != null ? _reportGhostObject.GetComponent<Piece>() : null;
            if (piece == null || string.IsNullOrEmpty(piece.m_name))
            {
                return "unnamed";
            }
            string label = Localization.instance != null
                ? Localization.instance.Localize(piece.m_name)
                : piece.m_name;
            return string.IsNullOrEmpty(label) ? piece.m_name : label;
        }

        /// <summary>
        /// Every component on the ghost and its children. The hoe's ring is drawn by something, and
        /// it is not a CircleProjector - the ghost reported having none - so this names the
        /// candidates rather than guessing at which Valheim type renders it.
        /// </summary>
        private static string DescribeComponents()
        {
            if (_reportGhostObject == null)
            {
                return "none";
            }
            var names = new List<string>();
            foreach (Component c in _reportGhostObject.GetComponentsInChildren<Component>(true))
            {
                if (c != null)
                {
                    names.Add(c.GetType().Name);
                }
            }
            return names.Count == 0
                ? "none"
                : string.Join(", ", names.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray());
        }

        private static void ReportGhost(string name, TerrainOp op, TerrainModifier mod)
        {
            if (!_reportedGhosts.Add(name))
            {
                return;
            }
            if (op != null && op.m_settings != null)
            {
                var set = op.m_settings;
                Debug.Log($"Terrain ghost '{name}' ({DescribePiece()}) components: {DescribeComponents()}");
                Debug.Log($"Terrain ghost '{name}' ({DescribePiece()}): TerrainOp, level={set.m_level} radius={set.m_levelRadius:0.##} "
                    + $"square={set.m_square} offset={set.m_levelOffset:0.##} smooth={set.m_smooth} "
                    + $"smoothRadius={set.m_smoothRadius:0.##} smoothPower={set.m_smoothPower:0.##}");
            }
            else if (mod != null)
            {
                Debug.Log($"Terrain ghost '{name}': TerrainModifier, level={mod.m_level} radius={mod.m_levelRadius:0.##} "
                    + $"square={mod.m_square} offset={mod.m_levelOffset:0.##} smooth={mod.m_smooth} "
                    + $"smoothRadius={mod.m_smoothRadius:0.##} smoothPower={mod.m_smoothPower:0.##}");
            }
            else
            {
                Debug.Log($"Terrain ghost '{name}': neither TerrainOp nor TerrainModifier. "
                    + $"Components: {DescribeComponents()}");
            }
        }

        /// <summary>
        /// Resolves the selected resizable op. For TerrainOp the prefab's own Settings must be
        /// edited, because the op serialises as a prefab hash and the receiver looks the settings
        /// back up - the ghost's copy is never consulted. TerrainModifier needs none of that: it is
        /// applied from the placed instance, so its radius is scaled per instance in Awake instead.
        /// </summary>
        private static bool ResolveSelectedScrollOp(Player player, out bool isTerrainOp)
        {
            isTerrainOp = false;
            _activeScrollOp = null;
            var ghost = PlacementGhostField?.GetValue(player) as GameObject;
            if (ghost == null)
            {
                return false;
            }
            // Children included: the component is not necessarily on the ghost root, and the
            // first two attempts at this both looked only at the root and found nothing.
            TerrainOp op = ghost.GetComponentInChildren<TerrainOp>(true);
            TerrainModifier mod = ghost.GetComponentInChildren<TerrainModifier>(true);

            // Reported before every bailout, including the one for "neither of them". Twice now a
            // report placed after a check has told us nothing precisely when the check was what
            // failed, which is indistinguishable from the code never running at all.
            string prefabName = Utils.GetPrefabName(ghost.name);
            _reportGhostObject = ghost;
            ReportGhost(prefabName, op, mod);

            if (op == null && mod == null)
            {
                return false;
            }

            // Not m_level. Every TerrainOp in the game has m_level false - the dump confirmed it
            // across all 28 - and the hoe levels through the smoothing pass instead. So the op is
            // identified by name.
            if (op == null || op.m_settings == null || !IsScrollRadiusOp(prefabName))
            {
                return false;
            }

            ObjectDB odb = ObjectDB.instance;
            if (odb == null || !odb.TryGetTerrainOp(prefabName.GetStableHashCode(), out TerrainOp prefab)
                || prefab == null || prefab.m_settings == null)
            {
                Debug.LogWarning($"Hoe level radius: '{prefabName}' is a TerrainOp but ObjectDB could not "
                    + "return its prefab, so the radius cannot be changed for it.");
                return false;
            }
            if (!_scrollableOps.TryGetValue(prefabName, out ScrollableOp state)
                || !ReferenceEquals(state.Settings, prefab.m_settings))
            {
                // Captured before anything is scaled, so these stay the vanilla sizes and a
                // multiplier can never compound against a value this code already changed
                state = new ScrollableOp
                {
                    Label = DescribePiece(),
                    Settings = prefab.m_settings,
                    BaseLevel = prefab.m_settings.m_levelRadius,
                    BaseRaise = prefab.m_settings.m_raiseRadius,
                    BaseSmooth = prefab.m_settings.m_smoothRadius,
                    BasePaint = prefab.m_settings.m_paintRadius,
                };
                _scrollableOps[prefabName] = state;
                Debug.Log($"Scroll radius armed for '{prefabName}' ({state.Label}): "
                    + $"level={state.BaseLevel:0.##} raise={state.BaseRaise:0.##} "
                    + $"smooth={state.BaseSmooth:0.##} paint={state.BasePaint:0.##}");
            }
            _activeScrollOp = state;

            if (string.Equals(prefabName, HoeLevelOpPrefab.Value, StringComparison.OrdinalIgnoreCase))
            {
                _perfectLevelSettings = prefab.m_settings;
            }
            isTerrainOp = true;
            return true;
        }

        private static bool IsScrollRadiusOp(string prefabName)
        {
            string list = ScrollRadiusOps.Value;
            if (string.IsNullOrEmpty(prefabName) || string.IsNullOrWhiteSpace(list))
            {
                return false;
            }
            foreach (string entry in list.Split(','))
            {
                if (string.Equals(entry.Trim(), prefabName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Puts every op back to its authored size except the one passed in, so a size scrolled
        /// onto Cultivate cannot leak into Level ground - or into an operation placed by something
        /// other than the player, since these are the shared prefab settings.
        /// </summary>
        private static void RestoreScrollOps(ScrollableOp except)
        {
            foreach (ScrollableOp op in _scrollableOps.Values)
            {
                if (op.Settings != null && !ReferenceEquals(op, except))
                {
                    op.ApplyScale(1f);
                }
            }
        }

        private const float HoeRadiusSettleSeconds = 0.5f;
        private static float _hoeRadiusSettledAt;
        private static bool _hoeRadiusMessagePending;
        private static bool _hoeWatcherAnnounced;

        private static void UpdateScrollRadius()
        {
            Player player = Player.m_localPlayer;
            // One line, once, the first time this runs with a player in the world. Without it,
            // "nothing in the log" cannot be told apart from "this code never executes", which is
            // the ambiguity that has cost two rounds on this feature already.
            if (player != null && !_hoeWatcherAnnounced)
            {
                _hoeWatcherAnnounced = true;
                Debug.Log($"Hoe level watcher active (scrollRadius={HoeLevelScrollRadius.Value}, "
                    + $"ghostField={(PlacementGhostField != null ? "ok" : "MISSING")})");
            }
            bool active = player != null && HoeLevelScrollRadius.Value && !IsTypingText()
                && ResolveSelectedScrollOp(player, out bool isTerrainOp);
            if (!active)
            {
                // Hand the wheel straight back, and restore every radius we scaled so a scaled
                // value cannot leak into an operation placed some other way
                _hoeCaptureScroll = false;
                RestoreScrollOps(null);
                return;
            }

            _hoeCaptureScroll = true;
            // Our own call is intercepted too: it returns zero but leaves the raw value behind,
            // which also makes this independent of the order Update methods happen to run in
            ZInput.GetMouseScrollWheel();
            float scroll = _hoeScrollFrame == Time.frameCount ? _hoeScrollValue : 0f;

            if (Mathf.Abs(scroll) > 0.001f)
            {
                float max = Mathf.Max(1f, HoeLevelMaxRadiusMultiplier.Value);
                float clamped = Mathf.Clamp(_activeScrollOp.Multiplier + (scroll > 0f ? 0.25f : -0.25f), 1f, max);
                if (!Mathf.Approximately(clamped, _activeScrollOp.Multiplier))
                {
                    _activeScrollOp.Multiplier = clamped;
                    // Only note that it changed. MessageHud holds each message for a minimum time
                    // and queues the rest, so one per notch meant spinning the wheel bought several
                    // seconds of backlog before the player could see where they had ended up.
                    _hoeRadiusSettledAt = Time.realtimeSinceStartup + HoeRadiusSettleSeconds;
                    _hoeRadiusMessagePending = true;
                }
            }

            // Sent once the wheel has been still for a moment, so a long spin produces one message
            // showing the final size rather than one per notch
            if (_hoeRadiusMessagePending && Time.realtimeSinceStartup >= _hoeRadiusSettledAt)
            {
                _hoeRadiusMessagePending = false;
                // The op's own radius, read back after scaling rather than computed here: which of
                // the four radii is the working one is the op's business, and GetRadius answers it
                _messageHud?.ShowMessage(MessageHud.MessageType.TopLeft,
                    $"{_activeScrollOp.Label} {_activeScrollOp.Multiplier:0.##}x "
                    + $"({_activeScrollOp.Settings.GetRadius():0.#}m)");
            }

            // Only the TerrainOp path needs the prefab edited here; TerrainModifier is scaled per
            // instance in its own Awake. Suspended in a shared session either way, since the other
            // side resolves its own settings and the terrain would disagree.
            if (_activeScrollOp.Settings != null)
            {
                float scale = IsSharedSession() ? 1f : _activeScrollOp.Multiplier;
                _activeScrollOp.ApplyScale(scale);
                RestoreScrollOps(_activeScrollOp);
                UpdateGhostVisualScale(scale);
            }
        }

        /// <summary>
        /// Resizes the expanding rings drawn under the cursor, so the area that will be flattened
        /// is visible rather than inferred from a number in the corner.
        ///
        /// These are particles, not a projector and not the mesh, and the system reported
        /// startSpeed=0 with scalingMode=Local. Both numbers rule something out: at zero speed the
        /// rings do not travel outward at all, so scaling speed multiplies nothing - they expand by
        /// growing, which makes particle size the dial. And Local scaling means the system uses its
        /// own transform only and ignores every ancestor, which is why scaling the ghost root had
        /// no effect either.
        ///
        /// Cosmetic in the strict sense: placing instantiates the prefab, never the ghost, and
        /// TerrainOp takes its radius from the settings rather than from anything here.
        /// </summary>
        private static int _ghostParticleInstanceId;
        private static readonly List<KeyValuePair<ParticleSystem, float>> _ghostParticleBases =
            new List<KeyValuePair<ParticleSystem, float>>();
        private static bool _ghostParticlesReported;

        private static void UpdateGhostVisualScale(float multiplier)
        {
            if (_reportGhostObject == null || multiplier <= 0f)
            {
                return;
            }

            // The ghost is destroyed and rebuilt at vanilla values whenever the selection changes,
            // so the authored speeds are re-read whenever the instance changes rather than cached
            // once - otherwise a multiplier would compound against an already-scaled value
            int instanceId = _reportGhostObject.GetInstanceID();
            if (instanceId != _ghostParticleInstanceId)
            {
                _ghostParticleInstanceId = instanceId;
                _ghostParticleBases.Clear();
                foreach (ParticleSystem ps in _reportGhostObject.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (ps != null)
                    {
                        _ghostParticleBases.Add(new KeyValuePair<ParticleSystem, float>(
                            ps, ps.main.startSizeMultiplier));
                    }
                }
                if (!_ghostParticlesReported && _ghostParticleBases.Count > 0)
                {
                    _ghostParticlesReported = true;
                    ParticleSystem first = _ghostParticleBases[0].Key;
                    var main = first.main;
                    Debug.Log($"Hoe level rings: {_ghostParticleBases.Count} particle system(s), "
                        + $"startSize={main.startSizeMultiplier:0.##} startSpeed={main.startSpeedMultiplier:0.##} "
                        + $"lifetime={main.startLifetimeMultiplier:0.##} "
                        + $"simulationSpace={main.simulationSpace} scalingMode={main.scalingMode}");
                }
            }

            foreach (KeyValuePair<ParticleSystem, float> entry in _ghostParticleBases)
            {
                ParticleSystem ps = entry.Key;
                if (ps == null)
                {
                    continue;
                }
                ParticleSystem.MainModule main = ps.main;
                float wanted = entry.Value * multiplier;
                if (Mathf.Approximately(main.startSizeMultiplier, wanted))
                {
                    continue;
                }
                // Only newly emitted particles take the new size, so the rings already in flight
                // finish at the old one and the change settles over roughly their lifetime. That
                // lag is why this looked like it was doing nothing until the read-back proved the
                // value was landing. Calling Clear() would snap it instantly at the cost of
                // blinking every visible ring out of existence, which is the worse trade.
                main.startSizeMultiplier = wanted;
            }
        }

        private static bool HoeLevelPerfectActive => HoeLevelPerfect.Value && !IsSharedSession();

        // Marks the window during which a smoothing pass belongs to the levelling operation.
        // SmoothTerrain is shared - Cultivate and Pave use it too - and it is handed only a
        // position, a radius and a power, so the call itself cannot be identified. The operation
        // that owns it can be, one level up.
        private static bool _inHoeLevelOp;

        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.InternalDoOperation))]
        class TerrainComp_InternalDoOperation_Patch
        {
            static void Prefix(TerrainOp.Settings modifier)
            {
                _inHoeLevelOp = modifier != null && ReferenceEquals(modifier, _perfectLevelSettings);
            }

            // A finalizer, not a postfix: terrain work throwing partway through must not leave the
            // flag set and turn every later smoothing pass into a flattening one
            static void Finalizer()
            {
                _inHoeLevelOp = false;
            }
        }

        // What vanilla actually does for "level ground", and why it never quite levels:
        //
        //     float t = 1f - Mathf.Pow(distance / radius, power);
        //     float delta = Mathf.Lerp(height, target, t) - height;
        //     m_smoothDelta[n] = Mathf.Clamp(m_smoothDelta[n] + delta, -1f, 1f);
        //
        // The lerp is weighted by distance from the centre, so only the exact centre ever reaches
        // the target and everything else is left on a dome; and the whole move is capped at one
        // metre, which is why a step in the ground survives any number of passes. Neither is the
        // -8/+8 clamp in LevelTerrain - that belongs to m_level, which no operation in the game
        // sets. An earlier version of this patched LevelTerrain and could never have had an effect.
        //
        // This sets every point in the circle to the target instead, with a clamp wide enough to be
        // worth having. The circle test and the vertex bookkeeping are vanilla's.
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.SmoothTerrain))]
        class TerrainComp_SmoothTerrain_Patch
        {
            static bool Prefix(TerrainComp __instance, Vector3 worldPos, float radius, bool square,
                Heightmap ___m_hmap, int ___m_width, float[] ___m_smoothDelta, bool[] ___m_modifiedHeight)
            {
                if (!_inHoeLevelOp || !HoeLevelPerfectActive || __instance == null || ___m_hmap == null
                    || ___m_smoothDelta == null || ___m_modifiedHeight == null)
                {
                    return true;   // fall back to vanilla rather than half-apply anything
                }
                float limit = Mathf.Max(1f, HoeLevelMaxDelta.Value);
                float target = worldPos.y - __instance.transform.position.y;
                ___m_hmap.WorldToVertex(worldPos, out int x, out int y);
                float vertexRadius = radius / ___m_hmap.m_scale;
                int span = Mathf.CeilToInt(vertexRadius);
                int stride = ___m_width + 1;
                var centre = new Vector2(x, y);

                for (int i = y - span; i <= y + span; i++)
                {
                    for (int j = x - span; j <= x + span; j++)
                    {
                        if (Vector2.Distance(centre, new Vector2(j, i)) > vertexRadius)
                        {
                            continue;
                        }
                        if (j < 0 || i < 0 || j >= stride || i >= stride)
                        {
                            continue;
                        }
                        int n = i * stride + j;
                        if (n < 0 || n >= ___m_smoothDelta.Length)
                        {
                            continue;
                        }
                        // The whole difference, not a fraction of it, and the same for every point
                        // in the circle - which is what makes the result actually flat
                        float delta = target - ___m_hmap.GetHeight(j, i);
                        ___m_smoothDelta[n] = Mathf.Clamp(___m_smoothDelta[n] + delta, -limit, limit);
                        ___m_modifiedHeight[n] = true;
                    }
                }
                return false;
            }
        }

        // ---------------- Passive Wisplight ----------------
        // Demisting is not a property of the item. Equipping a Wisplight applies its
        // m_equipStatusEffect (SE_Demister), whose UpdateStatusEffect spawns the wisp ball and
        // flies it around the player; the ball carries the ParticleSystemForceField that pushes the
        // mist back, and ParticleMist.InsideDemister reads exactly two things from it - the
        // transform position and m_forceField.endRange.
        //
        // So the effect is applied the way the forsaken powers are and vanilla does the rest.
        // Building our own invisible force field was the alternative and was rejected: the vanilla
        // field is tuned, InsideDemister dereferences m_forceField with no null check, and a
        // hand-made one would have to guess the shape and strength that make the push look right.
        //
        // WisplightRangeMultiplier needs nothing extra - the ball this spawns is a Demister like
        // any other, so the existing Demister.Awake postfix already scales it.
        private const float WisplightCheckSeconds = 1f;
        private static float _wisplightCheckedAt = float.NegativeInfinity;
        private static int _wisplightEffectHash;
        private static string _wisplightCraftStat;
        private static bool _wisplightLookupDone;
        private static int _passiveWisplightAdds;
        private static bool _passiveWisplightEffectsCleared;
        // Non-public in the shipped assembly even though the publicized reference declares it
        // public - the access audit caught this after the build looked clean
        private static readonly MethodInfo RemoveStartEffectsMethod =
            AccessTools.Method(typeof(StatusEffect), "RemoveStartEffects");

        /// <summary>
        /// Resolves the Wisplight's equip effect and its craft-stat key from ObjectDB rather than
        /// hard-coding either. The item prefab is "Demister"; the craft stat is keyed on the shared
        /// display token, which is what IncrementStatItemCraft records.
        /// </summary>
        private static void ResolveWisplight()
        {
            if (_wisplightLookupDone)
            {
                return;
            }
            ObjectDB odb = ObjectDB.instance;
            if (odb == null)
            {
                return;   // not loaded yet, try again on the next sweep
            }
            _wisplightLookupDone = true;
            GameObject prefab = odb.GetItemPrefab("Demister");
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            var shared = drop != null && drop.m_itemData != null ? drop.m_itemData.m_shared : null;
            if (shared == null)
            {
                Debug.LogWarning("PassiveWisplight: could not find the Demister item in ObjectDB");
                return;
            }
            _wisplightCraftStat = shared.m_name;
            StatusEffect equipEffect = shared.m_equipStatusEffect;
            if (equipEffect == null)
            {
                Debug.LogWarning("PassiveWisplight: the Wisplight has no equip status effect");
                return;
            }
            _wisplightEffectHash = equipEffect.NameHash();
        }

        /// <summary>
        /// Whether this character has ever crafted a Wisplight. Bucket 0 is the lifetime tally and
        /// is incremented unconditionally, unlike the achievement buckets which are only credited
        /// when CanGetAchievements allows it - so this still reads true on a character the game
        /// considers cheated, which every modded session is.
        /// </summary>
        private static bool HasCraftedWisplight()
        {
            if (string.IsNullOrEmpty(_wisplightCraftStat))
            {
                return false;
            }
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            var buckets = profile != null ? profile.m_playerStats : null;
            if (buckets == null || buckets.Length == 0 || buckets[0] == null
                || buckets[0].m_itemCraftStats == null)
            {
                return false;
            }
            return buckets[0].m_itemCraftStats.TryGetValue(_wisplightCraftStat, out float crafted)
                && crafted > 0f;
        }

        private static void UpdatePassiveWisplight()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            float now = Time.realtimeSinceStartup;
            if (now - _wisplightCheckedAt < WisplightCheckSeconds)
            {
                return;
            }
            _wisplightCheckedAt = now;
            ResolveWisplight();
            SEMan seman = player.GetSEMan();
            if (_wisplightEffectHash == 0 || seman == null)
            {
                return;
            }

            bool wanted = PassiveWisplight.Value && !IsSharedSession() && HasCraftedWisplight();
            StatusEffect active = seman.GetStatusEffect(_wisplightEffectHash);

            if (wanted)
            {
                if (active == null)
                {
                    // StatusEffect.Setup posts m_startMessage - "You release the bound wisp" - and
                    // our sweep adds the effect fresh every time the character loads, so without
                    // this it greeted the player on every login for something they never did
                    _suppressStatusStartFeedback = true;
                    try
                    {
                        active = seman.AddStatusEffect(_wisplightEffectHash, false, 0, 0f, -1);
                    }
                    finally
                    {
                        _suppressStatusStartFeedback = false;
                    }
                    if (active != null)
                    {
                        // Counted, not just flagged. The previous version logged only the first
                        // add, so an effect being re-added every sweep would have looked identical
                        // to one added once - and a once-per-second re-add is exactly what a
                        // once-per-second pulse implies.
                        _passiveWisplightAdds++;
                        if (_passiveWisplightAdds == 1)
                        {
                            Debug.Log("Wisplight held passively - the Mistlands mist clears with nothing equipped");
                        }
                        else if (_passiveWisplightAdds == 5)
                        {
                            Debug.LogWarning("PassiveWisplight: the effect has been added 5 times, so something is "
                                + "dropping it between sweeps rather than it persisting - SEMan.GetStatusEffect is "
                                + "not finding what AddStatusEffect created.");
                        }
                    }
                }
                if (active != null)
                {
                    // Same reasoning as the forsaken powers: a zero ttl never lapses, and this is
                    // SEMan's own clone so the asset the equipped item uses is left alone
                    active.m_ttl = 0f;
                    active.m_hidden = true;
                    // Every sweep, so toggling HideWisplightWisp reaches a ball already in the air
                    // and a ball respawned by SE_Demister is caught too
                    ApplyWisplightVisibility(active);
                    // Clears anything a previous Setup already parented to the character - the
                    // suppression above only prevents new instances being created
                    if (!_passiveWisplightEffectsCleared && RemoveStartEffectsMethod != null)
                    {
                        _passiveWisplightEffectsCleared = true;
                        RemoveStartEffectsMethod.Invoke(active, null);
                    }
                }
            }
            else if (active != null && active.m_ttl != 0f)
            {
                // An equipped Wisplight produces the same effect hash, so the hiding applies to it
                // too - which is what the config promises - without touching its lifetime
                ApplyWisplightVisibility(active);
            }
            else if (active != null && active.m_ttl == 0f && !IsWisplightEquipped(player))
            {
                // Withdraw only an unlimited effect we granted, and never while the item is
                // actually equipped - that effect belongs to the item and carries its own ttl
                seman.RemoveStatusEffect(_wisplightEffectHash, true);
            }
        }

        // Only true for the instant our own AddStatusEffect call is on the stack, so an effect the
        // player earns normally still announces itself. Blanking the message on the shared asset
        // instead would have silenced the equipped Wisplight too, and mutating a ScriptableObject
        // the game reuses is the kind of global edit that has bitten this project before.
        private static bool _suppressStatusStartFeedback;

        // StatusEffect.Setup posts the message and then calls TriggerStartEffects, which creates
        // the effect instances **parented to m_character.transform** - on the player, not on the
        // wisp. That is what pulsed: hiding the ball could never have hidden it, because it was
        // never on the ball. Skipping creation also covers the case where the effect is a looping
        // one, since there is then nothing attached to keep looping.
        [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.TriggerStartEffects))]
        class StatusEffect_TriggerStartEffects_Patch
        {
            static bool Prefix()
            {
                return !_suppressStatusStartFeedback;
            }
        }

        [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.Setup))]
        class StatusEffect_Setup_Patch
        {
            static void Prefix(StatusEffect __instance, ref string __state)
            {
                if (!_suppressStatusStartFeedback || __instance == null)
                {
                    return;
                }
                __state = __instance.m_startMessage;
                __instance.m_startMessage = string.Empty;
            }

            // Restored rather than left blank: this is SEMan's clone, but the effect may be
            // re-Setup later and the message is not ours to delete permanently
            static void Postfix(StatusEffect __instance, string __state)
            {
                if (__state != null && __instance != null)
                {
                    __instance.m_startMessage = __state;
                }
            }
        }

        /// <summary>
        /// Guards the withdrawal path. An equipped Wisplight re-applies its effect continuously, so
        /// removing it would fight the item rather than simply failing.
        /// </summary>
        private static bool IsWisplightEquipped(Player player)
        {
            Inventory inventory = player.GetInventory();
            if (inventory == null)
            {
                return false;
            }
            foreach (ItemDrop.ItemData item in inventory.GetEquippedItems())
            {
                if (item != null && item.m_shared != null && item.m_shared.m_equipStatusEffect != null
                    && item.m_shared.m_equipStatusEffect.NameHash() == _wisplightEffectHash)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Holds a guardian power on the player for every boss power this character has unlocked.
        /// The effect is added once and then given an unlimited lifetime rather than being
        /// re-applied each tick; the periodic sweep exists to catch the cases that clear it,
        /// chiefly death, which calls SEMan.RemoveAllStatusEffects.
        /// </summary>
        private static void UpdatePassiveForsakenPowers()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            float now = Time.realtimeSinceStartup;
            if (now - _powerCheckedAt < PowerCheckSeconds)
            {
                return;
            }
            _powerCheckedAt = now;
            ScanBossPowers();
            SEMan seman = player.GetSEMan();
            if (_bossPowers == null || _bossPowers.Count == 0 || seman == null)
            {
                return;
            }
            bool enabled = PassiveForsakenPowers.Value;
            bool shared = IsSharedSession();

            // A power is only held once the character has selected it at its stone at least once,
            // because that is what writes the unique key this reads. Mounting the trophy is not
            // enough. Reported once per session so the gap is visible rather than being something
            // the player has to already know - "4 of 7 held" sat in the log unnoticed for weeks.
            if (enabled && !_lockedPowersReported)
            {
                _lockedPowersReported = true;
                var locked = _bossPowers
                    .Where(b => !player.HaveUniqueKey(b.PowerName))
                    .Select(b => b.PowerName)
                    .ToArray();
                if (locked.Length > 0)
                {
                    Debug.LogWarning($"Forsaken powers not yet unlocked on this character: {string.Join(", ", locked)}. "
                        + "Select each once at its boss stone - mounting the trophy alone does not register it.");
                }
            }
            foreach (BossPower boss in _bossPowers)
            {
                // The only part of a status effect that reaches other players is the four-flag
                // StatusAttribute mask written to the player's ZDO. SailingPower is one of those
                // flags and it applies to whatever ship the player is aboard, so powers carrying an
                // attribute stay off in a shared session while the purely local ones still work.
                bool wanted = enabled
                    && player.HaveUniqueKey(boss.PowerName)
                    && !(shared && boss.Attributes != StatusEffect.StatusAttribute.None);
                StatusEffect active = seman.GetStatusEffect(boss.PowerHash);
                if (wanted)
                {
                    if (active == null)
                    {
                        active = seman.AddStatusEffect(boss.PowerHash, false, 0, 0f, -1);
                        if (active != null)
                        {
                            Debug.Log($"Forsaken power {boss.PowerName} held passively ({boss.StoneName})");
                        }
                    }
                    if (active != null)
                    {
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
                }
                else if (active != null && active.m_ttl == 0f)
                {
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
            if (!HealthFloorActive || character == null || !ReferenceEquals(character, Player.m_localPlayer))
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

                    // The pacing caps below belong to the health floor feature, so switching the
                    // floor off has to switch them off too. They cap each hit at a fraction of
                    // *current* health, which means health only ever approaches zero and never
                    // reaches it - left running with MinHealthPercent at 0 they make the player
                    // quietly unkillable, which looks like the game refusing to deal damage rather
                    // than like a mod setting.
                    if (!HealthFloorActive)
                    {
                        if (LogPlayerDamage.Value)
                        {
                            Debug.Log($"Damage gate: disabled, type={hit.m_hitType} incoming={totalDamage:0.#} health={currentHealth:0.#}/{maxHealth:0.#}");
                        }
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
