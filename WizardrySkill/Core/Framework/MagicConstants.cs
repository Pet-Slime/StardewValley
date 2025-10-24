namespace WizardrySkill.Core.Framework
{
    /// <summary>Defines constants for the magic system.</summary>
    public class MagicConstants
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The number of spell bar which players are expected to have.</summary>
        public const int SpellBarCount = 2;

        /// <summary>The ID of the event in which the player learns magic from the Wizard.</summary>
        public const int LearnedMagicEventId = 90001;

        /// <summary>The number of mana points gained per magic level.</summary>
        public const int ManaPointsPerLevel = 5;

        /// <summary>The number of mana points gained per magic level.</summary>
        public const int ManaPointsBase = 100;


        /// <summary>The number of mana points gained per magic level.</summary>
        public const int ProfessionIncreaseMana = 100;
    }
}
