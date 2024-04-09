using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using MoonShared;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;

namespace ArchaeologySkill
{
    internal class Archaeology_Skill : SpaceCore.Skills.Skill
    {
        public static KeyedProfession Archaeology5a;
        public static KeyedProfession Archaeology5b;
        public static KeyedProfession Archaeology10a1;
        public static KeyedProfession Archaeology10a2;
        public static KeyedProfession Archaeology10b1;
        public static KeyedProfession Archaeology10b2;
        public static readonly IList<string> StartingRecipes = new List<string>();
        public static readonly IDictionary<int, IList<string>> ArchaeologySkillLevelUpRecipes = new Dictionary<int, IList<string>>();

        public Archaeology_Skill() : base("moonslime.Archaeology")
        {
            this.Icon = ModEntry.Assets.IconA;
            this.SkillsPageIcon = ModEntry.Config.AlternativeSkillPageIcon ? ModEntry.Assets.IconBalt : ModEntry.Assets.IconB;
            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Archaeology5a = new KeyedProfession(this, "Archaeology5a", ModEntry.Assets.Archaeology5a, ModEntry.Instance.I18n),
                Archaeology5b = new KeyedProfession(this, "Archaeology5b", ModEntry.Assets.Archaeology5b, ModEntry.Instance.I18n),
                Archaeology10a1 = new KeyedProfession(this, "Archaeology10a1", ModEntry.Assets.Archaeology10a1, ModEntry.Instance.I18n),
                Archaeology10a2 = new KeyedProfession(this, "Archaeology10a2", ModEntry.Assets.Archaeology10a2, ModEntry.Instance.I18n),
                Archaeology10b1 = new KeyedProfession(this, "Archaeology10b1", ModEntry.Assets.Archaeology10b1, ModEntry.Instance.I18n),
                Archaeology10b2 = new KeyedProfession(this, "Archaeology10b2", ModEntry.Assets.Archaeology10b2, ModEntry.Instance.I18n)
            );

            // Read Archaeology skill level up recipes from data file
            foreach (KeyValuePair<string, List<string>> pair in ModEntry.ArchaeologySkillLevelUpTable)
            {
                ArchaeologySkillLevelUpRecipes.Add(int.Parse(pair.Key), pair.Value);
            }

            // Read starting recipes from general data file
            foreach (string entry in ModEntry.ItemDefinitions["StartingRecipes"])
            {
                StartingRecipes.Add(entry);
            }
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
            return ModEntry.Instance.I18n.Get("skill.name");
        }

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            List<string> result = new()
            {
                ModEntry.Instance.I18n.Get("skill.perk", new { bonus = 0.05 })
            };            
            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            double value = level * 0.05;
            double truncated = ((int)(value * 100)) / 100.00;
            return ModEntry.Instance.I18n.Get("skill.perk", new { bonus = truncated });
        }
    }
}
