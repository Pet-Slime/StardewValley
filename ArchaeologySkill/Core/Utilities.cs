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
        public static void ApplyArchaeologySkill(Farmer who, int EXP, bool bonusLoot = false, int xLocation = 0, int yLocation = 0, bool panning = false, string exactItem = "")
        {
            var farmer = Game1.GetPlayer(who.UniqueMultiplayerID);
            if (farmer == null) { return; };

            xLocation = farmer.TilePoint.X;
            yLocation = farmer.TilePoint.Y;
            //Give the player EXP
            Log.Trace("Archaeology Skll: Adding EXP to the player");

            AddEXP(farmer, EXP);
            //If the player has the gold rush profession, give them a speed buff
            Log.Trace("Archaeology Skll: Does the player have gold rusher?");
            if (farmer.HasCustomProfession(Archaeology_Skill.Archaeology10b2))
            {
                Log.Trace("Archaeology Skll: The player does have gold rusher!");
                ApplySpeedBoost(farmer);
            }
            else
            {
                Log.Trace("Archaeology Skll: the player does not have gold rusher");
            }

            //Check to see if the player wins the double loot chance roll if they are not panning.
            if (!panning)
            {
                Log.Trace("Archaeology Skll: Does the player get bonus loot?");
                double doubleLootChance = GetLevel(farmer) * 0.05;
                double diceRoll = Game1.random.NextDouble();
                bool didTheyWin = diceRoll < doubleLootChance;
                Log.Trace("Archaeology Skll: The dice roll is... " + diceRoll.ToString() + ". The player's chance is... " + doubleLootChance.ToString() + ". ");
                if (didTheyWin || bonusLoot)
                {
                    Log.Trace("Archaeology Skill: They do get bonus loot!");
                    string objectID;

                    if (!string.IsNullOrEmpty(exactItem))
                    {
                        objectID = exactItem;
                    }
                    else
                    {
                        List<string> newBonusLootTable = new List<string>(ModEntry.BonusLootTable);

                        if (farmer.mailReceived.Contains("willyBoatFixed"))
                            newBonusLootTable.AddRange(ModEntry.BonusLootTable_GI);

                        newBonusLootTable.Shuffle(Game1.random);

                        objectID = newBonusLootTable.RandomChoose(Game1.random, "390");
                    }
                    Game1.createMultipleObjectDebris(objectID, xLocation, yLocation, 1, farmer.UniqueMultiplayerID);
                }
                else
                {
                    Log.Trace("Archaeology Skill: They do not get bonus loot!");
                }
                if (Game1.random.NextDouble() < 0.02)
                {
                    Game1.createMultipleObjectDebris("moonslime.Archaeology.skill_book", xLocation, yLocation, 1, farmer.UniqueMultiplayerID);
                }
            }
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
