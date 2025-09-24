using System.Collections.Generic;
using MoonShared;
using StardewModdingAPI;

namespace BibliocraftSkill
{
    public class Book_Skill : SpaceCore.Skills.Skill
    {
        public static KeyedProfession Book5a;
        public static KeyedProfession Book5b;
        public static KeyedProfession Book10a1;
        public static KeyedProfession Book10a2;
        public static KeyedProfession Book10b1;
        public static KeyedProfession Book10b2;
        public readonly IModHelper _modHelper;

        public Book_Skill() : base("moonslime.Bibliocraft")
        {
            this.Icon = ModEntry.Assets.IconA;
            this.SkillsPageIcon = ModEntry.Assets.IconB;

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Book5a = new KeyedProfession(this, "Book5a", ModEntry.Assets.Book5a, ModEntry.Instance.I18N),
                Book5b = new KeyedProfession(this, "Book5b", ModEntry.Assets.Book5b, ModEntry.Instance.I18N),
                Book10a1 = new KeyedProfession(this, "Book10a1", ModEntry.Assets.Book10a1, ModEntry.Instance.I18N),
                Book10a2 = new KeyedProfession(this, "Book10a2", ModEntry.Assets.Book10a2, ModEntry.Instance.I18N),
                Book10b1 = new KeyedProfession(this, "Book10b1", ModEntry.Assets.Book10b1, ModEntry.Instance.I18N),
                Book10b2 = new KeyedProfession(this, "Book10b2", ModEntry.Assets.Book10b2, ModEntry.Instance.I18N)
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
            if (level == 2)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_2"));
            }
            if (level == 4)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_4a"));
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_4b"));
            }
            if (level == 7)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_7a"));
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_7b"));
            }
            if (level == 8)
            {
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_8a"));
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_8b"));
            }
            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            return ModEntry.Instance.I18N.Get("skill.perk", new { bonus = 5 * level });
        }
    }
}
