using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizardrySkill.Core;
using StardewValley;
using StardewValley.Buffs;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;

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
            if (player == Game1.player)
            {
                return !player.buffs.AppliedBuffs.Values.Any(u => u.source == "spell:nature:magnetic_force");
            }

            return base.CanCast(player, level);
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 5;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            if (player.buffs.AppliedBuffs.Values.Any(u => u.source == "spell:nature:magnetic_force"))
                return null;

            player.buffs.Apply(new Buff(
                id: "spacechase0.magic.haste",
                source: "spell:nature:magnetic_force",
                displaySource: "Haste (spell)",
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new BuffEffects
                {
                    MagneticRadius = { (level + 1) * 16 },
                    Defense = { level + 1 }
                }
            ));
            player.performPlayerEmote("exclamation");
            player.currentLocation.playSound("powerup", player.Tile);
            Utilities.AddEXP(player, 5);
            return null;
        }
    }
}
