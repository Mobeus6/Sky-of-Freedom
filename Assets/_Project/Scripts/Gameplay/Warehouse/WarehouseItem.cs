using UnityEngine;

namespace SkyOfFreedom.Warehouse
{
    [System.Serializable]
    public class WarehouseItem
    {
        public string ID
        {
            get;
            private set;
        }

        public int Quantity
        {
            get;
            private set;
        }

        public WarehouseItem(string id, int quantity)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError("Warehouse item ID is null or empty.");

                ID = string.Empty;
                Quantity = 0;

                return;
            }

            if (quantity < 0)
            {
                Debug.LogError("Warehouse quantity cannot be negative.");
                quantity = 0;
            }

            ID = id;
            Quantity = quantity;
        }

        public void SetQuantity(int quantity)
        {
            if (quantity < 0)
            {
                Debug.LogError("Warehouse quantity cannot be negative.");
                return;
            }

            Quantity = quantity;
        }

        public void Add(int quantity)
        {
            if (quantity <= 0)
            {
                Debug.LogError("Quantity must be greater than zero.");
                return;
            }

            Quantity += quantity;
        }

        public bool Remove(int quantity)
        {
            if (quantity <= 0)
            {
                Debug.LogError("Quantity must be greater than zero.");
                return false;
            }

            if (Quantity < quantity)
            {
                return false;
            }

            Quantity -= quantity;

            return true;
        }
    }
}
