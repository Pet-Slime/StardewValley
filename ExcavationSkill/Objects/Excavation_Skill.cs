using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using MoonShared;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;

namespace ExcavationSkill
{
    internal class Excavation_Skill : SpaceCore.Skills.Skill
    {
        public static KeyedProfession Excavation5a;
        public static KeyedProfession Excavation5b;
        public static KeyedProfession Excavation10a1;
        public static KeyedProfession Excavation10a2;
        public static KeyedProfession Excavation10b1;
        public static KeyedProfession Excavation10b2;
        public static readonly IList<string> StartingRecipes = new List<string>();
        public static readonly IDictionary<int, IList<string>> ExcavationSkillLevelUpRecipes = new Dictionary<int, IList<string>>();

        public Excavation_Skill() : base("moonslime.Excavation")
        {
            this.Icon = ModEntry.Assets.IconA;
            this.SkillsPageIcon = ModEntry.Config.AlternativeSkillPageIcon ? ModEntry.Assets.IconBalt : ModEntry.Assets.IconB;
            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);

           if (ModEntry.MargoLoaded && ModEntry.Config.EnablePrestige)
           {
               this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000, 20000, 25000, 30000, 35000, 40000, 45000, 50000, 55000, 60000, 70000 };
               this.AddProfessions(
                   Excavation5a = new KeyedProfession(this, "Excavation5a", ModEntry.Assets.Excavation5a, ModEntry.Assets.Excavation5aP, ModEntry.Instance.Helper),
                   Excavation5b = new KeyedProfession(this, "Excavation5b", ModEntry.Assets.Excavation5b, ModEntry.Assets.Excavation5bP, ModEntry.Instance.Helper),
                   Excavation10a1 = new KeyedProfession(this, "Excavation10a1", ModEntry.Assets.Excavation10a1, ModEntry.Assets.Excavation10a1P, ModEntry.Instance.Helper),
                   Excavation10a2 = new KeyedProfession(this, "Excavation10a2", ModEntry.Assets.Excavation10a2, ModEntry.Assets.Excavation10a2P, ModEntry.Instance.Helper),
                   Excavation10b1 = new KeyedProfession(this, "Excavation10b1", ModEntry.Assets.Excavation10b1, ModEntry.Assets.Excavation10b1P, ModEntry.Instance.Helper),
                   Excavation10b2 = new KeyedProfession(this, "Excavation10b2", ModEntry.Assets.Excavation10b2, ModEntry.Assets.Excavation10b2P, ModEntry.Instance.Helper)
               );
           }
           else
           {
               this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
               this.AddProfessions(
                   Excavation5a = new KeyedProfession(this, "Excavation5a", ModEntry.Assets.Excavation5a, ModEntry.Instance.I18n),
                   Excavation5b = new KeyedProfession(this, "Excavation5b", ModEntry.Assets.Excavation5b, ModEntry.Instance.I18n),
                   Excavation10a1 = new KeyedProfession(this, "Excavation10a1", ModEntry.Assets.Excavation10a1, ModEntry.Instance.I18n),
                   Excavation10a2 = new KeyedProfession(this, "Excavation10a2", ModEntry.Assets.Excavation10a2, ModEntry.Instance.I18n),
                   Excavation10b1 = new KeyedProfession(this, "Excavation10b1", ModEntry.Assets.Excavation10b1, ModEntry.Instance.I18n),
                   Excavation10b2 = new KeyedProfession(this, "Excavation10b2", ModEntry.Assets.Excavation10b2, ModEntry.Instance.I18n)
               );
           }

            // Read excavation skill level up recipes from data file
            foreach (KeyValuePair<string, List<string>> pair in ModEntry.ExcavationSkillLevelUpTable)
            {
                ExcavationSkillLevelUpRecipes.Add(int.Parse(pair.Key), pair.Value);
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
