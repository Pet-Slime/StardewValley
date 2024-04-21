using System.Collections.Generic;
using MoonShared;
using StardewModdingAPI;

namespace SpookySkill
{
    public class Spooky_Skill : SpaceCore.Skills.Skill
    {
        public static KeyedProfession Spooky5a;
        public static KeyedProfession Spooky5b;
        public static KeyedProfession Spooky10a1;
        public static KeyedProfession Spooky10a2;
        public static KeyedProfession Spooky10b1;
        public static KeyedProfession Spooky10b2;
        public readonly IModHelper _modHelper;

        public Spooky_Skill() : base("moonslime.Spooky")
        {
            this.Icon = ModEntry.Assets.IconA;
            this.SkillsPageIcon = ModEntry.Assets.IconB;

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Spooky5a = new KeyedProfession(this, "Spookyk5a", ModEntry.Assets.Spooky5a, ModEntry.Instance.I18N),
                Spooky5b = new KeyedProfession(this, "Spookyk5b", ModEntry.Assets.Spooky5b, ModEntry.Instance.I18N),
                Spooky10a1 = new KeyedProfession(this, "Spooky10a1", ModEntry.Assets.Spooky10a1, ModEntry.Instance.I18N),
                Spooky10a2 = new KeyedProfession(this, "Spooky10a2", ModEntry.Assets.Spooky10a2, ModEntry.Instance.I18N),
                Spooky10b1 = new KeyedProfession(this, "Spooky10b1", ModEntry.Assets.Spooky10b1, ModEntry.Instance.I18N),
                Spooky10b2 = new KeyedProfession(this, "Spooky10b2", ModEntry.Assets.Spooky10b2, ModEntry.Instance.I18N)
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
            return ModEntry.Instance.I18N.Get("skill.name");
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> result = new()
            {
                ModEntry.Instance.I18N.Get("skill.perk", new { bonus = 5 })
            };
            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return ModEntry.Instance.I18N.Get("skill.perk", new { bonus = 5 * level });
        }



    }
}
