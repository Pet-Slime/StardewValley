using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;

namespace ThievingSkill
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        [SAsset.Asset("assets/ThievingiconA.png")]
        public Texture2D IconA { get; set; }

        [SAsset.Asset("assets/ThievingiconB1.png")]
        public Texture2D IconB1 { get; set; }

        [SAsset.Asset("assets/ThievingiconB2.png")]
        public Texture2D IconB2 { get; set; }

        [SAsset.Asset("assets/ThievingiconB3.png")]
        public Texture2D IconB3 { get; set; }

        [SAsset.Asset("assets/Thieving5a.png")]
        public Texture2D Thieving5a { get; set; }
        [SAsset.Asset("assets/Thieving5b.png")]
        public Texture2D Thieving5b { get; set; }
        [SAsset.Asset("assets/Thieving10a1.png")]
        public Texture2D Thieving10a1 { get; set; }
        [SAsset.Asset("assets/Thieving10a2.png")]
        public Texture2D Thieving10a2 { get; set; }
        [SAsset.Asset("assets/Thieving10b1.png")]
        public Texture2D Thieving10b1 { get; set; }
        [SAsset.Asset("assets/Thieving10b2.png")]
        public Texture2D Thieving10b2 { get; set; }
    }
}
