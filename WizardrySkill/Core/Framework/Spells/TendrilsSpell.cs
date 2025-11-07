using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // TendrilsSpell creates magical tendrils at a target location to trap monsters
    // TODO: Might be refactored into a trap mechanic later
    public class TendrilsSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: assigns spell school and ID
        public TendrilsSpell()
            : base(SchoolId.Nature, "tendrils") { }

        // Mana required to cast
        public override int GetManaCost(Farmer player, int level)
        {
            return 10;
        }

        // Maximum spell level
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // What happens when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Create a new collection to hold the tendrils we generate
            TendrilGroup tendrils = new TendrilGroup();

            // Loop through all characters in the current location
            foreach (var npc in player.currentLocation.characters)
            {
                // Only target monsters
                if (npc is Monster mob)
                {
                    float rad = Game1.tileSize; // radius of effect (1 tile)
                    int dur = 11 * 60;          // duration of the tendril in game ticks

                    // If the monster is within range of the target location
                    if (Vector2.Distance(mob.position.Value, new Vector2(targetX, targetY)) <= rad)
                    {
                        // Add a new tendril that affects this monster
                        tendrils.Add(new Tendril(mob, new Vector2(targetX, targetY), rad, dur));

                        // Give some XP to the player for hitting a monster
                        Utilities.AddEXP(player, 3);
                    }
                }
            }

            // If any tendrils were created, return them as the active effect
            // Otherwise, the spell fizzles
            return tendrils.Any()
                ? tendrils
                : new SpellFizzle(player, this.GetManaCost(player, level));
        }
    }
}
