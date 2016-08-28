using System;

namespace com.ootii.Actors.Inventory
{
    /// <summary>
    /// Very basic inventory item
    /// </summary>
    [Serializable]
    public class BasicInventoryItem
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string ID = "";

        /// <summary>
        /// Resource path to the item
        /// </summary>
        public string ResourcePath = "";
    }
}
