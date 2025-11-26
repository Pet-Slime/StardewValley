using System.Collections.Generic;
using ArchaeologySkill.Objects;
using Microsoft.Xna.Framework;
using MoonShared;
using MoonShared.Attributes;
using SpaceCore;
using StardewValley;
using StardewValley.Buffs;

namespace ArchaeologySkill.Core
{
    public class Utilities
    {
        /// <summary>
        /// Apply the basic Archaeology skill items. Give who the exp, check to see if they have the gold rush profession, and spawn bonus loot
        /// </summary>
        /// <param name="who"> The player</param>
        /// <param name="bonusLoot"> Do they get bonus loot or not</param>
        /// <param name="Object"> the int that the bonus loot should be</param>
        /// <param name="xLocation">the player's x location</param>
        /// <param name="yLocation">the player's y location</param>
        /// <param name="panning">is the effect from panning, since bonus loot works differently there.</param>
        /// <param name="exactItem">What bonus item if it passes the checks do you want to give the player</param>
        public static void ApplyArchaeologySkill(
            Farmer who,
            int EXP,
            bool bonusLoot = false,
            int xLocation = 0,
            int yLocation = 0,
            bool panning = false,
            string exactItem = ""
        )
        {
            // Resolve farmer reference only once
            var farmer = who ?? Game1.GetPlayer(who.UniqueMultiplayerID);
            if (farmer == null)
                return;

            if (xLocation == 0 || yLocation == 0)
            {
                // Get tile location once
                xLocation = (int)farmer.Tile.X;
                yLocation = (int)farmer.Tile.Y;
            }



            // Give the player EXP
            AddEXP(farmer, EXP);

            // Apply Gold Rusher buff if needed
            if (farmer.HasCustomProfession(Archaeology_Skill.Archaeology10b2))
                ApplySpeedBoost(farmer);

            // No bonus loot for panning
            if (panning)
                return;

            // Precompute chance once
            double doubleLootChance = GetLevel(farmer) * 0.05;
            double roll = Game1.random.NextDouble();

            if (bonusLoot || roll < doubleLootChance)
            {
                string objectID = "390";

                // exact item chosen externally → no need to create tables
                if (!string.IsNullOrEmpty(exactItem))
                {
                    objectID = exactItem;
                }
                else
                {
                    IList<string> baseTable = ModEntry.BonusLootTable;
                    IList<string> giTable = ModEntry.BonusLootTable_GI;

                    int baseCount = baseTable.Count;
                    int giCount = (farmer.mailReceived.Contains("willyBoatFixed")) ? giTable.Count : 0;
                    int totalCount = baseCount + giCount;

                    if (totalCount > 0) // Only pick randomly if there’s something in the tables
                    {
                        // Pick a random index in [0, totalCount)
                        int index = Game1.random.Next(totalCount);
                        objectID = index < baseCount ? baseTable[index] : giTable[index - baseCount];
                    }
                    // else objectID stays as "390" backup
                }

                Game1.createMultipleObjectDebris(objectID, xLocation, yLocation, 1, farmer.UniqueMultiplayerID);
            }

            // 2% chance for skill book
            if (Game1.random.NextDouble() < 0.02)
                Game1.createMultipleObjectDebris("moonslime.Archaeology.skill_book", xLocation, yLocation, 1, farmer.UniqueMultiplayerID);
        }

        //For the goldrush profession
        public static bool ApplySpeedBoost(Farmer who)
        {
            //Get the player
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            //check to see the player who is doing the request is the same one as this player. 
            if (player != Game1.player)
                return false;

            Buff buff = new(
                id: "Archaeology:profession:haste",
                displayName: ModEntry.Instance.I18N.Get("Archaeology10b2.buff"), // can optionally specify description text too
                iconTexture: Assets.GoldRushBuff,
                iconSheetIndex: 0,
                duration: 6_000 * GetLevel(player), // 60 seconds by default. Can go higher with buffs.
                effects: new BuffEffects()
                {
                    Speed = { 3 } // shortcut for buff.Speed.Value = 5
                }
            );
            //Check to see if the player already has the haste buff. if so, don't refresh it and return false.
            if (player.hasBuff(buff.id))
                return false;


            //Apply the buff make sure we have it have a custon name.
            player.applyBuff(buff);

            //get the player's tile positon as a vector2
            Vector2 tile = new(
                x: (int)(player.position.X / Game1.tileSize),
                y: (int)(player.position.Y / Game1.tileSize)
            );

            //Play a sound to give feedback that this profession is working
            player.currentLocation.localSound("debuffHit", tile);
            return true;
        }

        public static void AddEXP(Farmer who, int amount)
        {
            who.AddCustomSkillExperience(ModEntry.SkillID, amount);
        }

        public static int GetLevel(Farmer who)
        {
            var player = Game1.GetPlayer(who.UniqueMultiplayerID);
            return Skills.GetSkillLevel(player, ModEntry.SkillID) + Skills.GetSkillBuffLevel(player, ModEntry.SkillID);
        }

    }
}
