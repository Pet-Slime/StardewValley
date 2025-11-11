using System;
using MoonShared.Attributes;
using StardewValley;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Network.NetEvents;
using StardewValley.Quests;

namespace LuckSkill.Core
{
    internal class Utilities
    {
        public static void AddEXP(Farmer who, int amount)
        {
            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
            SpaceCore.Skills.AddExperience(farmer, "moonslime.Luck", amount);
        }

        public static int GetLevel(Farmer who)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, "moonslime.Luck");
        }

        // Summary:
        //     Get the help-wanted quest to show on Pierre's bulletin board today, if any.
        public static Quest GetQuestOfTheDay(double seedEnhancer)
        {
            if (Game1.stats.DaysPlayed <= 1)
            {
                return null;
            }

            double num = Utility.CreateDaySaveRandom(100.0, Game1.stats.DaysPlayed * 777, seedEnhancer).NextDouble();
            Log.Info($"Quest RNG value is {num}");
            if (num < 0.08)
            {
                return new ResourceCollectionQuest();
            }

            if (num < 0.2 && MineShaft.lowestLevelReached > 0 && Game1.stats.DaysPlayed > 5)
            {
                return new SlayMonsterQuest
                {
                    ignoreFarmMonsters = { true }
                };
            }

            if (num < 0.5)
            {
                return null;
            }

            if (num < 0.6)
            {
                return new FishingQuest();
            }

            if (num < 0.66 && Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Mon"))
            {
                bool flag = false;
                foreach (Farmer allFarmer in Game1.getAllFarmers())
                {
                    foreach (Quest item in allFarmer.questLog)
                    {
                        if (item is SocializeQuest)
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (flag)
                    {
                        break;
                    }
                }

                if (!flag)
                {
                    return new SocializeQuest();
                }

                return new ItemDeliveryQuest();
            }

            return new ItemDeliveryQuest();
        }

        public static FarmEvent PickFarmEvent(double seedEnhancer)
        {
            Random random = Utility.CreateDaySaveRandom(100.0, Game1.stats.DaysPlayed * 777, seedEnhancer);
            for (int i = 0; i < 10; i++)
            {
                random.NextDouble();
            }

            if (Game1.weddingToday)
            {
                return null;
            }

            foreach (Farmer onlineFarmer in Game1.getOnlineFarmers())
            {
                Friendship spouseFriendship = onlineFarmer.GetSpouseFriendship();
                if (spouseFriendship != null && spouseFriendship.IsMarried() && spouseFriendship.WeddingDate == Game1.Date)
                {
                    return null;
                }
            }

            double num = Game1.getFarm().hasMatureFairyRoseTonight ? 0.007 : 0.0;
            Game1.getFarm().hasMatureFairyRoseTonight = false;
            if (random.NextDouble() < 0.02 + num && !Game1.IsWinter && Game1.dayOfMonth != 1)
            {
                return new FairyEvent();
            }

            if (random.NextDouble() < 0.04 && Game1.stats.DaysPlayed > 20)
            {
                return new WitchEvent();
            }

            if (random.NextDouble() < 0.01 && Game1.stats.DaysPlayed > 5)
            {
                return new SoundInTheNightEvent(1);
            }

            if (random.NextDouble() < 0.005)
            {
                return new SoundInTheNightEvent(3);
            }

            if (random.NextDouble() < 0.008 && Game1.year > 1 && !Game1.MasterPlayer.mailReceived.Contains("Got_Capsule"))
            {
                Game1.player.team.RequestSetMail(PlayerActionTarget.Host, "Got_Capsule", MailType.Received, add: true);
                return new SoundInTheNightEvent(0);
            }

            return null;
        }
    }
}
