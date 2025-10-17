using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace WizardryManaBar.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        public Texture2D ManaBG => Game1.content.Load<Texture2D>("Mods/moonslime.ManaBarApi/textures");
    }
}
