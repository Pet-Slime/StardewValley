using System;
using System.Linq;
using StardewValley;
using StardewValley.Buffs;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "MagnetSpell" that gives the player a magnetic effect to attract items and slightly increase defense
    public class MagnetSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public MagnetSpell()
            : base(SchoolId.Life, "magnetic_force")
        {
            // SchoolId.Nature indicates this spell belongs to the Nature school
            // "magnetic_force" is the internal name of this spell
        }

        public override bool CanCast(Farmer player, int level)
        {
            // Can cast if the player doesn't already have this spell's buff active
            return base.CanCast(player, level) && !player.hasBuff($"spell:nature:magnetic_force:{level}");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost scales with level
            return 5 * (level + 1);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            // If the buff is already active, cancel the spell
            if (player.hasBuff($"spell:nature:magnetic_force:{level}"))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            // Apply the magnetic buff to the player
            player.buffs.Apply(new Buff(
                id: $"spell:nature:magnetic_force:{level}",  // Unique ID for this spell
                source: $"spell:nature:magnetic_force:{level}",  // Source name for display
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.magnetic_force.buffDescription") + level.ToString(),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,  // Duration scales with level
                effects: new BuffEffects
                {
                    MagneticRadius = { (level + 1) * 16 }, // Radius for attracting nearby items
                    Defense = { level + 1 }                // Slightly increase defense
                }
            ));

            // Make the player do an exclamation emote when casting
            player.performPlayerEmote("exclamation");

            // Return a successful spell effect with a sound and grant exp
            return new SpellSuccess(player, "powerup", 5);
        }
    }
}
