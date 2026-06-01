using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewValley;

namespace PoisonBarAPI.Core
{

    [SAsset(Priority = 0)]
    public class Assets
    {

        public static Texture2D PoisonSymbol => Game1.content.Load<Texture2D>("Mods/moonslime.PoisonBarApi/poisonSymbol");
    }
}
