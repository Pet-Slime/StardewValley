using SpaceCore;
using StardewValley;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ThievingSkill.Core
{
    internal class Utilities
    {
        const string Boo = "moonslime.Thieving";

        public static bool IsBetween(int x, int low, int high)
        {
            return low <= x && x <= high;
        }

        public static void AddEXP(Farmer who, int amount)
        {
            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
            SpaceCore.Skills.AddExperience(farmer, Boo, amount);
        }

        public static int GetLevel(Farmer who)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            return SpaceCore.Skills.GetSkillLevel(player, Boo) + SpaceCore.Skills.GetSkillBuffLevel(player, Boo);
        }

        public static string Text(string str)
        {
            return ModEntry.Instance.I18N.Get(str);
        }

        // Get all NPCs within a tile radius of the player.
        // This is generic and does not require friendship data.
        public static List<NPC> GetNpcsInRange(Farmer who, float tileRange)
        {
            List<NPC> npcsInRange = [];

            if (who == null || who.currentLocation == null)
                return npcsInRange;

            foreach (NPC npc in who.currentLocation.characters)
            {
                if (npc == null)
                    continue;

                float distance = Vector2.Distance(npc.Tile, who.Tile);
                if (distance <= tileRange)
                    npcsInRange.Add(npc);
            }

            return npcsInRange;
        }

        // Check if the NPC has friendship data for this player.
        // This keeps friendship edits safe for vanilla and modded NPCs.
        public static bool HasFriendshipData(Farmer who, NPC npc)
        {
            if (who == null || npc == null)
                return false;

            return who.friendshipData.ContainsKey(npc.Name);
        }

        // Get all NPCs within a tile radius of the player that have friendship data.
        // This is useful for witnesses to lockpicking, shoplifting, and future thieving actions.
        public static List<NPC> GetFriendshipNpcsInRange(Farmer who, float tileRange)
        {
            List<NPC> npcsInRange = [];

            if (who == null || who.currentLocation == null)
                return npcsInRange;

            foreach (NPC npc in GetNpcsInRange(who, tileRange))
            {
                if (!HasFriendshipData(who, npc))
                    continue;

                npcsInRange.Add(npc);
            }

            return npcsInRange;
        }

        // Make all nearby NPCs with friendship data lose friendship.
        // friendshipLoss can be positive or negative; this method always subtracts the absolute value.
        public static int LoseFriendshipWithNpcsInRange(Farmer who, float tileRange, int friendshipLoss)
        {
            if (who == null || friendshipLoss == 0)
                return 0;


            // If the player has the friendship protection profession, they have a 50% chance of ignoring friendship lost
            bool friendshipProtection = who.HasCustomProfession(Events.Proffession5b) && Game1.random.NextDouble() < 0.5;

            if (friendshipProtection)
                return 0;

            int affectedCount = 0;
            int amountToLose = Math.Abs(friendshipLoss);

            foreach (NPC npc in GetFriendshipNpcsInRange(who, tileRange))
            {
                who.friendshipData[npc.Name].Points -= amountToLose;
                affectedCount++;
            }

            return affectedCount;
        }
    }
}
