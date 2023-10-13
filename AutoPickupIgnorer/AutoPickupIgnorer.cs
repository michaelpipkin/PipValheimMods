using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

        private const string modGUID = "Pip.AutoPickupIgnorer";
        private const string modName = "Pip's Auto-pickup Ignorer";
        private const string modVersion = "1.0.4";
        private readonly Harmony harmony = new Harmony(modGUID);
        private static ConfigEntry<string> AutoPickupIgnoreList;
        private static List<string> IgnoreList;
        private static ConfigEntry<KeyboardShortcut> ToggleBehaviorHotkey;
        private static PickupBehavior CurrentPickupBehavior = PickupBehavior.Custom;
        private static MessageHud MessageHud;
        private const string DefaultItemList = "#Acorn,#Amber,#AmberPearl,#AncientSeed,#ArmorBronzeChest,#ArmorBronzeLegs,#ArmorCarapaceChest,#ArmorCarapaceLegs," +
            "#ArmorDress1,#ArmorDress10,#ArmorDress2,#ArmorDress3,#ArmorDress4,#ArmorDress5,#ArmorDress6,#ArmorDress7,#ArmorDress8,#ArmorDress9,#ArmorFenringChest," +
            "#ArmorFenringLegs,#ArmorIronChest,#ArmorIronLegs,#ArmorLeatherChest,#ArmorLeatherLegs,#ArmorMageChest,#ArmorMageLegs,#ArmorPaddedCuirass,#ArmorPaddedGreaves," +
            "#ArmorRagsChest,#ArmorRagsLegs,#ArmorRootChest,#ArmorRootLegs,#ArmorTrollLeatherChest,#ArmorTrollLeatherLegs,#ArmorTunic1,#ArmorTunic10,#ArmorTunic2," +
            "#ArmorTunic3,#ArmorTunic4,#ArmorTunic5,#ArmorTunic6,#ArmorTunic7,#ArmorTunic8,#ArmorTunic9,#ArmorWolfChest,#ArmorWolfLegs,#ArrowBronze,#ArrowCarapace," +
            "#ArrowFire,#ArrowFlint,#ArrowFrost,#ArrowIron,#ArrowNeedle,#ArrowObsidian,#ArrowPoison,#ArrowSilver,#ArrowWood,#AtgeirBlackmetal,#AtgeirBronze," +
            "#AtgeirHimminAfl,#AtgeirIron,#AxeBlackMetal,#AxeBronze,#AxeFlint,#AxeIron,#AxeJotunBane,#AxeStone,#BarberKit,#Barley,#BarleyFlour,#BarleyWine," +
            "#BarleyWineBase,#Battleaxe,#BattleaxeCrystal,#BeechSeeds,#BeltStrength,#Bilebag,#BirchSeeds,#BlackCore,#BlackMarble,#BlackMetal,#BlackMetalScrap," +
            "#BlackSoup,#Bloodbag,#BloodPudding,#Blueberries,#BoarJerky,#BoltBlackmetal,#BoltBone,#BoltCarapace,#BoltIron,#BombBile,#BombOoze,#BoneFragments,#Bow," +
            "#BowDraugrFang,#BowFineWood,#BowHuntsman,#BowSpineSnap,#Bread,#BreadDough,#Bronze,#BronzeNails,#BronzeScrap,#BugMeat,#CapeDeerHide,#CapeFeather,#CapeLinen," +
            "#CapeLox,#CapeOdin,#CapeTrollHide,#CapeWolf,#Carapace,#Carrot,#CarrotSeeds,#CarrotSoup,#Chain,#chest_hildir1,#chest_hildir2,#chest_hildir3,#ChickenEgg," +
            "#ChickenMeat,#Chitin,#Cloudberry,#Club,#Coal,#Coins,#CookedBugMeat,#CookedChickenMeat,#CookedDeerMeat,#CookedEgg,#CookedHareMeat,#CookedLoxMeat,#CookedMeat," +
            "#CookedWolfMeat,#Copper,#CopperOre,#CopperScrap,#CrossbowArbalest,#CryptKey,#Crystal,#Cultivator,#Dandelion,#DeerHide,#DeerMeat,#DeerStew,#Demister," +
            "#DragonEgg,#DragonTear,#DvergerArbalest,#DvergerArbalest_shoot,#DvergrKey,#DvergrKeyFragment,#DvergrNeedle,#Eitr,#ElderBark,#Entrails,#Eyescream,#Feathers," +
            "#FineWood,#FirCone,#FireworksRocket_Blue,#FireworksRocket_Cyan,#FireworksRocket_Green,#FireworksRocket_Purple,#FireworksRocket_Red,#FireworksRocket_White," +
            "#FireworksRocket_Yellow,#Fish1,#Fish10,#Fish11,#Fish12,#Fish2,#Fish3,#Fish4_cave,#Fish5,#Fish6,#Fish7,#Fish8,#Fish9,#FishAndBread,#FishAndBreadUncooked," +
            "#FishAnglerRaw,#FishCooked,#FishingBait,#FishingBaitAshlands,#FishingBaitCave,#FishingBaitDeepNorth,#FishingBaitForest,#FishingBaitMistlands,#FishingBaitOcean," +
            "#FishingBaitPlains,#FishingBaitSwamp,#FishingRod,#FishRaw,#FishWraps,#FistFenrirClaw,#Flametal,#FlametalOre,#Flax,#Flint,#FreezeGland,#GiantBloodSack," +
            "#GoblinTotem,#GreydwarfEye,#Guck,#Hammer,#HardAntler,#HareMeat,#HelmetBronze,#HelmetCarapace,#HelmetDrake,#HelmetDverger,#HelmetFenring,#HelmetFishingHat," +
            "#HelmetHat1,#HelmetHat10,#HelmetHat2,#HelmetHat3,#HelmetHat4,#HelmetHat5,#HelmetHat6,#HelmetHat7,#HelmetHat8,#HelmetHat9,#HelmetIron,#HelmetLeather," +
            "#HelmetMage,#HelmetMidsummerCrown,#HelmetOdin,#HelmetPadded,#HelmetRoot,#HelmetTrollLeather,#HelmetYule,#HildirKey_forestcrypt,#HildirKey_mountaincave," +
            "#HildirKey_plainsfortress,#Hoe,#Honey,#HoneyGlazedChicken,#HoneyGlazedChickenUncooked,#Iron,#IronNails,#IronOre,#Ironpit,#IronScrap,#JuteBlue,#JuteRed," +
            "#KnifeBlackMetal,#KnifeButcher,#KnifeChitin,#KnifeCopper,#KnifeFlint,#KnifeSilver,#KnifeSkollAndHati,#Lantern,#Larva,#LeatherScraps,#LinenThread,#LoxMeat," +
            "#LoxPelt,#LoxPie,#LoxPieUncooked,#MaceBronze,#MaceIron,#MaceNeedle,#MaceSilver,#MagicallyStuffedShroom,#MagicallyStuffedShroomUncooked,#Mandible," +
            "#MeadBaseEitrMinor,#MeadBaseFrostResist,#MeadBaseHealthMajor,#MeadBaseHealthMedium,#MeadBaseHealthMinor,#MeadBasePoisonResist,#MeadBaseStaminaLingering," +
            "#MeadBaseStaminaMedium,#MeadBaseStaminaMinor,#MeadBaseTasty,#MeadEitrMinor,#MeadFrostResist,#MeadHealthMajor,#MeadHealthMedium,#MeadHealthMinor," +
            "#MeadPoisonResist,#MeadStaminaLingering,#MeadStaminaMedium,#MeadStaminaMinor,#MeadTasty,#MeatPlatter,#MeatPlatterUncooked,#MechanicalSpring,#MinceMeatSauce," +
            "#MisthareSupreme,#MisthareSupremeUncooked,#Mushroom,#MushroomBlue,#MushroomJotunPuffs,#MushroomMagecap,#MushroomOmelette,#MushroomYellow,#NeckTail," +
            "#NeckTailGrilled,#Needle,#Obsidian,#Onion,#OnionSeeds,#OnionSoup,#Ooze,#PickaxeAntler,#PickaxeBlackMetal,#PickaxeBronze,#PickaxeIron,#PickaxeStone," +
            "#PineCone,#Pukeberries,#QueenBee,#QueenDrop,#QueensJam,#Raspberry,#RawMeat,#Resin,#Root,#RottenMeat,#RoundLog,#RoyalJelly,#Ruby,#SaddleLox,#Salad,#Sap," +
            "#Sausages,#ScaleHide,#SeekerAspic,#SerpentMeat,#SerpentMeatCooked,#SerpentScale,#SerpentStew,#SharpeningStone,#ShieldBanded,#ShieldBlackmetal," +
            "#ShieldBlackmetalTower,#ShieldBoneTower,#ShieldBronzeBuckler,#ShieldCarapace,#ShieldCarapaceBuckler,#ShieldIronBuckler,#ShieldIronSquare,#ShieldIronTower," +
            "#ShieldKnight,#ShieldSerpentscale,#ShieldSilver,#ShieldWood,#ShieldWoodTower,#ShocklateSmoothie,#Silver,#SilverNecklace,#SilverOre,#SledgeDemolisher," +
            "#SledgeIron,#SledgeStagbreaker,#Softtissue,#Sparkler,#SpearBronze,#SpearCarapace,#SpearChitin,#SpearElderbark,#SpearFlint,#SpearWolfFang,#StaffFireball," +
            "#StaffIceShards,#StaffShield,#StaffSkeleton,#Stone,#SurtlingCore,#SwordBlackmetal,#SwordBronze,#SwordIron,#SwordIronFire,#SwordMistwalker,#SwordSilver," +
            "#Tankard,#Tankard_dvergr,#TankardAnniversary,#TankardOdin,#Tar,#Thistle,#THSwordKrom,#Thunderstone,#Tin,#TinOre,#Torch,#TorchMist,#TrollHide,#TrophyAbomination," +
            "#TrophyBlob,#TrophyBoar,#TrophyBonemass,#TrophyCultist,#TrophyCultist_Hildir,#TrophyDeathsquito,#TrophyDeer,#TrophyDragonQueen,#TrophyDraugr,#TrophyDraugrElite," +
            "#TrophyDraugrFem,#TrophyDvergr,#TrophyEikthyr,#TrophyFenring,#TrophyForestTroll,#TrophyFrostTroll,#TrophyGjall,#TrophyGoblin,#TrophyGoblinBrute," +
            "#TrophyGoblinBruteBrosBrute,#TrophyGoblinBruteBrosShaman,#TrophyGoblinKing,#TrophyGoblinShaman,#TrophyGreydwarf,#TrophyGreydwarfBrute,#TrophyGreydwarfShaman," +
            "#TrophyGrowth,#TrophyHare,#TrophyHatchling,#TrophyLeech,#TrophyLox,#TrophyNeck,#TrophySeeker,#TrophySeekerBrute,#TrophySeekerQueen,#TrophySerpent," +
            "#TrophySGolem,#TrophySkeleton,#TrophySkeletonHildir,#TrophySkeletonPoison,#TrophySurtling,#TrophyTheElder,#TrophyTick,#TrophyUlv,#TrophyWolf,#TrophyWraith," +
            "#Turnip,#TurnipSeeds,#TurnipStew,#TurretBolt,#TurretBoltWood,#Wishbone,#Wisp,#WitheredBone,#WolfClaw,#WolfFang,#WolfHairBundle,#WolfJerky,#WolfMeat," +
            "#WolfMeatSkewer,#WolfPelt,#Wood,#YagluthDrop,#YggdrasilPorridge,#YggdrasilWood,#YmirRemains";

        private void Awake() {
            AutoPickupIgnoreList = Config.Bind("General", "AutoPickupIgnoreList", DefaultItemList, "Comma-separated list of items to ignore auto-pickup. Remove # before item to add to ignore list.");
            IgnoreList = AutoPickupIgnoreList.Value.Split(',').Where(i => !i.Trim().StartsWith("#")).ToList();
            ToggleBehaviorHotkey = Config.Bind("General", "BehaviorHotkey", new KeyboardShortcut(KeyCode.Quote), "Hotkey to change pickup behavior between custom ignore, ignore all, and default behavior");
            Game.isModded = true;
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

        [HarmonyPatch(typeof(MessageHud), "Awake")]
        class MessageHud_Awake_Patch
        {
            [HarmonyPostfix]
            static void GetMessageHud(ref MessageHud ___m_instance) {
                MessageHud = ___m_instance;
            }
        }

        [HarmonyPatch(typeof(Player), "AutoPickup")]
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
    }
}
