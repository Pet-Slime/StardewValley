using System.Collections.Generic;
using BirbCore.Attributes;
using Microsoft.Xna.Framework;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using static BirbCore.Attributes.SMod;

namespace WizardrySkill.Core.Framework.Spells
{
    public class CharmSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public CharmSpell()
            : base(SchoolId.Eldritch, "charm") { }

        public override int GetManaCost(Farmer player, int level)
        {
            return 15;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            var mobs = player.currentLocation.characters;
            int levelAmount = level * 2 + 2;

            GameLocation location = player.currentLocation;
            Vector2 playerTile = player.Tile;
            List<NPC> npcsInRange = [];
            List<NPC> monstersInRange = [];


            foreach (var NPC in location.characters)
            {

                float Distance = Vector2.Distance(NPC.Tile, playerTile);
                float profession = levelAmount;


                Log.Trace("Wizardry Charm Spell: going to go through the list...");
                Log.Trace("NPC name is: " + NPC.Name);
                Log.Trace("is NPC villager?: " + NPC.IsVillager.ToString());
                Log.Trace("NPC Distance: " + Distance.ToString());
                Log.Trace("distance value: " + profession.ToString());
                Log.Trace("distance check: " + (Distance <= profession).ToString());

                //Check to see if the config is set to only scare monsters
                if (//Check to see if they are a villager
                    NPC.IsVillager &&
                    //Check to see if they are in range of the player
                    Distance <= profession &&
                    //Check to see if they are giftable
                    NPC.CanReceiveGifts() &&
                    //Make sure the player has friendship data with them
                    player.friendshipData.ContainsKey(NPC.Name))
                {
                    npcsInRange.Add(NPC);
                    continue;
                }
            }

            foreach (var NPC in npcsInRange)
            {
                // skip if out of mana
                if (!this.CanCast(player, level))
                    return null;

                if (player.health <= 24)
                    return null;

                player.AddMana(-15);
                player.changeFriendship(20 * (level + 1), NPC);
                player.takeDamage(25, false, null);
                NPC.currentLocation.playSound("jingle1", NPC.Tile);
                Utilities.AddEXP(player, 25);
                NPC.doEmote(Character.blushEmote);

            }

            return null;
        }
    }
}
