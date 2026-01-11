using Charactors;
using Effects;
using Store;
using System;
using UnityEngine;

namespace Stats
{
   
    [System.Serializable]
    public class ItemStat : Stat, IComparable<ItemStat>, IEquatable<ItemStat>
    {
        public ItemStat(ItemInfo itemInfo) : base(itemInfo)
        {
            
        }
        public ItemInfo ItemInfo { get => info as ItemInfo ;}
        public virtual int CompareTo(ItemStat info)
        {
            if(ItemInfo == null) throw new ArgumentNullException(nameof(ItemInfo));
            return ItemInfo.CompareTo(info.ItemInfo);
        }
        //静态道具数据（只读）直接传引用
        public override Stat Copy()
        {
           return this;
        }
        public bool Equals(ItemStat other)
        {
            if (ItemInfo == null) throw new ArgumentNullException(nameof(ItemInfo));
            if (other == null) return false;
            return ItemInfo.Equals(other.ItemInfo);
        }
        public virtual bool GetEffect(Charactor charactor,out Effect effect)
        {
            effect = ItemInfo.GetEffect(charactor);
            if(effect == null) return false;
            return true;
        }

    }
}
