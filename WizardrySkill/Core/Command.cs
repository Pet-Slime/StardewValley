using BirbCore.Attributes;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using WizardrySkill.Core.Framework;
using WizardrySkill.Objects;

namespace WizardryManaBar.Core
{
    [SCommand("player_wizard_fixMana")]
    public class Command_playerWizardFixMana
    {
        [SCommand.Command("Adjust all player's mana to the level it should be based off wizardry values")]
        public static void Run()
        {
            if (!Context.IsPlayerFree)
                return;
            foreach (Farmer player in Game1.getOnlineFarmers())
            {
                int magicLevel = player.GetCustomSkillLevel("moonslime.Wizard");
                // fix mana pool
                int expectedMaxMana = MagicConstants.ManaPointsBase + (magicLevel * MagicConstants.ManaPointsPerLevel);
                if (player.HasCustomProfession(Wizard_Skill.Magic10b2))
                    expectedMaxMana += MagicConstants.ProfessionIncreaseMana;

                // Fix Manapool
                if (player.GetMaxMana() != expectedMaxMana)
                {
                    player.SetMaxMana(expectedMaxMana);
                    player.SetManaToMax();
                }
                else if (player.GetCurrentMana() < expectedMaxMana)
                {
                    player.SetManaToMax();
                }
            }


        }
    }

    [SCommand("player_wizard_learnAllSpells")]
    public class Command_playerWizardLearnAllSpells
    {
        [SCommand.Command("Have all player's that are online learn all spells")]
        public static void Run()
        {
            if (!Context.IsPlayerFree)
                return;

            foreach (Farmer player in Game1.getOnlineFarmers())
            {

                SpellBook spellBook = player.GetSpellBook();
                foreach (string spellId in SpellManager.GetAll())
                {
                    spellBook.LearnSpell(spellId, 0);

                }
            }
        }
    }

    [SCommand("player_wizard_forgetAllSpells")]
    public class Command_player_wizard_forgetAllSpells
    {
        [SCommand.Command("Have all player's that are online forget all spells")]
        public static void Run()
        {
            if (!Context.IsPlayerFree)
                return;

            foreach (Farmer player in Game1.getOnlineFarmers())
            {
                SpellBook spellBook = player.GetSpellBook();
                foreach (PreparedSpellBar spellBar in spellBook.Prepared)
                {
                    spellBar.Spells.Clear();
                }
                foreach (string spellId in SpellManager.GetAll())
                {
                    spellBook.ForgetSpell(spellId, 0);
                }
                player.GetSpellBook().SetSpellPointsToZero();

                int magicLevel = player.GetCustomSkillLevel("moonslime.Wizard");

                if (Game1.player.HasCustomProfession(Wizard_Skill.Magic5a))
                    magicLevel += 2;
                if (Game1.player.HasCustomProfession(Wizard_Skill.Magic10a1))
                    magicLevel += 2;

                player.GetSpellBook().UseSpellPoints(magicLevel * -1);

                foreach (string spellId in CoreSpells)
                {
                    if (!spellBook.KnowsSpell(spellId, 0))
                        spellBook.LearnSpell(spellId, 0, true);
                }
            }

        }

        /// <summary>Base arcane spells that all magic users should know.</summary>
        private static readonly string[] CoreSpells =
        {
            "arcane:analyze",
            "elemental:magicmissle",
            "arcane:enchant",
            "arcane:disenchant"
        };
    }
}
