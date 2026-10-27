using UnityEngine;

namespace HorseRanch
{
    /// <summary>상점 소모품 1종</summary>
    [CreateAssetMenu(fileName = "Item", menuName = "HorseRanch/Item Definition")]
    public class ItemDefinitionSO : ScriptableObject
    {
        [SerializeField] private ItemDefinition item = new ItemDefinition();
        public ItemDefinition Item { get { return item; } }
    }
}
