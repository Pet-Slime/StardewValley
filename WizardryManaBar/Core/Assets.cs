using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;

namespace WizardryManaBar.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        [SAsset.Asset("assets/manabg.png")]
        public Texture2D ManaBG { get; set; }
    }
}
