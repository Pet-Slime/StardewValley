using System;
using System.Collections.Generic;
using System.Linq;
using LuckSkill.Objects;
using MoonShared.Attributes;
using Netcode;
using SpaceCore;
using SpaceCore.Events;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;

namespace LuckSkill.Core
{
    [SEvent]
    public class Events
    {
        // Cached deterministic RNG to ensure consistent daily luck/random events
        private static Random CachedRandom;

        // Fired after the game is launched
        [SEvent.GameLaunchedLate]
        private static void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Log.Trace("Luck: Trying to Register skill.");
            SpaceCore.Skills.RegisterSkill(new Luck_Skill());

            // Hook into the nightly farm event system for custom Luck10b1 behavior
            SpaceEvents.ChooseNightlyFarmEvent += ChangeFarmEvent;
        }

        // Fired when the in-game time changes
        [SEvent.TimeChanged]
        private static void TimeChanged(object sender, TimeChangedEventArgs e)
        {
            LuckSkill(Game1.player); // Update luck skill for the main player
            Log.Trace("Luck: Player luck level is: " + Game1.player.LuckLevel.ToString());
        }

        // Fired when a new save is created
        [SEvent.SaveCreated]
        private static void SaveCreated(object sender, SaveCreatedEventArgs e)
        {
            LuckSkill(Game1.player); // Initialize luck skill for new save
        }

        // Fired when a save is loaded
        [SEvent.SaveLoaded]
        private static void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Initialize deterministic RNG based on day and player steps for repeatable daily events
            CachedRandom = Utility.CreateDaySaveRandom(100.0, Game1.stats.DaysPlayed * 777, Game1.player.stats.StepsTaken);
            LuckSkill(Game1.player);

            var thing = SpaceCore.Skills.GetSkill("moonslime.Luck");
            foreach (var item in thing.Professions)
            {
                Game1.player.professions.Add(item.GetVanillaId());
            }
            ;
        }

        // Fired at the start of each day
        [SEvent.DayStarted]
        private static void DayStarted(object sender, DayStartedEventArgs e)
        {
            // Update RNG for today to be deterministic
            CachedRandom = Utility.CreateDaySaveRandom(100.0, Game1.stats.DaysPlayed * 777, Game1.player.stats.StepsTaken);

            // Only run the master game (host) for multiplayer
            if (!Game1.IsMasterGame)
                return;

            bool maxDailyLuck = false; // Flag for raising daily luck to 0.12
            int luckincrease = 0;      // Number of players with Luck5a to increase team luck
            bool raiseLuckFloor = false; // Flag for ensuring negative luck floor is 0
            int questchecks = 0;       // How many attempts we do for assigning a quest

            // Loop through all online players to calculate luck effects
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                // Award experience based on daily luck
                int exp = (int)(farmer.team.sharedDailyLuck.Value * ModEntry.Config.DailyLuckExpBonus);
                Utilities.AddEXP(farmer, Math.Max(0, exp));

                // Check for Luck5a profession for minor daily luck increase
                if (farmer.HasCustomProfession(Luck_Skill.Luck5a))
                {
                    luckincrease += 1;

                    // Check for Luck10a1 (chance for max daily luck)
                    if (farmer.HasCustomProfession(Luck_Skill.Luck10a1))
                    {
                        if (CachedRandom.NextDouble() <= 0.20)
                        {
                            maxDailyLuck = true;
                        }
                    }

                    // Check for Luck10a2 (prevent negative daily luck)
                    if (farmer.HasCustomProfession(Luck_Skill.Luck10a2))
                    {
                        if (farmer.team.sharedDailyLuck.Value < 0)
                            raiseLuckFloor = true;
                    }
                }

                // Check for Luck5b (additional quest generation)
                if (farmer.HasCustomProfession(Luck_Skill.Luck5b) && Game1.questOfTheDay == null)
                {
                    if (!Utility.isFestivalDay() && !Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.season))
                    {

                        Log.Warn($"A player has Luck5b, quest of the day is null, and tomorrow is not a festival! increasing quest checks by 3.");
                        questchecks += ModEntry.Config.QuestChecks; // Increase number of attempts for generating a quest
                    }
                }

                LuckSkill(farmer); // Update individual farmer's luck skill values
            }

            Log.Warn($"Master player is {Game1.player.Name}");
            Log.Warn($"Running Luck Skill Day events");
            // Enforce daily luck floor if any player has Luck10a2
            if (raiseLuckFloor)
            {
                Log.Warn($"A player has Luck10a2, raising luck floor to 0");
                Game1.player.team.sharedDailyLuck.Value = 0;
            }

            // Apply cumulative minor daily luck increases for Luck5a players
            if (luckincrease != 0)
            {
                double decimalLuckIncrease = luckincrease * 0.01;

                Log.Warn($"Increasing shared luck by {decimalLuckIncrease}");
                Game1.player.team.sharedDailyLuck.Value += decimalLuckIncrease;
            }

            // Apply max daily luck for Luck10a1 if the RNG triggered
            if (maxDailyLuck)
            {
                Log.Warn($"A player passed their check to max daily luck out.");
                Game1.player.team.sharedDailyLuck.Value = Math.Max(Game1.player.team.sharedDailyLuck.Value, 0.12);
            }

            // Assign quest of the day based on Luck5b profession
            if (questchecks != 0)
            {

                Log.Warn($"A player has added to the quest checks. current quest check value is {questchecks}");
                Quest quest = null;
                for (int i = 0; i < questchecks && quest == null; ++i)
                {
                    quest = Utilities.GetQuestOfTheDay(DateTime.Now.Ticks); // Use deterministic RNG for quest
                    if (quest == null)
                    {
                        Log.Warn($"Quest is null, failed check number {i+1}");
                    }
                }



                if (quest != null)
                {
                    Log.Info($"Applying quest {quest} for today, due to having PROFESSION_MOREQUESTS.");
                    quest?.dailyQuest.Set(newValue: true);
                    quest?.reloadObjective();
                    quest?.reloadDescription();
                    Game1.netWorldState.Value.SetQuestOfTheDay(quest); // Sync across network
                }
            }
        }

        // Hook to modify nightly farm events for Luck10b1 profession
        private static void ChangeFarmEvent(object sender, EventArgsChooseNightlyFarmEvent args)
        {
            if (Game1.player.HasCustomProfession(Luck_Skill.Luck10b1) && !Game1.weddingToday &&
                (args.NightEvent == null || (args.NightEvent is SoundInTheNightEvent &&
                ModEntry.Instance.Helper.Reflection.GetField<NetInt>(args.NightEvent, "behavior").GetValue().Value == 2)))
            {
                // Pick a farm event using deterministic RNG
                FarmEvent ev = Utilities.PickFarmEvent(DateTime.Now.Ticks);

                // Ensure the event can actually be set up; if not, discard
                if (ev != null && ev.setUp())
                {
                    ev = null;
                }

                if (ev != null)
                {
                    Log.Info($"Applying {ev} as tonight's nightly event, due to having PROFESSION_NIGHTLY_EVENTS");
                    args.NightEvent = ev;
                }
            }
        }

        [SEvent.DayEnding]
        private static void OnDayEnding(object sender, DayEndingEventArgs args)
        {
            // Only the master game should handle shared farm-wide effects.
            if (!Game1.IsMasterGame)
                return;

            // Phase 1: aggregate total rolls from all farmers who have Luck10b2 and gave gifts today.
            int totalRolls = 0;
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (!farmer.HasCustomProfession(Luck_Skill.Luck10b2))
                    continue;

                totalRolls += farmer.friendshipData.Values.Count(d => d.GiftsToday > 0);
            }

            if (totalRolls <= 0)
                return;

            // Cache and prepare shared context once
            List<AnimalHouse> eligibleBarns = new();
            List<GameLocation> allLocations = new(Game1.locations);
            foreach (var loc in Game1.locations)
            {
                if (loc.IsBuildableLocation())
                {
                    allLocations.AddRange(loc.buildings
                        .Select(b => b.indoors.Value)
                        .Where(i => i != null));

                    foreach (var building in loc.buildings)
                    {
                        if (building.indoors.Value is AnimalHouse ah &&
                            ah.Animals.Values.Any(a => a.friendshipTowardFarmer.Value < 1000))
                        {
                            eligibleBarns.Add(ah);
                        }
                    }
                }
            }

            // --- Phase 2: resolve global effect rolls ---
            for (int i = 0; i < totalRolls; i++)
            {
                // 15% chance to trigger any reward roll
                if (CachedRandom.NextDouble() > 0.15)
                    continue;

                // 5% chance: prismatic shard reward
                if (CachedRandom.NextDouble() <= 0.05 &&
                    Game1.player.addItemToInventoryBool(new StardewValley.Object(StardewValley.Object.prismaticShardID, 1)))
                {
                    Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.prismatic-shard"));
                    break;
                }

                // Build action pool — crops favored slightly more often
                List<Action> rewards = new()
                    {
                        () => AdvanceCrops(allLocations),
                        () => AdvanceCrops(allLocations),
                        () => AdvanceCrops(allLocations)
                    };

                foreach (var barn in eligibleBarns)
                    rewards.Add(() => AdvanceBarn(barn));

                rewards.Add(GrassAndFences);

                // Randomly pick one reward to apply globally
                rewards[CachedRandom.Next(rewards.Count)]();
                break;
            }
        }

        private static void AdvanceCrops(IEnumerable<GameLocation> locs)
        {
            foreach (var loc in locs)
            {
                foreach (var obj in loc.objects.Values.OfType<IndoorPot>())
                {
                    var dirt = obj.hoeDirt.Value;
                    if (dirt?.crop != null && !dirt.crop.fullyGrown.Value)
                        dirt.crop.newDay(1);
                }

                foreach (var tf in loc.terrainFeatures.Values)
                {
                    switch (tf)
                    {
                        case HoeDirt dirt when dirt.crop != null && !dirt.crop.fullyGrown.Value:
                            dirt.crop.newDay(1);
                            break;
                        case FruitTree ft:
                            ft.dayUpdate();
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
            foreach (var animal in house.Animals.Values)
                animal.friendshipTowardFarmer.Value = Math.Min(1000, animal.friendshipTowardFarmer.Value + 100);

            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.animal-friendship"));
        }


        private static void GrassAndFences()
        {
            var farm = Game1.getFarm();
            foreach (var entry in farm.terrainFeatures.Values.OfType<Grass>())
                entry.numberOfWeeds.Value = 4;

            foreach (var fence in farm.Objects.Values.OfType<Fence>())
                fence.repair();

            Game1.showGlobalMessage(ModEntry.Instance.I18N.Get("junimo-rewards.grow-grass"));
        }


        private static void LuckSkill(Farmer farmer)
        {
            // Skip null or non-local players entirely
            if (farmer is null || !farmer.IsLocalPlayer)
                return;

            const string key = "moonslime.LuckSkill.skillValue";
            int currentSkillLevel = Utilities.GetLevel(farmer);
            int storedSkillLevel = 0;

            // Try get cached level once
            if (farmer.modDataForSerialization.TryGetValue(key, out string stored))
                _ = int.TryParse(stored, out storedSkillLevel);

            // Only apply changes if needed
            if (currentSkillLevel != storedSkillLevel)
            {
                const int skillIndex = 5; // luck skill slot
                int delta = currentSkillLevel - storedSkillLevel;

                // Adjust player's luck and XP directly
                farmer.luckLevel.Value += delta;
                farmer.experiencePoints[skillIndex] = GetXPForLevel(currentSkillLevel);

                // Cache updated value
                farmer.modDataForSerialization[key] = currentSkillLevel.ToString();
            }
        }


        // Return XP required for a given luck skill level
        public static int GetXPForLevel(int level)
        {
            if (level < 1) level = 1;

            // --- 1–20: exact XP table ---
            int[] xpTable = new int[]
            {
                100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000,
                20000, 25000, 30000, 35000, 40000, 45000, 50000, 55000, 60000, 70000
            };

            if (level <= 20)
                return xpTable[level - 1]; // exact match

            // --- 21+: continuous formula ---
            int lastXP = xpTable[19];        // XP at level 20
            int extraLevels = level - 20;

            // Slightly accelerating growth per level (5%)
            double multiplier = 1.0 + 0.05 * extraLevels;
            double xpBeyond20 = lastXP + extraLevels * 5000 * multiplier;

            return (int)Math.Round(xpBeyond20);
        }
    }
}
