using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewModdingAPI.Events;
using StardewValley;

namespace MultipleConstructionOrders.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {

        public const string ConstructionWorkerSpriteAsset = "Mods/moonslime.MultipleConstructionOrders/textures";
        public const string ConstructionWorkerPortraitPrefix = "Portraits/moonslime_MCO_ConstructionWorker_";

        public static Texture2D ConstructionWorker => Game1.content.Load<Texture2D>(ConstructionWorkerSpriteAsset);

    }
}

