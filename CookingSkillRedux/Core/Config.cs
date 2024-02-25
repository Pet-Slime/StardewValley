using MoonShared.Config;

namespace CookingSkill
{
    [ConfigClass(I18NNameSuffix = "")]
    public class Config
    {

        [ConfigOption]
        public bool EnablePrestige{ get; set; } = false;
    }
}
