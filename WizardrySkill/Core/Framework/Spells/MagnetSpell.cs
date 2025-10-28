using System;
using System.Linq;
using StardewValley;
using StardewValley.Buffs;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class MagnetSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public MagnetSpell()
            : base(SchoolId.Nature, "magnetic_force") { }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && !player.hasBuff($"spell:nature:magnetic_force:{level}");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 5 * (level + 1);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            if (player.hasBuff($"spell:nature:magnetic_force:{level}"))
                return new SpellFizzle(player);

            player.buffs.Apply(new Buff(
                id: $"spell:nature:magnetic_force:{level}",
                source: $"spell:nature:magnetic_force:{level}",
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.magnetic_force.buffDescription") + level.ToString(),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new BuffEffects
                {
                    MagneticRadius = { (level + 1) * 16 },
                    Defense = { level + 1 }
                }
            ));
            player.performPlayerEmote("exclamation");

            return new SpellSuccess(player, "powerup", 5);
        }
    }
}
