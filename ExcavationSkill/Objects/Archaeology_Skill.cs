using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using MoonShared;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewValley;
using SpaceCore.Interface;
using StardewModdingAPI.Events;
using StardewModdingAPI;

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
        public readonly IModHelper _modHelper;

        public Archaeology_Skill() : base("moonslime.Archaeology")
        {
            this.Icon = ModEntry.Assets.IconA;
            if (ModEntry.Config.AlternativeSkillPageIcon == 1)
            {
                this.SkillsPageIcon = ModEntry.Assets.IconBalt;
            } else
            {
                this.SkillsPageIcon = ModEntry.Assets.IconB;
            }
            this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(205, 127, 50);
            this.ExperienceCurve = new[] { 100, 380, 770, 1300, 2150, 3300, 4000, 6900, 10000, 15000 };
            this.AddProfessions(
                Archaeology5a = new KeyedProfession(this, "Archaeology5a", ModEntry.Assets.Archaeology5a, ModEntry.Instance.I18N),
                Archaeology5b = new KeyedProfession(this, "Archaeology5b", ModEntry.Assets.Archaeology5b, ModEntry.Instance.I18N),
                Archaeology10a1 = new KeyedProfession(this, "Archaeology10a1", ModEntry.Assets.Archaeology10a1, ModEntry.Instance.I18N),
                Archaeology10a2 = new KeyedProfession(this, "Archaeology10a2", ModEntry.Assets.Archaeology10a2, ModEntry.Instance.I18N),
                Archaeology10b1 = new KeyedProfession(this, "Archaeology10b1", ModEntry.Assets.Archaeology10b1, ModEntry.Instance.I18N),
                Archaeology10b2 = new KeyedProfession(this, "Archaeology10b2", ModEntry.Assets.Archaeology10b2, ModEntry.Instance.I18N)
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
                ModEntry.Instance.I18N.Get("skill.perk", new { bonus = 0.05 })
            };            
            return result;
        }

        public override string GetSkillPageHoverText(int level)
        {
            double value = level * 0.05;
            double truncated = ((int)(value * 100)) / 100.00;
            return ModEntry.Instance.I18N.Get("skill.perk", new { bonus = truncated });
        }

        public void DisplayEvents_MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is not SkillLevelUpMenu levelUpMenu)
            {
                return;
            }

            string skill = this._modHelper.Reflection.GetField<string>(levelUpMenu, "currentSkill").GetValue();
            if (skill != this.Id)
            {
                return;
            }

            int level = this._modHelper.Reflection.GetField<int>(levelUpMenu, "currentLevel").GetValue();

            List<CraftingRecipe> newRecipes = [];

            int menuHeight = 0;
            foreach (KeyValuePair<string, string> recipePair in CraftingRecipe.craftingRecipes)
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(skill) || !conditions.Contains(level.ToString()))
                {
                    continue;
                }

                CraftingRecipe recipe = new(recipePair.Key, isCookingRecipe: false);
                newRecipes.Add(recipe);
                Game1.player.craftingRecipes.TryAdd(recipePair.Key, 0);
                menuHeight += recipe.bigCraftable ? 128 : 64;
            }

            foreach (KeyValuePair<string, string> recipePair in CraftingRecipe.cookingRecipes)
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 3, "");
                if (!conditions.Contains(skill) || !conditions.Contains(level.ToString()))
                {
                    continue;
                }

                CraftingRecipe recipe = new(recipePair.Key, isCookingRecipe: true);
                newRecipes.Add(recipe);
                if (Game1.player.cookingRecipes.TryAdd(recipePair.Key, 0) &&
                    !Game1.player.hasOrWillReceiveMail("robinKitchenLetter"))
                {
                    Game1.mailbox.Add("robinKitchenLetter");
                }

                menuHeight += recipe.bigCraftable ? 128 : 64;
            }

            this._modHelper.Reflection.GetField<List<CraftingRecipe>>(levelUpMenu, "newCraftingRecipes")
                .SetValue(newRecipes);

            levelUpMenu.height = menuHeight + 256 + (levelUpMenu.getExtraInfoForLevel(skill, level).Count * 64 * 3 / 4);
        }

        /// <summary>
        /// Tries to recover skills from invalid states, such as not having professions or recipes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            int skillLevel = Game1.player.GetCustomSkillLevel(this.Id);
            if (skillLevel == 0)
            {
                return;
            }

            if (skillLevel >= 5 && !(Game1.player.HasCustomProfession(this.Professions[0]) ||
                                     Game1.player.HasCustomProfession(this.Professions[1])))
            {
                Game1.endOfNightMenus.Push(new SkillLevelUpMenu(this.Id, 5));
            }

            if (skillLevel >= 10 && !(Game1.player.HasCustomProfession(this.Professions[2]) ||
                                      Game1.player.HasCustomProfession(this.Professions[3]) ||
                                      Game1.player.HasCustomProfession(this.Professions[4]) ||
                                      Game1.player.HasCustomProfession(this.Professions[5])))
            {
                Game1.endOfNightMenus.Push(new SkillLevelUpMenu(this.Id, 10));
            }

            foreach (KeyValuePair<string, string> recipePair in DataLoader.CraftingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 4, "");
                if (!conditions.Contains(this.Id))
                {
                    continue;
                }
                if (conditions.Split(" ").Length < 2)
                {
                    continue;
                }

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                {
                    continue;
                }

                Game1.player.craftingRecipes.TryAdd(recipePair.Key, 0);
            }

            foreach (KeyValuePair<string, string> recipePair in DataLoader.CookingRecipes(Game1.content))
            {
                string conditions = ArgUtility.Get(recipePair.Value.Split('/'), 3, "");
                if (!conditions.Contains(this.Id))
                {
                    continue;
                }
                if (conditions.Split(" ").Length < 2)
                {
                    continue;
                }

                int level = int.Parse(conditions.Split(" ")[1]);

                if (skillLevel < level)
                {
                    continue;
                }

                if (Game1.player.cookingRecipes.TryAdd(recipePair.Key, 0) &&
                    !Game1.player.hasOrWillReceiveMail("robinKitchenLetter"))
                {
                    Game1.mailbox.Add("robinKitchenLetter");
                }
            }
        }
    }
}
