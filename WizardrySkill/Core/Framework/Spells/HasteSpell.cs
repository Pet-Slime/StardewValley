using System;
using System.Linq;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    public class HasteSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public HasteSpell()
            : base(SchoolId.Life, "haste") { }

        public override bool CanCast(Farmer player, int level)
        {
            if (player == Game1.player)
            {
                return !player.buffs.AppliedBuffs.Values.Any(u => u.source == "spell:life:haste");
            }

            return base.CanCast(player, level) && player.Items.ContainsId("(O)433", 1);
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 50;
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            if (player.buffs.AppliedBuffs.Values.Any(u => u.source == "spell:life:haste"))
                return null;

            player.buffs.Apply(new Buff(
                id: "spacechase0.magic.haste",
                source: "spell:life:haste",
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.haste.buffDescription"),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new StardewValley.Buffs.BuffEffects
                {
                    Speed = { level + 1 }
                }
            ));

            player.Items.ReduceId("(O)433", 1);
            player.performPlayerEmote("exclamation");
            player.currentLocation.playSound("powerup", player.Tile);
            Utilities.AddEXP(player, 5);
            return null;
        }
    }
}
