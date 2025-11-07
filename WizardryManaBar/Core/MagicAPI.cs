using StardewValley;

namespace WizardryManaBar.Core
{
    public interface IManaBarApi
    {
        int GetMana(Farmer farmer);
        void AddMana(Farmer farmer, int amt);
        int GetMaxMana(Farmer farmer);
        void SetMaxMana(Farmer farmer, int newMaxMana);
        void AddToMaxMana(Farmer farmer, int maxManaToAdd);
        void IsCurrentManaGreaterThanValue(Farmer farmer, int valueToCheckAgainst);
        void IsCurrentManaLessThanValue(Farmer farmer, int valueToCheckAgainst);
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

        public void AddToMaxMana(Farmer farmer, int maxManaToAdd)
        {
            farmer.SetMaxMana(farmer.GetMaxMana() + maxManaToAdd);
        }

        public void SetManaToMax(Farmer farmer)
        {
            farmer.SetManaToMax();
        }

        public void IsCurrentManaGreaterThanValue(Farmer farmer, int valueToCheckAgainst)
        {
            farmer.IsCurrentManaGreaterThanValue(valueToCheckAgainst);
        }

        public void IsCurrentManaLessThanValue(Farmer farmer, int valueToCheckAgainst)
        {
            farmer.IsCurrentManaLessThanValue(valueToCheckAgainst);
        }
    }
}
