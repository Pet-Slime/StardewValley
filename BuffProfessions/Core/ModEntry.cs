using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BirbCore.Attributes;
using StardewModdingAPI;

namespace BuffProfessions.Core
{
    [SMod]
    public class ModEntry : Mod
    {
        [SMod.Instance]
        internal static ModEntry Instance;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Parser.ParseAll(this);
        }
    }
}
