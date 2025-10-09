using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace ManaBar
{
    public interface IManaBarApi
    {
        int GetMana(Farmer farmer);
        void AddMana(Farmer farmer, int amt);
        int GetMaxMana(Farmer farmer);
        void SetMaxMana(Farmer farmer, int newMaxMana);
        void SetManaToMax(Farmer farmer);
    }

    public class Api : IManaBarApi
    {
        public const int BaseMaxMana = 100;

        public int GetMana(Farmer farmer)
        {
            return farmer.GetCurrentMana();
        }

        public void AddMana(Farmer farmer, int amt)
        {
            farmer.AddMana(amt);
        }

        public int GetMaxMana(Farmer farmer)
        {
            return farmer.GetMaxMana();
        }

        public void SetMaxMana(Farmer farmer, int newMaxMana)
        {
            farmer.SetMaxMana(newMaxMana);
        }

        public void SetManaToMax(Farmer farmer)
        {
            farmer.SetManaToMax();
        }
    }
}
