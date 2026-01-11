using UnityEngine;

namespace Store
{
    [CreateAssetMenu(fileName = "NewBlock", menuName = "Game/Info/Block")]
    public class BlocksInfo : ItemInfo
    {
        [SerializeField] private Material material;
        [SerializeField] private BlockType blockType = BlockType.Solid;
        [SerializeField] private float toughness = 2f;
        public BlockType BlockType { get => blockType;}
        public Material Material { get => material;}
        public float Toughness { get => toughness;}
    }
}
