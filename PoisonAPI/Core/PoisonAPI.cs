using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace PoisonBarAPI.Core
{
    public interface IPoisonBarApi
    {
        int GetPoison(Farmer farmer);
        void AddPoison(Farmer farmer, int amt);
    }
    public class Api : IPoisonBarApi
    {

        public int GetPoison(Farmer farmer)
        {
            return farmer.GetCurrentPoison();
        }

        public void AddPoison(Farmer farmer, int amt)
        {
            farmer.AddPoison(amt);
        }
    }
}
