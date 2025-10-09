using System.Collections.Generic;
using AthleticSkill.Objects;
using MoonShared;
using StardewModdingAPI;

namespace AthleticSkill
{
    public class Athletic_Skill : SpaceCore.Skills.Skill
    {
        public static KeyedProfession Athletic5a;
        public static KeyedProfession Athletic5b;
        public static StrongProfession Athletic10a1;
        public static KeyedProfession Athletic10a2;
        public static KeyedProfession Athletic10b1;
        public static KeyedProfession Athletic10b2;
        public readonly IModHelper _modHelper;

        public Athletic_Skill() : base("moonslime.Athletic")
        {
            this.Icon = ModEntry.Assets.IconA;
            this.SkillsPageIcon = ModEntry.Assets.IconB;

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Athletic5a = new KeyedProfession(this, "Athletic5a", ModEntry.Assets.Athletic5a, ModEntry.Instance.I18N),
                Athletic5b = new KeyedProfession(this, "Athletic5b", ModEntry.Assets.Athletic5b, ModEntry.Instance.I18N),
                Athletic10a1 = new StrongProfession(this, "Athletic10a1", ModEntry.Assets.Athletic10a1, ModEntry.Instance.I18N),
                Athletic10a2 = new KeyedProfession(this, "Athletic10a2", ModEntry.Assets.Athletic10a2, ModEntry.Instance.I18N),
                Athletic10b1 = new KeyedProfession(this, "Athletic10b1", ModEntry.Assets.Athletic10b1, ModEntry.Instance.I18N),
                Athletic10b2 = new KeyedProfession(this, "Athletic10b2", ModEntry.Assets.Athletic10b2, ModEntry.Instance.I18N)
            );
        }

        private void AddProfessions(KeyedProfession lvl5A, KeyedProfession lvl5B, StrongProfession lvl10A1, KeyedProfession lvl10A2, KeyedProfession lvl10B1, KeyedProfession lvl10B2)
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
            List<string> result = new()
            {
                ModEntry.Instance.I18N.Get("skill.perk", new { bonus = 1 })
            };
            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return ModEntry.Instance.I18N.Get("skill.perk", new { bonus = 1 * level });
        }



    }
}
