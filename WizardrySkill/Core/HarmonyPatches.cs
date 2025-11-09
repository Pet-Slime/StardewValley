using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using SpaceCore;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Minigames;
using WizardrySkill.Core.Framework;
using WizardrySkill.Objects;

namespace WizardrySkill.Core
{


    [HarmonyPatch(typeof(StardewValley.Object), "_PopulateContextTags")]
    class PopulateContextTags_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(StardewValley.Object __instance, ref HashSet<string> tags)
        {
            // Map of item names to mana restoration values
            if (!ManaFillMap.TryGetValue(__instance.BaseName, out int manaValue))
                return;

            tags.Add($"moonslime.ManaBarApi.ManaFill/{manaValue}");
        }

        private static readonly Dictionary<string, int> ManaFillMap = new()
        {
            { "Common Mushroom", 5 },
            { "Purple Mushroom", 25 },
            { "Fried Mushroom", 10 },
            { "Stir Fry", 20 },
            { "Tom Kha Soup", 15 }
        };
    }

    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.performUseAction))]
    class Spellscrolls_patch
    {

        /*********
        ** Constants
        *********/
        private const string BaseModDataKey = "moonSlime.Wizardry.ActiveEffect";

        [HarmonyPrefix]
        private static bool Prefix(StardewValley.Object __instance, ref bool __result)
        {
            bool didStuff = false;
            if (!Game1.player.canMove || __instance.isTemporarilyInvisible)
            {
                __result = false;
                return false;
            }
            if (__instance.HasContextTag("moonslime_spellscroll"))
            {
                Farmer player = Game1.GetPlayer(Game1.player.UniqueMultiplayerID);
                string spell = __instance.Name;

                if (!string.IsNullOrEmpty(spell))
                {
                    Log.Trace("Casting " + spell);
                    Point pos = new Point(Game1.getMouseX() + Game1.viewport.X, Game1.getMouseY() + Game1.viewport.Y);
                    string entry = $"{player.UniqueMultiplayerID},{spell},0,{pos.X},{pos.Y}";
                    player.modData["moonslime.Wizardry.scrollspell"] = "yes";
                    Farm farm = Game1.getFarm();
                    foreach (var who in Game1.getOnlineFarmers())
                    {
                        string playerKey = $"{BaseModDataKey}/{who.UniqueMultiplayerID}";
                        Log.Trace($"Sending data to {who.displayName}");
                        if (!farm.modData.TryGetValue(playerKey, out string existing))
                            existing = "";

                        if (!string.IsNullOrEmpty(existing))
                            existing += "/";

                        farm.modData[playerKey] = existing + entry;
                    }
                    didStuff = true;
                    __result = true;
                }
            }

            return !didStuff;
        }
    }


    [HarmonyPatch]
    class Walkoflife_level_reset_patch
    {
        static MethodBase TargetMethod()
        {
            try
            {
                if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Professions"))
                    return typeof(Walkoflife_level_reset_patch).GetMethod(nameof(DummyMethod), BindingFlags.Static | BindingFlags.NonPublic);

                var type = AccessTools.TypeByName("DaLion.Professions.Framework.CustomSkill");
                if (type == null)
                    return typeof(Walkoflife_level_reset_patch).GetMethod(nameof(DummyMethod), BindingFlags.Static | BindingFlags.NonPublic);

                return AccessTools.Method(type, "Reset");
            }
            catch
            {
                return typeof(Walkoflife_level_reset_patch).GetMethod(nameof(DummyMethod), BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        // Dummy method just to return a MethodBase
        private static void DummyMethod() { }

        [HarmonyPrefix]
        public static void Prefix(object __instance)
        {
            try
            {
                // Skip entirely if DaLion.Professions isn’t active
                if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Professions"))
                    return;

                if (__instance == null)
                    return;

                // Reflect the skill ID (StringId)
                var nameProp = __instance.GetType().GetProperty("StringId", BindingFlags.Public | BindingFlags.Instance);
                string skillId = nameProp?.GetValue(__instance)?.ToString() ?? "UnknownSkill";

                // Ensure we’re handling the Wizardry skill only
                if (skillId != "moonslime.Wizard")
                    return;

                Farmer player = Game1.player;
                if (player == null)
                    return;

                player.modData["moonslime.Wizardry.PrestigeOriginalLevel"] = player.GetCustomSkillLevel("moonslime.Wizard").ToString();

                

                MoonShared.Attributes.Log.Trace("[Walkoflife] Wizard skill prefix handled successfully.");
            }
            catch (Exception ex)
            {
                MoonShared.Attributes.Log.Error($"[Walkoflife] Wizardry patch Error in CustomSkill.Reset patch: {ex}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(object __instance)
        {
            try
            {
                // Skip entirely if DaLion.Professions isn’t active
                if (!ModEntry.Instance.Helper.ModRegistry.IsLoaded("DaLion.Professions"))
                    return;

                if (__instance == null)
                    return;

                // Reflect the skill ID (StringId)
                var nameProp = __instance.GetType().GetProperty("StringId", BindingFlags.Public | BindingFlags.Instance);
                string skillId = nameProp?.GetValue(__instance)?.ToString() ?? "UnknownSkill";

                // Ensure we’re handling the Wizardry skill only
                if (skillId != "moonslime.Wizard")
                    return;

                Farmer player = Game1.player;
                if (player == null)
                    return;

                if (!player.modData.TryGetValue("moonslime.Wizardry.PrestigeOriginalLevel", out string origLevelStr) ||
                    !int.TryParse(origLevelStr, out int originalMagicLevel))
                    return;

                // Remove old Wizardry contribution
                int ManaToRemove = MagicConstants.ManaPointsBase + (originalMagicLevel * MagicConstants.ManaPointsPerLevel);
                if (player.HasCustomProfession(Wizard_Skill.Magic10b2))
                    ManaToRemove += MagicConstants.ProfessionIncreaseMana;
                player.AddToMaxMana(-ManaToRemove);

                // Recalculate new Wizardry contribution
                int magicLevel = player.GetCustomSkillLevel("moonslime.Wizard");
                int expectedMaxMana = MagicConstants.ManaPointsBase + (magicLevel * MagicConstants.ManaPointsPerLevel);
                if (player.HasCustomProfession(Wizard_Skill.Magic10b2))
                    expectedMaxMana += MagicConstants.ProfessionIncreaseMana;
                player.AddToMaxMana(expectedMaxMana);
                player.SetManaToMax();

                // Remove temporary key
                player.modData.Remove("moonslime.Wizardry.PrestigeOriginalLevel");

                // Reset spellbook state
                SpellBook spellBook = player.GetSpellBook();
                if (spellBook != null)
                {
                    foreach (PreparedSpellBar spellBar in spellBook.Prepared)
                        spellBar.Spells.Clear();

                    List<string> spellList = new List<string>();
                    foreach (string spellId in SpellManager.GetAll())
                    {
                        spellList.Add(spellId);
                        spellBook.ForgetSpell(spellId, 0);
                    }
                    spellBook.SetSpellPointsToZero();

                    // Adjust points for prestige professions
                    if (player.HasCustomProfession(Wizard_Skill.Magic5a))
                        magicLevel += 2;
                    if (player.HasCustomProfession(Wizard_Skill.Magic10a1))
                        magicLevel += 2;

                    spellBook.UseSpellPoints(magicLevel * -1);

                    // Re-add default spells
                    foreach (string spellId in spellList)
                    {
                        if (!spellBook.KnowsSpell(spellId, 0))
                            spellBook.LearnSpell(spellId, 0, true);
                    }
                }

                // Mark prestige flag
                player.modData["moonSlime.Wizardry.HasPrestigedMagic"] = "yes";

                MoonShared.Attributes.Log.Trace("[Walkoflife] Wizard skill reset handled successfully.");
            }
            catch (Exception ex)
            {
                MoonShared.Attributes.Log.Error($"[Walkoflife] Wizardry patch Error in CustomSkill.Reset patch: {ex}");
            }
        }

    }

}
