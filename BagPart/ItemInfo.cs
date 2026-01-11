using Charactors;
using Effects;
using UnityEngine;

namespace Store
{
    public enum ItemType
    {
        None,   
        Money,
        AttackAdd,
        DefenceAdd,
        HpAdd,
        Drug,
        AtkSpeed,
        Other,
    }
    [CreateAssetMenu(fileName = "newItem", menuName = "info/Item")]
    public class ItemInfo : Info
    {
        [SerializeField] private ItemType itemType;
        [SerializeField] private string text;
        [SerializeField] private int cost;
        [SerializeField] private Sprite image;
        [SerializeField] private int maxStackCount=99;
        public Sprite Image { get => image; }
        public string Text { get => text; }
        public int MaxStackCount { get => maxStackCount; }
        public int Cost { get => cost; }
        public ItemType ItemType { get => itemType; }
        public override int CompareTo(Info info)
        {
            if (info is ItemInfo item)
            {
                if (info == null) return 1;
                if (itemType > item.itemType) return 1;
                if (itemType < item.itemType) return -1;
                return Id.CompareTo(info.Id);
            }
            return base.CompareTo(info);
        }  
        public virtual Effect GetEffect(Charactor origin)
        {
            return null;
        }
    }
}
