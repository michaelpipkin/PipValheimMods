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
        private static ConfigEntry<KeyboardShortcut> StatBucketRepairHotkey;
        private static ConfigEntry<KeyboardShortcut> ReloadConfigHotkey;
        private static ConfigEntry<KeyboardShortcut> HealthFloorToggleHotkey;
        private static ConfigEntry<KeyboardShortcut> FreeBuildHotkey;
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
        private static ConfigEntry<bool> ShowMapCursorCoordinates;
        private static ConfigEntry<string> HudFontName;
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
            SmelterInputPriority = Config.Bind("Containers", "SmelterInputPriority", "FlametalOreNew,FlametalOre,BlackMetalScrap,SilverOre,IronScrap,CopperOre,TinOre", "Which ore a smelter, kiln or blast furnace reaches for first when several are available." + priorityHelp);
            CookingStationInputPriority = Config.Bind("Containers", "CookingStationInputPriority", "SerpentMeat,LoxMeat,BugMeat,ChickenMeat,HareMeat,WolfMeat,DeerMeat,RawMeat,NeckTail,FishRaw", "Which raw item a cooking station or oven reaches for first when several are available. The default is ordered roughly by biome progression and is only a starting point - reorder it to taste." + priorityHelp);
            ContainerRange = Config.Bind("Containers", "ContainerRange", 20f, "How far away, in metres, a container can be and still count toward crafting requirements.");
            MinHealthPercent = Config.Bind("General", "MinHealthPercent", 0.25f, new ConfigDescription("Damage can never take you below this fraction of your maximum health. 0.25 keeps you at a quarter health no matter how big the hit. Set to 0 to disable the floor and take damage normally.", new AcceptableValueRange<float>(0f, 0.95f)));
            LogPlayerDamage = Config.Bind("General", "LogPlayerDamage", false, "Diagnostic. Logs every hit that reaches the damage gate, with its type, the raw amount, what the gate allowed through and your health at the time. Use it to find out what actually killed you.");
            MassPlantKey = Config.Bind("Farming", "MassPlantKey", KeyCode.LeftShift, "Hold this while planting with the cultivator to plant a whole grid at once instead of a single seed.");
            MassPlantGridSize = Config.Bind("Farming", "MassPlantGridSize", 5, new ConfigDescription("Width of the grid planted while the mass-plant key is held. 5 plants a 5x5 block of 25. Set to 1 to disable.", new AcceptableValueRange<int>(1, 11)));
            PassiveForsakenPowers = Config.Bind("Powers", "PassiveForsakenPowers", true,
                "Keep every forsaken power this character has unlocked permanently active - no selecting, no activating, no cooldown. A power counts as unlocked once you have selected it at its stone, which the game records on the character rather than in the world, so unlocked powers follow you into any world. Powers carrying a networked status attribute (Moder's sailing power) stay off in a shared session, since that flag reaches any ship you are aboard.");
            LogConsoleOutput = Config.Bind("General", "LogConsoleOutput", true,
                "Mirror in-game console output to the BepInEx log. The console keeps only a few lines and cannot be scrolled, so commands with long output - 'achievements' in particular - are unreadable in game without this.");
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
            PinIncludeLocations = Config.Bind("Map", "PinIncludeLocations", "CharredFortress",
                "Also pin surface locations whose name contains one of these comma separated fragments, even though they have no interior and no dungeon generator. "
                + "Those two are what the other hooks key off, so a structure that is simply built on the surface - a charred fortress, for instance - is otherwise invisible to all of them. "
                + "PinExcludeNames still wins, so a fragment listed in both is excluded.");
            HudFontName = Config.Bind("Clock", "HudFontName", "AveriaSerifLibre-Bold",
                "Font for the clock, coordinates and map cursor readout, matched against the fonts the game has loaded. AveriaSerifLibre is the body font the game uses for item and effect names; Norsebold is the all-caps display font behind headings. An exact name beats a partial one, so AveriaSerifLibre-Bold selects that variant specifically. Leave empty for Unity's default. If the named font is not found, the log lists every font that is.");
            ShowMapCursorCoordinates = Config.Bind("Map", "ShowMapCursorCoordinates", true,
                "While the full map is open, show the world coordinates under the mouse cursor. Useful for finding a spot someone else has given you coordinates for on a known seed.");
            MapCursorPositionX = Config.Bind("Map", "MapCursorPositionX", 12f,
                "Map cursor readout offset in pixels from the left edge of the map panel, not the screen.");
            MapCursorPositionY = Config.Bind("Map", "MapCursorPositionY", 12f,
                "Map cursor readout offset in pixels from the top edge of the map panel, not the screen.");
            AutoPinResources = Config.Bind("Map", "AutoPinResources", true,
                "Pin resource nodes as they load - tar pits, ore deposits, scrap piles.");
            ResourcePinIcon = Config.Bind("Map", "ResourcePinIcon", 3,
                "Minimap.PinType index for resource pins, separate from the dungeon icon. 3 is the dot.");
            ResourcePinNames = Config.Bind("Map", "ResourcePinNames",
                "rock4_copper=Copper,MineRock_Copper=Copper,rock3_silver=Silver,silvervein=Silver,mudpile=Mud Pile,FlametalRockstand=Flametal,LeviathanLava=Flametal,TarPit=Tar Pit,DragonEgg=Dragon Egg,Leviathan=Leviathan",
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
            DungeonPinIconOverrides = Config.Bind("Map", "DungeonPinIconOverrides", "Fuling Camp=0,Draugr Village=0,Meadows Farm=0,GoblinCamp2=0",
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
            FreeBuildHotkey = Config.Bind("Hotkeys", "FreeBuildHotkey", new KeyboardShortcut(KeyCode.Slash),
                "Toggles building without resources, for when you are far from base and need one piece you cannot carry the materials for. Affects placing pieces only - crafting, upgrading and repairing still cost what they cost. Starts off each time the game launches, and is suspended in a shared session since the pieces it leaves behind are part of the world everyone sees.");
            JumpBufferSeconds = Config.Bind("General", "JumpBufferSeconds", 0.3f, new ConfigDescription(
                "How long a jump press is remembered and retried when the game refuses it, in seconds. A press that lands a fraction too early - while still in the air from a stride or a step off a ledge - is otherwise dropped silently. Set to 0 to turn the buffer off.", new AcceptableValueRange<float>(0f, 1f)));
            LogJumpBlocks = Config.Bind("General", "LogJumpBlocks", false,
                "Diagnostic. On every jump press, log the state the game decides from: whether you are grounded, blocking, crouched, attacking and so on. Use it to find out what is actually swallowing a jump.");
            HealthFloorToggleHotkey = Config.Bind("Hotkeys", "HealthFloorToggleHotkey", new KeyboardShortcut(KeyCode.Minus),
                "Turns the MinHealthPercent damage floor on and off without editing the config. The floor keeps you alive through things like Ashlands lava, but it also makes it impossible to die on purpose - sailing off the edge of the world leaves no other way out. Starts on each time the game launches.");
            ReloadConfigHotkey = Config.Bind("Hotkeys", "ReloadConfigHotkey", new KeyboardShortcut(KeyCode.Equals),
                "Re-read this file from disk so edits take effect without relaunching. BepInEx has no file watcher, so nothing else notices the file changing - values parsed at startup are kept for the lifetime of the process. Settings read every frame apply at once; anything applied only at startup, such as which Harmony patches exist, still needs a relaunch.");
            StatBucketRepairHotkey = Config.Bind("Hotkeys", "StatBucketRepairHotkey", new KeyboardShortcut(KeyCode.Backslash),
                "Reports achievement stats that the vanilla bucket-latch bug has frozen, and repairs them when StatBucketRepairApply is on. Must be pressed in a loaded world.");
            StatBucketRepairApply = Config.Bind("General", "StatBucketRepairApply", false,
                "Let the stat bucket repair hotkey actually write to the character profile. Off means it only reports what it would change. Back up the character .fch file before turning this on.");
            DumpItemListHotkey = Config.Bind("Hotkeys", "DumpItemListHotkey", new KeyboardShortcut(KeyCode.RightBracket), "Hotkey to dump every item prefab in the game to files in the BepInEx config folder. Must be pressed in a loaded world.");

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

            if (StatBucketRepairHotkey.Value.IsDown())
            {
                RepairStatBuckets();
            }

            if (ReloadConfigHotkey.Value.IsDown())
            {
                ReloadModConfig();
            }

            if (HealthFloorToggleHotkey.Value.IsDown())
            {
                ToggleHealthFloor();
            }

            if (FreeBuildHotkey.Value.IsDown())
            {
                ToggleFreeBuild();
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
            if (Player.m_localPlayer == null || Hud.IsUserHidden())
            {
                return;
            }
            DrawHudLine();
            DrawMapCursorCoordinates();
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
        // Spawn, GetClosestPin and m_pins are all non-public in the shipped assembly even though
        // the publicized reference shows them public. Spawn is only *patched*, which is always
        // allowed; GetClosestPin has to be invoked, so it goes through an AccessTools handle. It is
        // called with mustBeVisible false deliberately - the vanilla default only counts pins whose
        // UI element is currently active, which would let duplicates through for pins off-screen.
        private static readonly MethodInfo GetClosestPinMethod = AccessTools.Method(
            typeof(Minimap), "GetClosestPin", new[] { typeof(Vector3), typeof(float), typeof(bool) });
        private static readonly MethodInfo AddPinMethod = AccessTools.Method(typeof(Minimap), "AddPin");

        /// <summary>
        /// Calls Minimap.AddPin without referencing Splatform. Its last parameter is a
        /// PlatformUserID with a default value, and the compiler has to materialise that default at
        /// the call site, which drags in an assembly this project otherwise has no use for. Going
        /// through reflection and filling the trailing optional parameters here avoids adding a
        /// dependency for one call, and survives Iron Gate appending further optional parameters.
        /// </summary>
        private static bool AddMapPin(Minimap map, Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked)
        {
            if (AddPinMethod == null)
            {
                return false;
            }
            ParameterInfo[] parameters = AddPinMethod.GetParameters();
            if (parameters.Length < 5)
            {
                return false;
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
            AddPinMethod.Invoke(map, args);
            return true;
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
                message = "Free building ON - pieces cost nothing and drop nothing";
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
            if (GetClosestPinMethod != null)
            {
                object existing = GetClosestPinMethod.Invoke(map,
                    new object[] { position, Mathf.Max(0f, mergeRadius), false });
                if (existing != null)
                {
                    return false;
                }
            }
            if (!AddMapPin(map, position, type, label, true, false))
            {
                return false;
            }
            Debug.Log($"Pin added: '{label}' as {type} at X {position.x:0} Z {position.z:0} ({detail})");
            return true;
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
                bool interior = PinInteriorLocations.Value && __instance.m_hasInterior;
                if (!interior && !IsIncludedLocation(StripClone(__instance.gameObject.name)))
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
                Debug.Log($"Blocked stat regression: {stat} would have been set to {amount:0.##}, keeping {best:0.##}");
                return false;
            }
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
