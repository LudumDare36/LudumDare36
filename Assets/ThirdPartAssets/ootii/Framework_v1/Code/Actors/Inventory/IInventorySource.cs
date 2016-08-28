using System;
using UnityEngine;

namespace com.ootii.Actors.Inventory
{
    /// <summary>
    /// Interface used to abstract how inventory and item information is retrieved. Implement from this interface
    /// as needed in order to allow other assets to provide access to your character's inventory.
    /// </summary>
    public interface IInventorySource
    {
        /// <summary>
        /// Given the specified item, grab the resource path we'll use to instanciate the object with
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the item we want</param>
        /// <returns>String that is the resource path or an empty string if there is none found.</returns>
        string GetResourcePath(string rItemID);

        /// <summary>
        /// Tells the inventory system that specified item is now in the specified slot
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the item to equip</param>
        /// <param name="rSlotID">String representing the name or ID of the slot to equip</param>
        /// <returns></returns>
        void EquipItem(string rItemID, string rSlotID);

        /// <summary>
        /// Retrieves the item id for the item that is in the specified slot. If no item is slotted, returns an empty string.
        /// </summary>
        /// <param name="rSlotID">String representing the name or ID of the slot we're checking</param>
        /// <returns>ID of the item that is in the slot or the empty string</returns>
        string GetEquippedItemID(string rSlotID);

        /// <summary>
        /// Tells the inventory system that the slot should be emptied
        /// </summary>
        /// <param name="rSlotID">String representing the name or ID of the slot to unequip</param>
        /// <returns></returns>
        void ClearSlot(string rSlotID);
    }
}
