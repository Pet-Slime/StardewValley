using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;

namespace BibliocraftSkill
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        [SAsset.Asset("assets/BookiconA.png")]
        public Texture2D IconA { get; set; }

        [SAsset.Asset("assets/BookiconB.png")]
        public Texture2D IconB { get; set; }

        [SAsset.Asset("assets/Book5a.png")]
        public Texture2D Book5a { get; set; }
        [SAsset.Asset("assets/Book5b.png")]
        public Texture2D Book5b { get; set; }
        [SAsset.Asset("assets/Book10a1.png")]
        public Texture2D Book10a1 { get; set; }
        [SAsset.Asset("assets/Book10a2.png")]
        public Texture2D Book10a2 { get; set; }
        [SAsset.Asset("assets/Book10b1.png")]
        public Texture2D Book10b1 { get; set; }
        [SAsset.Asset("assets/Book10b2.png")]
        public Texture2D Book10b2 { get; set; }


        [SAsset.Asset("assets/bookworm_buff.png")]
        public Texture2D Bookworm_buff { get; set; }

    }
}
