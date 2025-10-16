using BirbCore.Attributes;
using StardewModdingAPI;

namespace WizardrySkill.Core
{
    [SConfig]
    [SToken]
    public class Config
    {

        [SConfig.Option()]
        public bool VoidSchool { get; set; } = false;


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
    }
}
