using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace WizardrySkill.Core.Framework
{
    public class Content
    {
        /*********
        ** Public methods
        *********/
        public static Texture2D LoadTexture(string path)
        {
            return Game1.content.Load<Texture2D>($"Mods/moonslime.WizardrySkill/{path}");
        }
    }
}
