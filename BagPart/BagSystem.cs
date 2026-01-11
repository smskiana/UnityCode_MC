using Bags;
using Stats;
using Store;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BagSystem : MonoBehaviour
{
    [System.Serializable]
    private class BagCell : IComparable<BagCell>,IEquatable<BagCell>
    {
        public int Index;
        [SerializeField]private ItemStat item;
        [SerializeField]private int count;
        public event Action<int> CellStatChange;

        public BagCell() { }
        public BagCell (Action<int> OnCellStatChange, ItemStat item, int count)
        {
            this.Item = item;
            this.Count = count;        
            this.CellStatChange += OnCellStatChange;
        }
        public BagCell(Action<int> OnCellStatChange)
        {
            this.CellStatChange += OnCellStatChange;
            Item = null;
            Count = 0;
        }

        public ItemStat Item { get => item; set => item = value; }
        public int Count { get => count; set 
            {
                if(count == value) return;
                count = value;
                CellStatChange?.Invoke(Index);
            } }

        public void OnItemStatChange()
        {
            CellStatChange?.Invoke(Index);
        }
        public bool IsEmpty()
        {
            return item == null || Count <= 0;
        }
        public void Reset()
        {
            Item = null;
            Count = 0;
        }
        public void Reset(BagCell bagPair)
        {
            Item = bagPair.Item;
            Count = bagPair.Count;
        }
        public int Add(BagCell pair)
        {
            if (pair == null || pair.IsEmpty()) throw new Exception("");

            if (IsEmpty())
            {
                Item = pair.Item;
                Count = pair.Count;
            }
            else if (Item .Equals(pair.Item))
            {
                Count += pair.Count;

            }
            else
            {
                return pair.Count;
            }
            if(Count > Item.ItemInfo.MaxStackCount)
            {
                var rest = Count - Item.ItemInfo.MaxStackCount;
                Count = Item.ItemInfo.MaxStackCount;
                return rest;
            }
            return 0;
        }
        public int Add(ItemStat item,int count)
        {
            if (item == null || count <= 0) return count;

            if (IsEmpty())
            {
                this.Item =item;
                this.Count = count;
            }
            else if (this.Item.Equals(item))
            {
                this.Count += count;
            }
            else
            {
                return count;
            }

            if (this.Count > Item.ItemInfo.MaxStackCount)
            {
                var rest = this.Count - Item.ItemInfo.MaxStackCount;
                this.Count = Item.ItemInfo.MaxStackCount;
                return rest;
            }
            return 0;
        }
        public bool Equals(BagCell other)
        {
            if (other == null) return false;
            if (IsEmpty())
            {
              if(other.IsEmpty()) return true;
              return false;
            }
              return item.Equals(other.Item);
        }
        public bool Equals(ItemStat other)
        {
            return Item != null && Item.Equals(other);
        }
        public bool Take(int count,out int rest)
        {
            rest = count;
            if(IsEmpty()) return false;

            if(this.Count >= count)
            {
                this.Count -= count;
                rest = 0;
                return true;
            }
            else
            {
                rest = count - this.Count;
                this.Count = 0;
                return false;
            }
        }
        public BagCell Copy()
        {
            return new()
            {
                Item = Item,
                Count = Count,
            };
        }
        public override int GetHashCode()
        {
            return Item == null ? 0 : Item.GetHashCode();
        }
        public int CompareTo(BagCell other)
        {
            if (other == null) return 1;
            bool aEmpty = IsEmpty();
            bool bEmpty = other.IsEmpty();

            if (aEmpty && !bEmpty) return 1;
            if (!aEmpty && bEmpty) return -1;
            if (aEmpty && bEmpty) return 0;

            int itemCompare = Item.CompareTo(other.Item);
            if (itemCompare != 0) return itemCompare;

            return other.Count.CompareTo(Count); // 数量降序
        }

        public bool NotFull()
        {
            if(item == null) return false;
            return (Count< item.ItemInfo.MaxStackCount);
        }
       
    }



    private readonly struct EventLockScope : IDisposable
    {
        private readonly BagSystem bag;
        public EventLockScope(BagSystem bag)
        {
            this.bag = bag;
            bag.lockCellCountChangeEvent = true;
        }
        public void Dispose()
        {
            bag.lockCellCountChangeEvent = false;
        }
    }

    [SerializeField] private int size;
    [SerializeField] private List<BagCell> bagItems;
    [SerializeField] private SerializableMinHeap<int> emptyPos;
    [SerializeField] private bool lockCellCountChangeEvent = false;
    [SerializeField] private bool initAccomplished;
#if UNITY_EDITOR
    [SerializeField] string Mask;
#endif
    private BagLayMask mask;
    public virtual int Size => size;
    public bool InitAccomplished { get => initAccomplished;}
    #region 事件
    private event Action<int> ItemRefreshed;
    private event Action<int, int> ItemSwapped;
    private event Action AllItemsRefreshed;
    private event Action<int> BagSizeChanged;
    public void RegisterItemRefreshed(Action<int> callback) => ItemRefreshed += callback;
    public void UnregisterItemRefreshed(Action<int> callback) => ItemRefreshed -= callback;
    public void RegisterItemSwapped(Action<int, int> callback) => ItemSwapped += callback;
    public void UnregisterItemSwapped(Action<int, int> callback) => ItemSwapped -= callback;
    public void RegisterAllItemsRefreshed(Action callback) => AllItemsRefreshed += callback;
    public void UnregisterAllItemsRefreshed(Action callback) => AllItemsRefreshed -= callback;
    public void RegisterSizeChanged(Action<int> callback) => BagSizeChanged += callback;
    public void UnregisterSizeChanged(Action<int> callback) => BagSizeChanged -= callback;
    // 在原来的事件触发点调用新事件
    protected void TriggerItemRefreshed(int index) => ItemRefreshed?.Invoke(index);
    protected void TriggerItemSwapped(int a, int b) => ItemSwapped?.Invoke(a, b);
    protected void TriggerAllItemsRefreshed() => AllItemsRefreshed?.Invoke();
    protected void TriggerSizeChanged(int a) => BagSizeChanged?.Invoke(a);
    #endregion
    public void Start()
    {
        if (mask == null || mask.Equals(new BagLayMask())) mask = BagLayMask.AllContainMask;
#if UNITY_EDITOR
        Mask = Convert.ToString(mask.Mask, 2);
#endif
        bagItems ??= new List<BagCell>(size);
        int i = 0;
        //TODO:测试中，记得更换
        while (bagItems.Count < size)
        {
            bagItems.Add(new BagCell(OnCellStatChange));
            i++;
        }
        emptyPos = new SerializableMinHeap<int>();
        RefreshIndex();
        InitEmptyPos();

    }
    private void InitEmptyPos()
    {
        emptyPos.Clear();
        for (int i = 0; i < size; i++)
            if (bagItems[i].IsEmpty())
                emptyPos.Add(i);
    }
    public void RefreshIndex()
    {
        for (int i = 0; i < bagItems.Count; i++)
        {
            bagItems[i].Index = i;
        }
    }
    public int Add(ItemStat item,int count)
    {
        if(!mask.Contains(item.ItemInfo.ItemType)) return count;

        if (item == null || count <= 0)
        {
            Debug.LogWarning("无法添加：item为null||数量小于1");
            return count;
        }
        int DefaultMaxCountPerCell = item.ItemInfo.MaxStackCount;
        int index = bagItems.FindIndex(x =>
            !x.IsEmpty() &&
            x.Equals(item)&&
            x.Count < DefaultMaxCountPerCell);
        if (index >= 0)
        {
            int bur = bagItems[index].Add(item,count);
            if (bur > 0)
            {
                return Add(item,bur);
            }
            return 0;
        }
        if (emptyPos.Count > 0)
        {
            int pos = emptyPos.Pop();
            int rest = bagItems[pos].Add(item,count);
            if(rest > 0)
            {
               return Add(item,rest);
            }
            return 0;
        }

        return count;
    }
    public (ItemStat item, int count) Take(int pos, int count = int.MaxValue)
    {
        if (pos < 0 || pos >= size) return (null,-1);
        if (count <= 0) return (null, 0);
        var slot = bagItems[pos];
        var item = slot.Item;
        if (slot.IsEmpty()) return (null, 0);
        slot.Take(count, out int rest);   
        return (item,count - rest);
    }
    public bool Swap(int a, int b)
    {
        if (a < 0 || a >= size || b < 0 || b >= size)
        {
            Debug.LogError("越界");
            return false;
        }
        if (a == b)
        {
#if UNITY_EDITOR
            Debug.Log("相同位置放弃交换");
#endif
            return false;
        }

        if (bagItems[a].Equals(bagItems[b]) && bagItems[b].NotFull())
        {
#if UNITY_EDITOR
            Debug.Log("相同内容,目标未满，堆叠");
#endif
            if (bagItems[a].Item==null)return false;
            int rest = bagItems[b].Add(bagItems[a]);
            bagItems[a].Count = rest;
            return true;
        }
        using (new EventLockScope(this))
        {        
            var aSlot = bagItems[a];
            var bSlot = bagItems[b];
            bool aEmpty = aSlot.IsEmpty();
            bool bEmpty = bSlot.IsEmpty();
            if (aEmpty && bEmpty) { return false; }

            // 交换
            (var itemA, var countA) = (bagItems[a].Item, bagItems[a].Count);
            (var itemB, var countB) = (bagItems[b].Item, bagItems[b].Count);
            bagItems[a].Item = itemB;
            bagItems[a].Count = countB;
            bagItems[b].Item = itemA;
            bagItems[b].Count = countA;
            // 维护 emptyPos
            if (aEmpty ^ bEmpty)
            {
                if (aEmpty)
                {
                    emptyPos.Remove(a);
                    emptyPos.Add(b);
                }
                else
                {
                    emptyPos.Remove(b);
                    emptyPos.Add(a);
                }
            }
        }
            TriggerItemSwapped(a, b);
            return true;
           
    }
    public bool Swap(int a, BagSystem other, int b)
    {
        if(other== null || this.bagItems[a].IsEmpty())
        {
            Debug.LogError("对象背包系统为null||使用空物体交换");
            return false;
        }
        if (other == this) 
        {
#if UNITY_EDITOR
            Debug.Log("同一背包，自交换");
#endif
            return Swap(a, b);
        }
        
        if (bagItems[a].Equals(other.bagItems[b]) && other.bagItems[b].NotFull())
        {
#if UNITY_EDITOR
            Debug.Log("不同背包 相同类型,目标未满，进行堆叠");
#endif
            int rest = other.bagItems[b].Add(bagItems[a]);
            bagItems[a].Count = rest;
            return true;
        }
        if (!other.mask.Contains(bagItems[a].Item.ItemInfo.ItemType))
        {
#if UNITY_EDITOR
            Debug.Log("背包过滤掉这层，无法交换");
#endif
            return false;
        }

#if UNITY_EDITOR
        Debug.Log("进行背包间交换");
#endif
        //默认最大堆叠数量不变
        var (item_b,count_b) = other.Take(b);
        var (item_a,count_a) = Take(a);
        if(item_b != null && count_b > 0)
        {
            AddAt(a, item_b,count_b);
        }
        if (item_a != null && count_a > 0)
        {
            other.AddAt(b,item_a,count_a);
        }
        return false;
    }
    public void SortAndMerge()
    {
        using (new EventLockScope(this))
        {
            //空格在后
            bagItems.Sort();
            int write = 0;
            for (int read = 0; read < size; read++)
            {
                var cur = bagItems[read];
                if (cur.IsEmpty()) continue;

                // 第一个有效格子
                if (write == 0)
                {
                    if (write != read)
                    {
                        bagItems[write].Reset(cur);
                        cur.Reset();
                    }
                    write++;
                    continue;
                }

                var last = bagItems[write - 1];
                // 同物品 → 合并
                if (last.Equals(cur))
                {
                    int rest = last.Add(cur);
                    cur.Count = rest;

                    if (cur.Count <= 0)
                        cur.Reset();
                    else
                    {
                        // 还有剩余，占用新格子
                        if (write != read)
                        {
                            bagItems[write].Reset(cur);
                            cur.Reset();
                        }
                        write++;
                    }
                }
                else
                {
                    // 不同物品，直接放到 write
                    if (write != read)
                    {
                        bagItems[write].Reset(cur);
                        cur.Reset();
                    }
                    write++;
                }
            }
            // 清理尾部
            for (int i = write; i < size; i++)
            {
                bagItems[i].Reset();
            }
            InitEmptyPos();
            RefreshIndex();
        }           
            TriggerAllItemsRefreshed();
    }
    public (ItemStat item,int count) Peek(int index)
    {
        return (bagItems[index].Item, bagItems[index].Count);
    }
    public List<(ItemStat item,int count)> SetSize(int size)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        // 扩容
        while (bagItems.Count < size)
        {
            bagItems.Add(new BagCell(OnCellStatChange));
        }
        // 缩容
        List<(ItemStat item, int count)> removedCells = new();
        while (bagItems.Count > size)
        {
            var last = bagItems[^1];
            if (!last.IsEmpty()) removedCells.Add((last.Item,last.Count));
            bagItems.RemoveAt(bagItems.Count - 1);
        }
        this.size = size;
        TriggerSizeChanged(size);
        return removedCells;
    }
    public bool TryTakeAnyStrict(ItemStat item, int count)
    {
        if (item == null || count <= 0) return false;

        int total = 0;
        for (int i = 0; i < size; i++)
        {
            var slot = bagItems[i];
            if (!slot.IsEmpty() && slot.Equals(item))
                total += slot.Count;

            if (total >= count)
                break;
        }
        // 不够直接失败
        if (total < count)
            return false;
        // 开始真正取
        int remaining = count;
        for (int i = 0; i < size && remaining > 0; i++)
        {
            var slot = bagItems[i];
            if (slot.IsEmpty() || !slot.Equals(item)) continue;
            slot.Take(remaining, out remaining);
        }
        return true;
    }
    public bool TryTakeAny(ItemStat item, int count,out int rest)
    {
        rest = count;   
        if (item == null || count <= 0) return false;
        int remaining = count;
        for (int i = 0; i < size && remaining > 0; i++)
        {
            var slot = bagItems[i];
            if (slot.IsEmpty() || !slot.Equals(item)) continue;
            slot.Take(remaining, out remaining);
        }
        rest = remaining;
        return remaining<=0;
    }
    protected int AddAt(int index, ItemStat item,int count)
    {
        if (item == null || count <= 0) return -1;
        if (index < 0 || index >= size) return -1;
        
        var slot = bagItems[index];

        int rest = slot.Add(item, count);
        if (rest > 0)
        {
           return Add(item, rest);
        }
        return 0;
    }
    public void OnCellStatChange(int pos)
    {
        if (lockCellCountChangeEvent) return;
        if (bagItems[pos].IsEmpty())
        {
            emptyPos.Add(pos);
            bagItems[pos].Reset();
        }
        else
        {
            emptyPos.Remove(pos);
        }
        TriggerItemRefreshed(pos);
    }
    public void AddFitter(ItemType type)
    {
        mask.AddLayer(type);
#if UNITY_EDITOR
        Mask = Convert.ToString(mask.Mask, 2);
#endif
    }
    public void RemoveFitter(ItemType type)
    {
        mask.RemoveLayer(type);
#if UNITY_EDITOR
        Mask = Convert.ToString(mask.Mask, 2);
#endif

    }
    public bool MoveAllTo(BagSystem target)
    {
        if (target == null || target == this) return false;

        bool movedAny = false;

        // 锁事件，避免频繁刷新 UI
        using (new EventLockScope(this))
        using (new EventLockScope(target))
        {
            for (int i = 0; i < size; i++)
            {
                var slot = bagItems[i];
                if (slot.IsEmpty()) continue;

                var item = slot.Item;
                int count = slot.Count;

                // 目标背包不接受该类型
                if (!target.mask.Contains(item.ItemInfo.ItemType))
                    continue;

                // 尝试加入目标背包
                int rest = target.Add(item, count);

                if (rest != count)
                {
                    // 有成功移动
                    movedAny = true;

                    if (rest <= 0)
                    {
                        // 全部转移
                        slot.Reset();
                    }
                    else
                    {
                        // 部分转移，剩余留在原背包
                        slot.Count = rest;
                    }
                }
            }

            // 重新维护 emptyPos
            InitEmptyPos();
            target.InitEmptyPos();
        }

        if (movedAny)
        {
            TriggerAllItemsRefreshed();
            target.TriggerAllItemsRefreshed();
        }

        return movedAny;
    }
    public void Split(int pos , int count)
    {
        if (bagItems[pos].IsEmpty()) return;
        if(emptyPos.Count == 0) return;
        var buf = bagItems[pos].Item;
        bagItems[pos].Take(count, out int rest);
        int emptypos = emptyPos.Pop();
        AddAt(emptypos, buf, count - rest);
    }
    public bool Contain(ItemStat item)
    {
        int index = bagItems.FindIndex(x =>
            !x.IsEmpty() &&
            x.Equals(item));
        return index >= 0;
    }

#if UNITY_EDITOR
    public void SwitchLayerMask(int layer)
    {
        if (mask.Contains((ItemType)layer))
        {
            mask.RemoveLayer((ItemType)layer);
        }
        else
        {
            mask.AddLayer((ItemType)layer);
        }
        Mask = Convert.ToString(mask.Mask, 2);
    }
    public void SetSizeTest(int size)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        // 扩容
        while (bagItems.Count < size)
        {
            bagItems.Add(new BagCell(OnCellStatChange));
        }
        // 缩容
        List<BagCell> removedCells = new List<BagCell>();
        while (bagItems.Count > size)
        {
            var last = bagItems[^1];
            removedCells.Add(last);
            bagItems.RemoveAt(bagItems.Count - 1);
        }
        this.size = size;
        TriggerSizeChanged(size);
        return ;
    }
#endif
}