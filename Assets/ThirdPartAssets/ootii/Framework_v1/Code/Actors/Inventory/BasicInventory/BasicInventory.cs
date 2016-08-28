using System.Collections.Generic;
using UnityEngine;

namespace com.ootii.Actors.Inventory
{
    /// <summary>
    /// Creates a simple inventory system. That is both the "InventorySource" and the
    /// character inventory.
    /// 
    /// If you use a more advanced inventory system, simply create an "InventorySource" 
    /// that represents a bridge for your system.
    /// </summary>
    public class BasicInventory : MonoBehaviour, IInventorySource
    {
        /// <summary>
        /// List of inventory items
        /// </summary>
        public List<BasicInventoryItem> Items = new List<BasicInventoryItem>();

        /// <summary>
        /// List of slots with items
        /// </summary>
        public List<BasicInventorySlot> Slots = new List<BasicInventorySlot>();

        /// <summary>
        /// Given the specified item, grab the resource path we'll use to instanciate the object with
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the item we want</param>
        /// <returns>String that is the resource path or an empty string if there is none found.</returns>
        public virtual string GetResourcePath(string rItemID)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].ID == rItemID)
                {
                    return Items[i].ResourcePath;
                }
            }

            return "";
        }

        /// <summary>
        /// Tells the inventory system that specified item is now in the specified slot
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the item to equip</param>
        /// <param name="rSlotID">String representing the name or ID of the slot to equip</param>
        /// <returns></returns>
        public virtual void EquipItem(string rItemID, string rSlotID)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].ID == rSlotID)
                {
                    Slots[i].ItemID = rItemID;
                }
            }
        }

        /// <summary>
        /// Retrieves the item id for the item that is in the specified slot. If no item is slotted, returns an empty string.
        /// </summary>
        /// <param name="rSlotID">String representing the name or ID of the slot we're checking</param>
        /// <returns>ID of the item that is in the slot or the empty string</returns>
        public virtual string GetEquippedItemID(string rSlotID)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].ID == rSlotID)
                {
                    return Slots[i].ItemID;
                }
            }

            return "";
        }

        /// <summary>
        /// Tells the inventory system that specified slot should be cleared
        /// </summary>
        /// <param name="rSlotID">String representing the name or ID of the slot to unequip</param>
        /// <returns></returns>
        public virtual void ClearSlot(string rSlotID)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].ID == rSlotID)
                {
                    Slots[i].ItemID = "";
                }
            }
        }
    }
}
