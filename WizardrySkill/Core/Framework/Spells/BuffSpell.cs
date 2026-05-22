using System;
using SpaceCore;
using StardewValley;
using StardewValley.Buffs;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "BuffSpell" that temporarily increases multiple player skills.
    public class BuffSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public BuffSpell()
            : base(SchoolId.Life, "buff")
        {
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && !player.hasBuff(this.GetBuffId(level));
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25 * (level + 1);
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null)
                return null;

            // Only the caster's own machine should apply personal buffs.
            if (!player.IsLocalPlayer)
                return null;

            string buffId = this.GetBuffId(level);
            if (player.hasBuff(buffId))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            int buffLevel = level + 1;

            Buff baseSkillBuff = new(
                id: buffId,
                source: buffId,
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.buff.buffDescription") + level.ToString(),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new BuffEffects
                {
                    FarmingLevel = { buffLevel },
                    FishingLevel = { buffLevel },
                    MiningLevel = { buffLevel },
                    CombatLevel = { buffLevel },
                    ForagingLevel = { buffLevel }
                }
            );

            foreach (string customSkill in Skills.GetSkillList())
                baseSkillBuff.customFields.Add($"spacechase.SpaceCore.SkillBuff.{customSkill}", $"{buffLevel}");

            player.buffs.Apply(baseSkillBuff);

            return new SpellSuccess(player, "powerup", 10);
        }


        /*********
        ** Private methods
        *********/
        private string GetBuffId(int level)
        {
            return $"spell:{this.FullId}:{level}";
        }
    }
}
