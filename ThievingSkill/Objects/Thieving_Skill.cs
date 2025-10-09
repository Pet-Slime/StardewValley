using System.Collections.Generic;
using MoonShared;
using StardewModdingAPI;
using StardewValley;

namespace ThievingSkill
{
    public class Thieving_Skill : SpaceCore.Skills.Skill
    {
        public static KeyedProfession Thieving5a;
        public static KeyedProfession Thieving5b;
        public static KeyedProfession Thieving10a1;
        public static KeyedProfession Thieving10a2;
        public static KeyedProfession Thieving10b1;
        public static KeyedProfession Thieving10b2;

        public Thieving_Skill() : base("moonslime.Thieving")
        {
            this.Icon = ModEntry.Assets.IconA;
            switch (ModEntry.Config.ThiefIcon)
            {
                case 1:
                    this.SkillsPageIcon = ModEntry.Assets.IconB1;
                    break;
                case 2:
                    this.SkillsPageIcon = ModEntry.Assets.IconB2;
                    break;
                case 3:
                    this.SkillsPageIcon = ModEntry.Assets.IconB3;
                    break;
                default:
                    this.SkillsPageIcon = ModEntry.Assets.IconB1;
                    break;
            }

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Thieving5a = new KeyedProfession(this, "Thieving5a", ModEntry.Assets.Thieving5a, ModEntry.Instance.I18N),
                Thieving5b = new KeyedProfession(this, "Thieving5b", ModEntry.Assets.Thieving5b, ModEntry.Instance.I18N),
                Thieving10a1 = new KeyedProfession(this, "Thieving10a1", ModEntry.Assets.Thieving10a1, ModEntry.Instance.I18N),
                Thieving10a2 = new KeyedProfession(this, "Thieving10a2", ModEntry.Assets.Thieving10a2, ModEntry.Instance.I18N),
                Thieving10b1 = new KeyedProfession(this, "Thieving10b1", ModEntry.Assets.Thieving10b1, ModEntry.Instance.I18N),
                Thieving10b2 = new KeyedProfession(this, "Thieving10b2", ModEntry.Assets.Thieving10b2, ModEntry.Instance.I18N)
            );


        }

        private void AddProfessions(KeyedProfession lvl5A, KeyedProfession lvl5B, KeyedProfession lvl10A1, KeyedProfession lvl10A2, KeyedProfession lvl10B1, KeyedProfession lvl10B2)
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
            return ModEntry.Instance.I18N.Get("skill.Thieving.name");
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> result = new()
                {
                    ModEntry.Instance.I18N.Get("skill.Thieving.perk", new { bonus = 2 })
                };
            if (level == 3)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Thieving.perk.level_3"));
            }
            if (level == 7)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Thieving.perk.level_6"));
            }
            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return ModEntry.Instance.I18N.Get("skill.Thieving.perk", new { bonus = 2 * level });
        }
    }
}
