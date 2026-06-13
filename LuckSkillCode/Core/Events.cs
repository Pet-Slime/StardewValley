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
            Log.Trace(() => "Luck: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Luck_Skill());

            SpaceEvents.ChooseNightlyFarmEvent += ChangeFarmEvent;
            Log.Trace(() => "Luck: Registered ChooseNightlyFarmEvent handler.");
        }

        [SEvent.SaveCreated]
        private static void SaveCreated(object sender, SaveCreatedEventArgs e)
        {
            Log.Trace(() => "Luck: SaveCreated sync started.");
            SyncSpaceCoreLuckToVanilla(Game1.player);
        }

        [SEvent.SaveLoaded]
        private static void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            Log.Trace(() => "Luck: SaveLoaded sync started.");
            SyncSpaceCoreLuckToVanilla(Game1.player);
            MoonSharedSpaceCore.SpaceUtilities.LearnRecipesOnLoad(Game1.GetPlayer(Game1.player.UniqueMultiplayerID), ModEntry.SkillID);
            Log.Trace(() => "Luck: SaveLoaded recipe learning check finished.");
        }

        [SEvent.TimeChanged]
        private static void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            SyncSpaceCoreLuckToVanilla(Game1.player);

            var player = Game1.player;
            Log.Mute(() => $"Luck: base luckLevel.Value={player.luckLevel.Value}, total LuckLevel={player.LuckLevel}, SpaceCore luck level={player.GetCustomSkillLevel(ModEntry.SkillID)}, buff luck={player.buffs.LuckLevel}");
        }

        [SEvent.DayStarted]
        private static void DayStarted(object sender, DayStartedEventArgs e)
        {
            SyncSpaceCoreLuckToVanilla(Game1.player);

            if (!Game1.IsMasterGame)
            {
                Log.Mute(() => "Luck: Skipping DayStarted luck events because this instance is not the master game.");
                return;
            }

            Log.Mute(() => "Running Luck Skill Day events.");
            Log.Mute(() => $"Luck DayStarted: Starting sharedDailyLuck={Game1.player.team.sharedDailyLuck.Value}, questOfTheDay={(Game1.questOfTheDay == null ? "null" : Game1.questOfTheDay.ToString())}.");

            bool forceMaxDailyLuck = false;
            bool raiseLuckFloor = false;
            int gamblerCount = 0;
            int extraQuestChecks = 0;

            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                SyncSpaceCoreLuckToVanilla(farmer);

                int exp = (int)(farmer.team.sharedDailyLuck.Value * ModEntry.Config.DailyLuckExpBonus);
                Utilities.AddEXP(farmer, Math.Max(0, exp));
                Log.Mute(() => $"Luck DayStarted: {farmer.Name} daily luck EXP calculation: sharedDailyLuck={farmer.team.sharedDailyLuck.Value}, DailyLuckExpBonus={ModEntry.Config.DailyLuckExpBonus}, rawExp={exp}, awardedExp={Math.Max(0, exp)}.");

                if (farmer.HasCustomProfession(Luck_Skill.Luck5a))
                {
                    gamblerCount++;
                    Log.Mute(() => $"Luck DayStarted: {farmer.Name} has Gambler. gamblerCount={gamblerCount}.");
                }

                if (farmer.HasCustomProfession(Luck_Skill.Luck10a1) && RollLuckyProfession(farmer))
                {
                    forceMaxDailyLuck = true;
                    Log.Mute(() => $"Luck DayStarted: {farmer.Name} passed Lucky. forceMaxDailyLuck={forceMaxDailyLuck}.");
                }

                if (farmer.HasCustomProfession(Luck_Skill.Luck10a2) && farmer.team.sharedDailyLuck.Value < 0)
                {
                    raiseLuckFloor = true;
                    Log.Mute(() => $"Luck DayStarted: {farmer.Name} has Fortune Teller and sharedDailyLuck is negative. raiseLuckFloor={raiseLuckFloor}.");
                }

                if (farmer.HasCustomProfession(Luck_Skill.Luck5b) && Game1.questOfTheDay == null && CanHaveQuestOfTheDay())
                {
                    extraQuestChecks += AssistantExtraQuestChecks;
                    Log.Mute(() => $"Luck DayStarted: {farmer.Name} has Assistant. Added {AssistantExtraQuestChecks} quest checks. extraQuestChecks={extraQuestChecks}.");
                }
            }

            Log.Mute(() => $"Luck DayStarted summary before applying bonuses: gamblerCount={gamblerCount}, forceMaxDailyLuck={forceMaxDailyLuck}, raiseLuckFloor={raiseLuckFloor}, extraQuestChecks={extraQuestChecks}, sharedDailyLuck={Game1.player.team.sharedDailyLuck.Value}.");

            if (raiseLuckFloor)
            {
                Log.Mute(() => "A player has Fortune Teller, raising daily luck floor to 0.");
                Game1.player.team.sharedDailyLuck.Value = Math.Max(0, Game1.player.team.sharedDailyLuck.Value);
                Log.Mute(() => $"Luck DayStarted: sharedDailyLuck after Fortune Teller={Game1.player.team.sharedDailyLuck.Value}.");
            }

            if (gamblerCount > 0)
            {
                double luckIncrease = gamblerCount * 0.01;
                Log.Mute(() => $"Increasing shared daily luck by {luckIncrease} from Gambler.");
                Game1.player.team.sharedDailyLuck.Value += luckIncrease;
                Log.Mute(() => $"Luck DayStarted: sharedDailyLuck after Gambler={Game1.player.team.sharedDailyLuck.Value}.");
            }

            if (forceMaxDailyLuck)
            {
                Log.Mute(() => "A player passed their Lucky check. Raising daily luck to at least 0.12.");
                Game1.player.team.sharedDailyLuck.Value = Math.Max(Game1.player.team.sharedDailyLuck.Value, 0.12);
                Log.Mute(() => $"Luck DayStarted: sharedDailyLuck after Lucky={Game1.player.team.sharedDailyLuck.Value}.");
            }

            TryApplyAssistantQuest(extraQuestChecks);
            Log.Mute(() => $"Luck DayStarted complete: final sharedDailyLuck={Game1.player.team.sharedDailyLuck.Value}, questOfTheDay={(Game1.questOfTheDay == null ? "null" : Game1.questOfTheDay.ToString())}.");
        }

        private static bool CanHaveQuestOfTheDay()
        {
            return !Utility.isFestivalDay(Game1.dayOfMonth, Game1.season) && !Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.season);
        }

        private static bool RollLuckyProfession(Farmer farmer)
        {
            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, farmer.UniqueMultiplayerID, 5105);
            double roll = random.NextDouble();
            bool result = roll <= 0.20;
            Log.Mute(() => $"Lucky profession roll for {farmer.Name}: roll={roll}, success={result}.");
            return result;
        }

        private static void TryApplyAssistantQuest(int extraQuestChecks)
        {
            Log.Mute(() => $"Assistant quest check started: extraQuestChecks={extraQuestChecks}, questOfTheDay={(Game1.questOfTheDay == null ? "null" : Game1.questOfTheDay.ToString())}.");

            if (extraQuestChecks <= 0 || Game1.questOfTheDay != null)
            {
                Log.Mute(() => $"Assistant quest check skipped: extraQuestChecks={extraQuestChecks}, questOfTheDay={(Game1.questOfTheDay == null ? "null" : Game1.questOfTheDay.ToString())}.");
                return;
            }

            Log.Mute(() => $"Assistant added {extraQuestChecks} extra quest checks.");

            Quest quest = null;
            for (int i = 1; i <= extraQuestChecks && quest == null; i++)
            {
                quest = Utilities.GetQuestOfTheDay(i);
                if (quest == null)
                    Log.Mute(() => $"Assistant quest check {i} failed.");
                else
                    Log.Mute(() => $"Assistant quest check {i} found quest {quest}.");
            }

            if (quest == null)
            {
                Log.Mute(() => "Assistant quest checks finished without finding a quest.");
                return;
            }

            Log.Mute(() => $"Applying quest {quest} for today due to Assistant.");

            quest.dailyQuest.Set(newValue: true);
            quest.reloadObjective();
            quest.reloadDescription();
            Game1.netWorldState.Value.SetQuestOfTheDay(quest);
            Log.Mute(() => $"Assistant quest applied: questOfTheDay={Game1.questOfTheDay}.");
        }

        private static void ChangeFarmEvent(object sender, EventArgsChooseNightlyFarmEvent args)
        {
            Log.Mute(() => $"Shooting Star nightly event check started: IsMasterGame={Game1.IsMasterGame}, weddingToday={Game1.weddingToday}, currentNightEvent={(args.NightEvent == null ? "null" : args.NightEvent.ToString())}.");

            if (!Game1.IsMasterGame)
            {
                Log.Mute(() => "Shooting Star nightly event check skipped because this instance is not the master game.");
                return;
            }

            if (!HasAnyOnlineFarmerWithProfession(Luck_Skill.Luck10b1))
            {
                Log.Mute(() => "Shooting Star nightly event check skipped because no online farmer has Shooting Star.");
                return;
            }

            if (Game1.weddingToday || !CanReplaceNightEvent(args.NightEvent))
            {
                Log.Mute(() => $"Shooting Star nightly event check skipped: weddingToday={Game1.weddingToday}, currentNightEvent={(args.NightEvent == null ? "null" : args.NightEvent.ToString())}.");
                return;
            }

            FarmEvent ev = Utilities.PickFarmEvent(99999);
            Log.Mute(() => $"Shooting Star picked farm event candidate: {(ev == null ? "null" : ev.ToString())}.");

            if (ev != null && ev.setUp())
            {
                Log.Mute(() => $"Shooting Star event {ev} returned true from setUp(), so it will not be used.");
                ev = null;
            }

            if (ev == null)
            {
                Log.Mute(() => "Shooting Star nightly event check finished without a valid event.");
                return;
            }

            Log.Mute(() => $"Applying {ev} as tonight's nightly event due to Shooting Star.");
            args.NightEvent = ev;
        }

        private static bool CanReplaceNightEvent(FarmEvent nightEvent)
        {
            if (nightEvent == null)
            {
                Log.Mute(() => "Shooting Star can replace nightly event because current night event is null.");
                return true;
            }

            if (nightEvent is not SoundInTheNightEvent)
            {
                Log.Mute(() => $"Shooting Star cannot replace nightly event because current event is {nightEvent}.");
                return false;
            }

            int behavior = ModEntry.Instance.Helper.Reflection.GetField<NetInt>(nightEvent, "behavior").GetValue().Value;
            bool canReplace = behavior == 2;
            Log.Mute(() => $"Shooting Star SoundInTheNightEvent replacement check: behavior={behavior}, canReplace={canReplace}.");
            return canReplace;
        }

        [SEvent.DayEnding]
        private static void OnDayEnding(object sender, DayEndingEventArgs args)
        {
            if (!Game1.IsMasterGame)
            {
                Log.Mute(() => "Luck: Skipping DayEnding luck events because this instance is not the master game.");
                return;
            }

            int totalRolls = CountSpiritChildGiftRolls();
            Log.Mute(() => $"Spirit Child DayEnding reward check: totalRolls={totalRolls}.");

            if (totalRolls <= 0)
            {
                Log.Mute(() => "Spirit Child DayEnding reward check skipped because there are no gift rolls.");
                return;
            }

            Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, 10003);
            List<GameLocation> farmAdvanceLocations = GetFarmAdvanceLocations();
            List<AnimalHouse> eligibleAnimalHouses = GetEligibleAnimalHouses();
            Log.Mute(() => $"Spirit Child reward setup: farmAdvanceLocations={farmAdvanceLocations.Count}, eligibleAnimalHouses={eligibleAnimalHouses.Count}.");

            for (int i = 0; i < totalRolls; i++)
            {
                double rewardRoll = random.NextDouble();
                Log.Mute(() => $"Spirit Child reward roll {i + 1}/{totalRolls}: rewardRoll={rewardRoll}, needed<=0.15.");

                if (rewardRoll > 0.15)
                    continue;

                double prismaticRoll = random.NextDouble();
                Log.Mute(() => $"Spirit Child reward roll {i + 1}/{totalRolls}: prismaticRoll={prismaticRoll}, needed<=0.05.");

                if (prismaticRoll <= 0.05 && Game1.player.addItemToInventoryBool(new StardewValley.Object(StardewValley.Object.prismaticShardID, 1)))
                {
                    Log.Mute(() => "Spirit Child reward: gave prismatic shard.");
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

                int rewardIndex = random.Next(rewards.Count);
                Log.Mute(() => $"Spirit Child reward: selected reward index {rewardIndex} from {rewards.Count} possible rewards.");
                rewards[rewardIndex]();
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

                int giftRolls = farmer.friendshipData.Values.Count(data => data.GiftsToday > 0);
                totalRolls += giftRolls;
                Log.Mute(() => $"Spirit Child gift rolls: farmer={farmer.Name}, giftRolls={giftRolls}, runningTotal={totalRolls}.");
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

            Log.Mute(() => $"Spirit Child farm advance locations found: {locations.Count}.");
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

            Log.Mute(() => $"Spirit Child eligible animal houses found: {houses.Count}.");
            return houses;
        }

        private static void AdvanceCrops(IEnumerable<GameLocation> locs)
        {
            int indoorPotsAdvanced = 0;
            int cropsAdvanced = 0;
            int fruitTreesAdvanced = 0;
            int treesAdvanced = 0;

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
                    indoorPotsAdvanced++;
                }

                foreach (var entry in loc.terrainFeatures.Pairs.ToList())
                {
                    switch (entry.Value)
                    {
                        case HoeDirt dirt when dirt.crop != null && !dirt.crop.fullyGrown.Value:
                            dirt.crop.newDay(1);
                            cropsAdvanced++;
                            break;

                        case FruitTree fruitTree:
                            fruitTree.dayUpdate();
                            fruitTreesAdvanced++;
                            break;

                        case Tree tree:
                            tree.dayUpdate();
                            treesAdvanced++;
                            break;
                    }
                }
            }

            Log.Mute(() => $"Spirit Child AdvanceCrops reward complete: indoorPotsAdvanced={indoorPotsAdvanced}, cropsAdvanced={cropsAdvanced}, fruitTreesAdvanced={fruitTreesAdvanced}, treesAdvanced={treesAdvanced}.");
            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.grow-crops"));
        }

        private static void AdvanceBarn(AnimalHouse house)
        {
            int animalsAdvanced = 0;

            foreach (var animal in house.Animals.Values.ToList())
            {
                animal.friendshipTowardFarmer.Value = Math.Min(1000, animal.friendshipTowardFarmer.Value + 100);
                animalsAdvanced++;
            }

            Log.Mute(() => $"Spirit Child AdvanceBarn reward complete: animalsAdvanced={animalsAdvanced}.");
            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.animal-friendship"));
        }

        private static void GrassAndFences()
        {
            Farm farm = Game1.getFarm();
            int grassTilesUpdated = 0;
            int fencesRepaired = 0;

            foreach (var entry in farm.terrainFeatures.Pairs.ToList())
            {
                if (entry.Value is Grass grass)
                {
                    grass.numberOfWeeds.Value = 4;
                    grassTilesUpdated++;
                }
            }

            foreach (var entry in farm.Objects.Pairs.ToList())
            {
                if (entry.Value is Fence fence)
                {
                    fence.repair();
                    fencesRepaired++;
                }
            }

            Log.Mute(() => $"Spirit Child GrassAndFences reward complete: grassTilesUpdated={grassTilesUpdated}, fencesRepaired={fencesRepaired}.");
            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.grow-grass"));
        }

        private static bool HasAnyOnlineFarmerWithProfession(KeyedProfession professionId)
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.HasCustomProfession(professionId))
                {
                    Log.Mute(() => $"Found online farmer with profession {professionId}: {farmer.Name}.");
                    return true;
                }
            }

            Log.Mute(() => $"No online farmer found with profession {professionId}.");
            return false;
        }

        private static void SyncSpaceCoreLuckToVanilla(Farmer farmer)
        {
            if (farmer is null)
            {
                Log.Mute(() => "Luck sync skipped because farmer is null.");
                return;
            }

            if (!Game1.IsMasterGame && !farmer.IsLocalPlayer)
            {
                Log.Mute(() => $"Luck sync skipped for {farmer.Name} because this instance is not master and the farmer is not local.");
                return;
            }

            int spaceCoreLuckLevel = Utilities.GetLevel(farmer);

            if (farmer.luckLevel.Value != spaceCoreLuckLevel)
            {
                Log.Mute(() => $"Syncing {farmer.Name}'s base vanilla luck from {farmer.luckLevel.Value} to SpaceCore luck level {spaceCoreLuckLevel}.");
                farmer.luckLevel.Value = spaceCoreLuckLevel;
            }

            farmer.experiencePoints[Farmer.luckSkill] = GetXPForLevel(spaceCoreLuckLevel);
            farmer.modDataForSerialization[SyncedLuckLevelKey] = spaceCoreLuckLevel.ToString();
            Log.Mute(() => $"Luck sync complete for {farmer.Name}: vanillaLuck={farmer.luckLevel.Value}, spaceCoreLuck={spaceCoreLuckLevel}, vanillaLuckXP={farmer.experiencePoints[Farmer.luckSkill]}, syncedKey={farmer.modDataForSerialization[SyncedLuckLevelKey]}.");
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
