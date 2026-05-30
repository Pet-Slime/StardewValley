using System;
using System.Collections.Generic;
using System.Linq;
using LuckSkill.Objects;
using MoonShared;
using MoonShared.Attributes;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;

namespace LuckSkill.Core
{
    [SEvent]
    public class Events
    {
        private const string SyncedLuckLevelKey = "moonslime.LuckSkill.syncedBaseLuckLevel";
        private const int AssistantExtraQuestChecks = 2;

        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Log.Trace("Luck: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Luck_Skill());

            SpaceEvents.ChooseNightlyFarmEvent += ChangeFarmEvent;
        }

        [SEvent.SaveCreated]
        private static void SaveCreated(object sender, SaveCreatedEventArgs e)
        {
            SyncSpaceCoreLuckToVanilla(Game1.player);
        }

        [SEvent.SaveLoaded]
        private static void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            SyncSpaceCoreLuckToVanilla(Game1.player);
            MoonSharedSpaceCore.SpaceUtilities.LearnRecipesOnLoad(Game1.GetPlayer(Game1.player.UniqueMultiplayerID), ModEntry.SkillID);
        }

        [SEvent.TimeChanged]
        private static void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            SyncSpaceCoreLuckToVanilla(Game1.player);

            var player = Game1.player;
            Log.Debug($"Luck: base luckLevel.Value={player.luckLevel.Value}, total LuckLevel={player.LuckLevel}, SpaceCore luck level={player.GetCustomSkillLevel(ModEntry.SkillID)}, buff luck={player.buffs.LuckLevel}");
        }

        [SEvent.DayStarted]
        private static void DayStarted(object sender, DayStartedEventArgs e)
        {
            SyncSpaceCoreLuckToVanilla(Game1.player);

            if (!Game1.IsMasterGame)
                return;

            Log.Trace("Running Luck Skill Day events.");

            bool forceMaxDailyLuck = false;
            bool raiseLuckFloor = false;
            int gamblerCount = 0;
            int extraQuestChecks = 0;

            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                SyncSpaceCoreLuckToVanilla(farmer);

                int exp = (int)(farmer.team.sharedDailyLuck.Value * ModEntry.Config.DailyLuckExpBonus);
                Utilities.AddEXP(farmer, Math.Max(0, exp));

                if (farmer.HasCustomProfession(Luck_Skill.Luck5a))
                    gamblerCount++;

                if (farmer.HasCustomProfession(Luck_Skill.Luck10a1) && RollLuckyProfession(farmer))
                    forceMaxDailyLuck = true;

                if (farmer.HasCustomProfession(Luck_Skill.Luck10a2) && farmer.team.sharedDailyLuck.Value < 0)
                    raiseLuckFloor = true;

                if (farmer.HasCustomProfession(Luck_Skill.Luck5b) && Game1.questOfTheDay == null && CanHaveQuestOfTheDay())
                    extraQuestChecks += AssistantExtraQuestChecks;
            }

            if (raiseLuckFloor)
            {
                Log.Trace("A player has Fortune Teller, raising daily luck floor to 0.");
                Game1.player.team.sharedDailyLuck.Value = Math.Max(0, Game1.player.team.sharedDailyLuck.Value);
            }

            if (gamblerCount > 0)
            {
                double luckIncrease = gamblerCount * 0.01;
                Log.Trace($"Increasing shared daily luck by {luckIncrease} from Gambler.");
                Game1.player.team.sharedDailyLuck.Value += luckIncrease;
            }

            if (forceMaxDailyLuck)
            {
                Log.Trace("A player passed their Lucky check. Raising daily luck to at least 0.12.");
                Game1.player.team.sharedDailyLuck.Value = Math.Max(Game1.player.team.sharedDailyLuck.Value, 0.12);
            }

            TryApplyAssistantQuest(extraQuestChecks);
        }

        private static bool CanHaveQuestOfTheDay()
        {
            return !Utility.isFestivalDay(Game1.dayOfMonth, Game1.season) && !Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.season);
        }

        private static bool RollLuckyProfession(Farmer farmer)
        {
            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, farmer.UniqueMultiplayerID, 5105);
            return random.NextDouble() <= 0.20;
        }

        private static void TryApplyAssistantQuest(int extraQuestChecks)
        {
            if (extraQuestChecks <= 0 || Game1.questOfTheDay != null)
                return;

            Log.Trace($"Assistant added {extraQuestChecks} extra quest checks.");

            Quest quest = null;
            for (int i = 1; i <= extraQuestChecks && quest == null; i++)
            {
                quest = Utilities.GetQuestOfTheDay(i);
                if (quest == null)
                    Log.Info($"Assistant quest check {i} failed.");
            }

            if (quest == null)
                return;

            Log.Trace($"Applying quest {quest} for today due to Assistant.");

            quest.dailyQuest.Set(newValue: true);
            quest.reloadObjective();
            quest.reloadDescription();
            Game1.netWorldState.Value.SetQuestOfTheDay(quest);
        }

        private static void ChangeFarmEvent(object sender, EventArgsChooseNightlyFarmEvent args)
        {
            if (!Game1.IsMasterGame)
                return;

            if (!HasAnyOnlineFarmerWithProfession(Luck_Skill.Luck10b1))
                return;

            if (Game1.weddingToday || !CanReplaceNightEvent(args.NightEvent))
                return;

            FarmEvent ev = Utilities.PickFarmEvent(99999);

            if (ev != null && ev.setUp())
                ev = null;

            if (ev == null)
                return;

            Log.Info($"Applying {ev} as tonight's nightly event due to Shooting Star.");
            args.NightEvent = ev;
        }

        private static bool CanReplaceNightEvent(FarmEvent nightEvent)
        {
            if (nightEvent == null)
                return true;

            if (nightEvent is not SoundInTheNightEvent)
                return false;

            return ModEntry.Instance.Helper.Reflection.GetField<NetInt>(nightEvent, "behavior").GetValue().Value == 2;
        }

        [SEvent.DayEnding]
        private static void OnDayEnding(object sender, DayEndingEventArgs args)
        {
            if (!Game1.IsMasterGame)
                return;

            int totalRolls = CountSpiritChildGiftRolls();
            if (totalRolls <= 0)
                return;

            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, 10003);
            List<GameLocation> farmAdvanceLocations = GetFarmAdvanceLocations();
            List<AnimalHouse> eligibleAnimalHouses = GetEligibleAnimalHouses();

            for (int i = 0; i < totalRolls; i++)
            {
                if (random.NextDouble() > 0.15)
                    continue;

                if (random.NextDouble() <= 0.05 && Game1.player.addItemToInventoryBool(new StardewValley.Object(StardewValley.Object.prismaticShardID, 1)))
                {
                    Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.prismatic-shard"));
                    break;
                }

                List<Action> rewards = new()
                {
                    () => AdvanceCrops(farmAdvanceLocations),
                    () => AdvanceCrops(farmAdvanceLocations),
                    () => AdvanceCrops(farmAdvanceLocations),
                    GrassAndFences
                };

                foreach (AnimalHouse house in eligibleAnimalHouses)
                {
                    AnimalHouse capturedHouse = house;
                    rewards.Add(() => AdvanceBarn(capturedHouse));
                }

                rewards[random.Next(rewards.Count)]();
                break;
            }
        }

        private static int CountSpiritChildGiftRolls()
        {
            int totalRolls = 0;

            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (!farmer.HasCustomProfession(Luck_Skill.Luck10b2))
                    continue;

                totalRolls += farmer.friendshipData.Values.Count(data => data.GiftsToday > 0);
            }

            return totalRolls;
        }

        private static List<GameLocation> GetFarmAdvanceLocations()
        {
            List<GameLocation> locations = Game1.locations.Where(loc => loc != null).Distinct().ToList();

            foreach (GameLocation loc in Game1.locations.ToList())
            {
                if (loc == null || !loc.IsBuildableLocation())
                    continue;

                foreach (var building in loc.buildings.ToList())
                {
                    GameLocation indoors = building?.indoors.Value;
                    if (indoors != null && !locations.Contains(indoors))
                        locations.Add(indoors);
                }
            }

            return locations;
        }

        private static List<AnimalHouse> GetEligibleAnimalHouses()
        {
            List<AnimalHouse> houses = new();

            foreach (GameLocation loc in Game1.locations.ToList())
            {
                if (loc == null || !loc.IsBuildableLocation())
                    continue;

                foreach (var building in loc.buildings.ToList())
                {
                    if (building?.indoors.Value is not AnimalHouse house)
                        continue;

                    if (house.Animals.Values.ToList().Any(animal => animal.friendshipTowardFarmer.Value < 1000))
                        houses.Add(house);
                }
            }

            return houses;
        }

        private static void AdvanceCrops(IEnumerable<GameLocation> locs)
        {
            foreach (GameLocation loc in locs.Where(loc => loc != null).Distinct())
            {
                foreach (var entry in loc.objects.Pairs.ToList())
                {
                    if (entry.Value is not IndoorPot pot)
                        continue;

                    HoeDirt dirt = pot.hoeDirt.Value;
                    if (dirt?.crop == null || dirt.crop.fullyGrown.Value)
                        continue;

                    dirt.crop.newDay(1);
                }

                foreach (var entry in loc.terrainFeatures.Pairs.ToList())
                {
                    switch (entry.Value)
                    {
                        case HoeDirt dirt when dirt.crop != null && !dirt.crop.fullyGrown.Value:
                            dirt.crop.newDay(1);
                            break;

                        case FruitTree fruitTree:
                            fruitTree.dayUpdate();
                            break;

                        case Tree tree:
                            tree.dayUpdate();
                            break;
                    }
                }
            }

            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.grow-crops"));
        }

        private static void AdvanceBarn(AnimalHouse house)
        {
            foreach (var animal in house.Animals.Values.ToList())
                animal.friendshipTowardFarmer.Value = Math.Min(1000, animal.friendshipTowardFarmer.Value + 100);

            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.animal-friendship"));
        }

        private static void GrassAndFences()
        {
            Farm farm = Game1.getFarm();

            foreach (var entry in farm.terrainFeatures.Pairs.ToList())
            {
                if (entry.Value is Grass grass)
                    grass.numberOfWeeds.Value = 4;
            }

            foreach (var entry in farm.Objects.Pairs.ToList())
            {
                if (entry.Value is Fence fence)
                    fence.repair();
            }

            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.grow-grass"));
        }

        private static bool HasAnyOnlineFarmerWithProfession(KeyedProfession professionId)
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.HasCustomProfession(professionId))
                    return true;
            }

            return false;
        }

        private static void SyncSpaceCoreLuckToVanilla(Farmer farmer)
        {
            if (farmer is null)
                return;

            if (!Game1.IsMasterGame && !farmer.IsLocalPlayer)
                return;

            int spaceCoreLuckLevel = Utilities.GetLevel(farmer);

            if (farmer.luckLevel.Value != spaceCoreLuckLevel)
            {
                Log.Trace($"Syncing {farmer.Name}'s base vanilla luck from {farmer.luckLevel.Value} to SpaceCore luck level {spaceCoreLuckLevel}.");
                farmer.luckLevel.Value = spaceCoreLuckLevel;
            }

            farmer.experiencePoints[Farmer.luckSkill] = GetXPForLevel(spaceCoreLuckLevel);
            farmer.modDataForSerialization[SyncedLuckLevelKey] = spaceCoreLuckLevel.ToString();
        }

        public static int GetXPForLevel(int level)
        {
            if (level <= 0)
                return 0;

            int[] xpTable = new int[]
            {
                100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000,
                20000, 25000, 30000, 35000, 40000, 45000, 50000, 55000, 60000, 70000
            };

            if (level <= 20)
                return xpTable[level - 1];

            int lastXP = xpTable[19];
            int extraLevels = level - 20;
            double multiplier = 1.0 + 0.05 * extraLevels;
            double xpBeyond20 = lastXP + extraLevels * 5000 * multiplier;

            return (int)Math.Round(xpBeyond20);
        }
    }
}
