using System;
using System.Linq;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

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
            return base.CanCast(player, level) && player.Items.ContainsId("(O)433", 1) && !player.hasBuff($"spell:life:haste:{level}");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 40 * (level + 1);
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;

            if (player.hasBuff($"spell:life:haste:{level}"))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            player.buffs.Apply(new Buff(
                id: $"spell:life:haste:{level}",
                source: $"spell:life:haste:{level}",
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.haste.buffDescription") + level.ToString(),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new StardewValley.Buffs.BuffEffects
                {
                    Speed = { level + 1 }
                }
            ));

            player.Items.ReduceId("(O)433", 1);
            player.performPlayerEmote("exclamation");
            player.currentLocation.playSound("powerup", player.Tile);

            return new SpellSuccess(player, "powerup", 5);
        }
    }
}
