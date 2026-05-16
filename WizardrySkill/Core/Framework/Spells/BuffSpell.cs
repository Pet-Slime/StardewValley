using System;
using SpaceCore;
using StardewValley;
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

        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            return null;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && !player.hasBuff($"spell:life:buff:{level}");
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 25 * (level + 1);
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (!player.IsLocalPlayer)
                return null;

            if (player.hasBuff($"spell:life:buff:{level}"))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            int l = level + 1;

            var baseSkillBuff = new Buff(
                id: $"spell:life:buff:{level}",
                source: $"spell:life:buff:{level}",
                displaySource: ModEntry.Instance.I18N.Get("moonslime.Wizardry.buff.buffDescription") + level.ToString(),
                duration: (int)TimeSpan.FromSeconds(60 + level * 120).TotalMilliseconds,
                effects: new StardewValley.Buffs.BuffEffects
                {
                    FarmingLevel = { l },
                    FishingLevel = { l },
                    MiningLevel = { l },
                    CombatLevel = { l },
                    ForagingLevel = { l }
                }
            );

            foreach (string customSkill in Skills.GetSkillList())
                baseSkillBuff.customFields.Add($"spacechase.SpaceCore.SkillBuff.{customSkill}", $"{l}");

            player.buffs.Apply(baseSkillBuff);

            return new SpellSuccess(player, "powerup", 10);
        }
    }
}
