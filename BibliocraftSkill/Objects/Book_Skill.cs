using System.Collections.Generic;
using System.Linq;
using BibliocraftSkill.Core;
using MoonShared;
using StardewModdingAPI;

namespace BibliocraftSkill.Objects
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

        public Book_Skill() : base(ModEntry.SkillID)
        {
            this.Icon = Assets.IconA;
            this.SkillsPageIcon = Assets.IconB;

            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Book5a = new KeyedProfession(this, "Book5a", Assets.Book5a, ModEntry.Instance.I18N),
                Book5b = new KeyedProfession(this, "Book5b", Assets.Book5b, ModEntry.Instance.I18N),
                Book10a1 = new KeyedProfession(this, "Book10a1", Assets.Book10a1, ModEntry.Instance.I18N),
                Book10a2 = new KeyedProfession(this, "Book10a2", Assets.Book10a2, ModEntry.Instance.I18N),
                Book10b1 = new KeyedProfession(this, "Book10b1", Assets.Book10b1, ModEntry.Instance.I18N),
                Book10b2 = new KeyedProfession(this, "Book10b2", Assets.Book10b2, ModEntry.Instance.I18N)
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
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_2a"));
                result.Add(ModEntry.Instance.I18N.Get("skill.Book.perk.level_2b"));
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

        private static readonly Dictionary<int, string> _cachedHoverText = new();

        public override string GetSkillPageHoverText(int level)
        {
            // Check cache first
            if (_cachedHoverText.TryGetValue(level, out string cached))
                return cached;

            // Base perk text
            string text = ModEntry.Instance.I18N.Get("skill.perk", new { bonus = 5 * level });

            // Map of level thresholds to perk translation keys
            var perks = new Dictionary<int, string[]>
                {
                    { 2, new[] { "skill.Book.perk.level_2a", "skill.Book.perk.level_2b" } },
                    { 4, new[] { "skill.Book.perk.level_4a", "skill.Book.perk.level_4b" } },
                    { 7, new[] { "skill.Book.perk.level_7a", "skill.Book.perk.level_7b" } },
                    { 8, new[] { "skill.Book.perk.level_8a", "skill.Book.perk.level_8b" } }
                };

            // Append perks for levels up to the current level
            foreach (var kvp in perks.OrderBy(p => p.Key))
            {
                if (level >= kvp.Key)
                {
                    foreach (string key in kvp.Value)
                    {
                        text += "\n" + ModEntry.Instance.I18N.Get(key);
                    }
                }
            }

            // Cache the result for future calls
            _cachedHoverText[level] = text;

            return text;
        }
    }
}
