using MoonShared.Attributes;
using StardewModdingAPI;

namespace ThievingSkill.Core
{
    [SConfig]
    public class Config
    {
        // ------------------------------------------------------------
        // General
        // ------------------------------------------------------------

        [SConfig.PageLink("General")]
        [SConfig.PageLink("Pickpocketing")]
        [SConfig.PageLink("Lockpicking")]
        [SConfig.PageLink("Shoplifting")]

        [SConfig.PageBlock("General")]
        [SConfig.SectionTitle("GeneralSettings")]

        [SConfig.Option(1, 3, 1)]
        public int ThiefIcon { get; set; } = 1;

        [SConfig.Option()]
        public SButton Key_Cast { get; set; } = SButton.B;


        // ------------------------------------------------------------
        // Pickpocketing
        // ------------------------------------------------------------

        [SConfig.PageBlock("Pickpocketing")]
        [SConfig.SectionTitle("PickpocketingSettings")]

        [SConfig.Option()]
        public bool MonstersOnly { get; set; } = false;

        [SConfig.Option()]
        public bool ShortCoolDown { get; set; } = false;

        [SConfig.Option(0.0f, 50f, 1.0f)]
        public float ExpMod { get; set; } = 1f;

        [SConfig.SectionTitle("PickpocketingExperience")]

        [SConfig.Option(0, 100, 1)]
        public int ExpFromFail { get; set; } = 1;

        [SConfig.Option(0, 100, 1)]
        public int ExpLevel1 { get; set; } = 8;

        [SConfig.Option(0, 100, 1)]
        public int ExpLevel2 { get; set; } = 16;

        [SConfig.Option(0, 100, 1)]
        public int ExpLevel3 { get; set; } = 24;

        [SConfig.Option(0, 100, 1)]
        public int ExpLevel4 { get; set; } = 32;


        // ------------------------------------------------------------
        // Lockpicking
        // ------------------------------------------------------------

        [SConfig.PageBlock("Lockpicking")]
        [SConfig.SectionTitle("LockpickingSettings")]

        // How much EXP the player gets when they successfully lockpick a door
        [SConfig.Option(0, 100, 1)]
        public int LockpickingExp { get; set; } = 5;

        // How far away NPCs can notice the player lockpicking
        [SConfig.Option(0.0f, 25f, 0.5f)]
        public float LockpickingWitnessRadius { get; set; } = 10f;

        // How much friendship nearby NPCs lose when they witness lockpicking
        [SConfig.Option(0, 500, 5)]
        public int LockpickingWitnessFriendshipLoss { get; set; } = 25;


        // ------------------------------------------------------------
        // Shoplifting
        // ------------------------------------------------------------

        [SConfig.PageBlock("Shoplifting")]
        [SConfig.SectionTitle("ShopliftingSettings")]

        // How much internal item value each Thieving level safely covers before adding extra caught chance
        [SConfig.Option(1, 5000, 25)]
        public int ShopliftGoldPerLevel { get; set; } = 175;

        // Base caught chance before skill level, daily attempts, and item value are applied
        [SConfig.Option(0.0f, 1.0f, 0.01f)]
        public float ShopliftingBaseCatchChance { get; set; } = 0.25f;

        // How much caught chance is reduced per Thieving level
        [SConfig.Option(0.0f, 1.0f, 0.01f)]
        public float ShopliftingCatchChanceReductionPerLevel { get; set; } = 0.01f;

        // How much caught chance is added for each shoplifting attempt already made today
        [SConfig.Option(0.0f, 1.0f, 0.01f)]
        public float ShopliftingCatchChanceIncreasePerAttemptToday { get; set; } = 0.05f;

        // How much caught chance is added for each value step above the player's safe value limit
        [SConfig.Option(0.0f, 1.0f, 0.01f)]
        public float ShopliftingCatchChanceIncreasePerValueStepOverLimit { get; set; } = 0.05f;

        // How many days the player is banned from a shop after being caught
        [SConfig.Option(0, 28, 1)]
        public int ShopliftingBanDays { get; set; } = 5;

        [SConfig.SectionTitle("ShopliftingExperience")]

        // How much EXP the player gets when they successfully shoplift
        [SConfig.Option(0, 500, 5)]
        public int ShopliftingSuccessExp { get; set; } = 25;

        // How much EXP the player gets when they are caught
        [SConfig.Option(0, 500, 5)]
        public int ShopliftingCaughtExp { get; set; } = 0;

        [SConfig.SectionTitle("ShopliftingWitnesses")]

        // How far away NPCs can witness the player getting caught
        [SConfig.Option(0.0f, 25f, 0.5f)]
        public float ShopliftingWitnessRadius { get; set; } = 25f;

        // How much friendship nearby NPCs lose when the player is caught
        [SConfig.Option(0, 500, 5)]
        public int ShopliftingWitnessFriendshipLoss { get; set; } = 75;

        // How much friendship the shop owner loses when the player is caught
        [SConfig.Option(0, 500, 5)]
        public int ShopliftingShopOwnerFriendshipLoss { get; set; } = 75;
    }
}
