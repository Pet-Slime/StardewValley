using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;

namespace AthleticSkill
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        [SAsset.Asset("assets/AthleticiconA.png")]
        public Texture2D IconA { get; set; }

        [SAsset.Asset("assets/AthleticiconB.png")]
        public Texture2D IconB { get; set; }

        [SAsset.Asset("assets/Athletic5a.png")]
        public Texture2D Athletic5a { get; set; }
        [SAsset.Asset("assets/Athletic5b.png")]
        public Texture2D Athletic5b { get; set; }
        [SAsset.Asset("assets/Athletic10a1.png")]
        public Texture2D Athletic10a1 { get; set; }
        [SAsset.Asset("assets/Athletic10a2.png")]
        public Texture2D Athletic10a2 { get; set; }
        [SAsset.Asset("assets/Athletic10b1.png")]
        public Texture2D Athletic10b1 { get; set; }
        [SAsset.Asset("assets/Athletic10b2.png")]
        public Texture2D Athletic10b2 { get; set; }
    }
}
