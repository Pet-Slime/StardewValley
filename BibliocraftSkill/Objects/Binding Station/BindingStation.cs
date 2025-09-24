using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley.GameData.Machines;
using StardewValley.Objects;
using StardewValley;
using Object = StardewValley.Object;
using Netcode;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Tools;

namespace BibliocraftSkill.Objects.Binding_Station
{
    [XmlType("Mods_moonslime.Bibliocraft.binding_station")]
    public class BindingStation : Object
    {
        public static Item OutputCask(Object machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = null;
            if (!probe)
            {
                int defaultTime = 10;
                string itemFlavor = "";

                Object @object_0 = (Object)inputItem.getOne();
                itemFlavor = @object_0.preservedParentSheetIndex.ToString();
               
                if (itemFlavor == "") 
                {
                    return null;
                }

                Object @object = ItemRegistry.Create<Object>(itemFlavor);
                overrideMinutesUntilReady = defaultTime;
                return @object;
            }

            return null;
        }
    }
}
