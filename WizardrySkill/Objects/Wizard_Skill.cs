using System.Collections.Generic;
using MoonShared;
using StardewModdingAPI;
using StardewValley;
using WizardrySkill.Core;
using WizardrySkill.Core.Framework;

namespace WizardrySkill.Objects
{
    public class Wizard_Skill : SpaceCore.Skills.Skill
    {
        public static UpgradePointProfession Magic5a;
        public static MagicProfession Magic5b;
        public static UpgradePointProfession Magic10a1;
        public static MagicProfession Magic10a2;
        public static MagicProfession Magic10b1;
        public static ManaCapProfession Magic10b2;
        public readonly IModHelper _modHelper;

        public Wizard_Skill() : base("moonslime.Wizard")
        {
            this.Icon = ModEntry.Assets.IconA;
            this.SkillsPageIcon = ModEntry.Assets.IconB;

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                //Potential
                Magic5a = new UpgradePointProfession(this, "Magic5a", ModEntry.Assets.Magic5a, ModEntry.Instance.I18N),
                //Mana Regen I
                Magic5b = new MagicProfession(this, "Magic5b", ModEntry.Assets.Magic5b, ModEntry.Instance.I18N),
                //Prodigy
                Magic10a1 = new UpgradePointProfession(this, "Magic10a1", ModEntry.Assets.Magic10a1, ModEntry.Instance.I18N),
                //Memory
                Magic10a2 = new MagicProfession(this, "Magic10a2", ModEntry.Assets.Magic10a2, ModEntry.Instance.I18N),
                //Mana Regen II
                Magic10b1 = new MagicProfession(this, "Magic10b1", ModEntry.Assets.Magic10b1, ModEntry.Instance.I18N),
                //Mana Reserve
                Magic10b2 = new ManaCapProfession(this, "Magic10b2", ModEntry.Assets.Magic10b2, ModEntry.Instance.I18N)
            );
        }

        private void AddProfessions(UpgradePointProfession lvl5A, MagicProfession lvl5B, UpgradePointProfession lvl10A1, MagicProfession lvl10A2, MagicProfession lvl10B1, ManaCapProfession lvl10B2)
        {
            this.Professions.Add(lvl5A);
            this.Professions.Add(lvl5B);
            this.ProfessionsForLevels.Add(new ProfessionPair(5, lvl5A, lvl5B));

            this.Professions.Add(lvl10A1);
            this.Professions.Add(lvl10A2);
            this.ProfessionsForLevels.Add(new ProfessionPair(10, lvl10A1, lvl10A2, lvl5A));

            this.Professions.Add(lvl10B1);
            this.Professions.Add(lvl10B2);
            this.ProfessionsForLevels.Add(new ProfessionPair(10, lvl10B1, lvl10B2, lvl5B));
        }

        public override string GetName()
        {
            return ModEntry.Instance.I18N.Get("skill.name");
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> result = new();
            if (level == 1)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.perk0.1"));
                result.Add(ModEntry.Instance.I18N.Get("skill.perk1", new { bonus = 5 }));
                result.Add(ModEntry.Instance.I18N.Get("skill.perk2", new { bonus = 1 }));
                result.Add(ModEntry.Instance.I18N.Get("skill.perk0.2"));
                result.Add(ModEntry.Instance.I18N.Get("skill.perk0.3"));
                result.Add(ModEntry.Instance.I18N.Get("skill.perk0.4"));
            }
            else
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.perk1", new { bonus = 5 }));
                result.Add(ModEntry.Instance.I18N.Get("skill.perk2", new { bonus = 1 }));
            }

            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return ModEntry.Instance.I18N.Get("skill.perk_bonus1", new { bonus = 5 * level }) + "\n" + ModEntry.Instance.I18N.Get("skill.perk_bonus2", new { bonus = 1 * level });
        }

        public override void DoLevelPerk(int level)
        {
            // fix magic info if invalid
            Events.FixMagicIfNeeded(Game1.player, overrideMagicLevel: level - 1);

            // add level perk
            int curMana = Game1.player.GetMaxMana();
            if (level > 1 || curMana < MagicConstants.ManaPointsPerLevel) // skip increasing mana for first level, since we did it on learning the skill
                Game1.player.SetMaxMana(curMana + MagicConstants.ManaPointsPerLevel);

            Game1.player.GetSpellBook().UseSpellPoints(-1);
        }
    }
}
