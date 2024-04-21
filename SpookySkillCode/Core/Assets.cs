using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;

namespace SpookySkill
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        [SAsset.Asset("assets/SpookyiconA.png")]
        public Texture2D IconA { get; set; }

        [SAsset.Asset("assets/SpookyiconB.png")]
        public Texture2D IconB { get; set; }

        [SAsset.Asset("assets/Spooky5a.png")]
        public Texture2D Spooky5a { get; set; }
        [SAsset.Asset("assets/Spooky5b.png")]
        public Texture2D Spooky5b { get; set; }
        [SAsset.Asset("assets/Spooky10a1.png")]
        public Texture2D Spooky10a1 { get; set; }
        [SAsset.Asset("assets/Spooky10a2.png")]
        public Texture2D Spooky10a2 { get; set; }
        [SAsset.Asset("assets/Spooky10b1.png")]
        public Texture2D Spooky10b1 { get; set; }
        [SAsset.Asset("assets/Spooky10b2.png")]
        public Texture2D Spooky10b2 { get; set; }
    }
}
