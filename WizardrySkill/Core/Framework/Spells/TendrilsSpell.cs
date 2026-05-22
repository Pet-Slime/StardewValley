using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using xTile.Tiles;

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

        public override SpellSyncMode SyncMode => SpellSyncMode.NetworkedEffect;

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

        // What happens when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {

            player.currentLocation.playSound("grassyStep", new Vector2(targetX,targetY) / Game1.tileSize);
            return this.CreateTendrils(player, level, targetX, targetY, giveExperience: true, showFizzle: true, allowMonsterPositionMutation: Context.IsMainPlayer);
        }

        // What happens when another machine observes this spell cast.
        public override IActiveEffect OnRemoteCast(Farmer caster, int level, int targetX, int targetY, IDictionary<string, string> data)
        {
            return this.CreateTendrils(caster, level, targetX, targetY, giveExperience: false, showFizzle: false, allowMonsterPositionMutation: Context.IsMainPlayer);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Create tendril effects at a target position.</summary>
        /// <param name="caster">The player who cast the spell.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="targetX">The target X position in pixels.</param>
        /// <param name="targetY">The target Y position in pixels.</param>
        /// <param name="giveExperience">Whether this machine should award caster EXP for affected monsters.</param>
        /// <param name="showFizzle">Whether this machine should show a fizzle if no monsters were found.</param>
        /// <param name="allowMonsterPositionMutation">Whether this machine may constrain monster position.</param>
        private IActiveEffect CreateTendrils(Farmer caster, int level, int targetX, int targetY, bool giveExperience, bool showFizzle, bool allowMonsterPositionMutation)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            GameLocation location = caster.currentLocation;
            Vector2 targetPosition = new(targetX, targetY);

            // Create a new collection to hold the tendrils we generate.
            TendrilGroup tendrils = new();

            // Loop through all characters in the caster's current location.
            foreach (var npc in location.characters)
            {
                // Only target monsters.
                if (npc is not Monster mob)
                    continue;

                float rad = Game1.tileSize; // radius of effect (1 tile)
                int dur = 11 * 60;          // duration of the tendril in game ticks

                // If the monster is within range of the target location.
                if (Vector2.Distance(mob.position.Value, targetPosition) <= rad)
                {
                    // Add a new tendril that affects this monster.
                    tendrils.Add(new Tendril(location, mob, targetPosition, rad, dur, allowMonsterPositionMutation));

                    // Give some XP to the player for hitting a monster.
                    if (giveExperience && caster.IsLocalPlayer)
                        Utilities.AddEXP(caster, 3);
                }
            }

            // If any tendrils were created, return them as the active effect.
            // Otherwise, the spell fizzles only on the actual caster's machine.
            return tendrils.Any()
                ? tendrils
                : showFizzle && caster.IsLocalPlayer ? new SpellFizzle(caster, this.GetManaCost(caster, level)) : null;
        }
    }
}
