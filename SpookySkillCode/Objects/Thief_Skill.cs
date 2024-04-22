using System.Collections.Generic;
using MoonShared;
using StardewModdingAPI;

namespace SpookySkill
{
    public class Thief_Skill : SpaceCore.Skills.Skill
    {
        public static KeyedProfession Thief5a;
        public static KeyedProfession Thief5b;
        public static KeyedProfession Thief10a1;
        public static KeyedProfession Thief10a2;
        public static KeyedProfession Thief10b1;
        public static KeyedProfession Thief10b2;

        public Thief_Skill() : base("moonslime.Spooky")
        {
            this.Icon = ModEntry.Assets.IconA_Thief;

            switch (ModEntry.Config.ThiefIcon)
            {
                case 1:
                    this.SkillsPageIcon = ModEntry.Assets.IconB_Thief_1;
                    break;
                case 2:
                    this.SkillsPageIcon = ModEntry.Assets.IconB_Thief_2;
                    break;
                case 3:
                    this.SkillsPageIcon = ModEntry.Assets.IconB_Thief_3;
                    break;
                default:
                    this.SkillsPageIcon = ModEntry.Assets.IconB_Thief_1;
                    break;
            }

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Thief5a = new KeyedProfession(this,     "Thief5a",     ModEntry.Assets.Spooky5a_Thief, ModEntry.Instance.I18N),
                Thief5b = new KeyedProfession(this,     "Thief5b",     ModEntry.Assets.Spooky5b_Thief, ModEntry.Instance.I18N),
                Thief10a1 = new KeyedProfession(this,   "Thief10a1",    ModEntry.Assets.Spooky10a1_Thief, ModEntry.Instance.I18N),
                Thief10a2 = new KeyedProfession(this,   "Thief10a2",    ModEntry.Assets.Spooky10a2_Thief, ModEntry.Instance.I18N),
                Thief10b1 = new KeyedProfession(this,   "Thief10b1",    ModEntry.Assets.Spooky10b1_Thief, ModEntry.Instance.I18N),
                Thief10b2 = new KeyedProfession(this,   "Thief10b2",    ModEntry.Assets.Spooky10b2_Thief, ModEntry.Instance.I18N)
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
            return ModEntry.Instance.I18N.Get("skill.Thief.name");
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> result = new()
            {
                ModEntry.Instance.I18N.Get("skill.Thief.perk", new { bonus = 2 })
            };
            if (level == 3)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Thief.perk.level_3"));
            }
            if (level == 7)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Thief.perk.level_6"));
            }
            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return ModEntry.Instance.I18N.Get("skill.Thief.perk", new { bonus = 2 * level });
        }



    }
}
