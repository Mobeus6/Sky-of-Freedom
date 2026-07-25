using System;

namespace SkyOfFreedom.Inventory
{
    [Serializable]
    public class InventoryItem
    {
        private string id;
        private int amount;

        public string ID => id;
        public int Amount => amount;

        public InventoryItem(string id, int amount)
        {
            this.id = id;
            this.amount = amount;
        }

        public void SetAmount(int amount)
        {
            this.amount = amount;
        }

        public void Add(int amount)
        {
            this.amount += amount;
        }

        public bool Remove(int amount)
        {
            if (this.amount < amount)
            {
                return false;
            }

            this.amount -= amount;
            return true;
        }
    }
}