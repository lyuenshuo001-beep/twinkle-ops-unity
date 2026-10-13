using UnityEngine;
namespace TwinkleOps
{
    public sealed class DroppedItem : MonoBehaviour
    {
        public string itemId, instanceId;
        public bool Take(Inventory bag)
        { if(!bag.Add(itemId,instanceId)) return false; Destroy(gameObject); return true; }
    }
}
