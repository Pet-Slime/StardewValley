using MoonShared.Attributes;
using StardewModdingAPI;

namespace WizardrySkill.Core
{
    [SConfig]
    public class Config
    {

        [SConfig.Option()]
        public bool VoidSchool { get; set; } = false;


        [SConfig.Option()]
        public bool EnableBaseManaRegen { get; set; } = true;

        [SConfig.Option(-500, 500, 1)]
        public int SpellBarOffset_X { get; set; } = 0;

        [SConfig.Option(-500, 500, 1)]
        public int SpellBarOffset_Y { get; set; } = 0;

        [SConfig.Option("WizardHouse")]
        public string RadioLocation { get; set; } = "WizardHouse";

        [SConfig.Option()]
        public int RadioX { get; set; } = -1;

        [SConfig.Option()]
        public int RadioY { get; set; } = -1;



        [SConfig.Option()]
        public SButton Key_Cast { get; set; } = SButton.Q;

        [SConfig.Option()]
        public SButton Key_SwapSpells { get; set; } = SButton.Tab;

        [SConfig.Option()]
        public SButton Key_Spell1 { get; set; } = SButton.D1;

        [SConfig.Option()]
        public SButton Key_Spell2 { get; set; } = SButton.D2;

        [SConfig.Option()]
        public SButton Key_Spell3 { get; set; } = SButton.D3;

        [SConfig.Option()]
        public SButton Key_Spell4 { get; set; } = SButton.D4;

        [SConfig.Option()]
        public SButton Key_Spell5 { get; set; } = SButton.D5;



        [SConfig.Option(1, 100, 1)]
        public int Magic_arrow_base { get; set; } = 2;

        [SConfig.Option(1, 100, 1)]
        public int Magic_arrow_scale { get; set; } = 15;

        [SConfig.Option(1, 100, 1)]
        public int Fire_ball_base { get; set; } = 3;

        [SConfig.Option(1, 100, 1)]
        public int Fire_ball_scale { get; set; } = 20;

        [SConfig.Option(1, 100, 1)]
        public int Frost_bolt_base { get; set; } = 2;

        [SConfig.Option(1, 100, 1)]
        public int Frost_bolt_scale { get; set; } = 10;



        [SConfig.Option(1, 100, 1)]
        public float Spirit_attack_range { get; set; } = 10;
    }
}
