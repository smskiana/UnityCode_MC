using Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bags
{  
    public class HotBar : MonoBehaviour
    {
        [System.Serializable]
        private class HotBarCell
        {
            [SerializeField]private ItemStat item;
            [SerializeField]private bool active = false;
            public bool Active { get => active; set {
                    if(active==value) return;
                    active = value;
                    ActiveSwtich?.Invoke(active);
                } }
            public ItemStat Item { get => item; set => item = value; }

            private event Action<bool> ActiveSwtich;
            public void RegisterActiveSwtich(Action<bool> action)
            {
                ActiveSwtich += action;
            }
            public void UnregisterActiveSwtich(Action<bool> action)
            {
                ActiveSwtich -= action;
            }
        }
        [SerializeField] private BagSystem bagSystem;
        [SerializeField] private List<HotBarCell> HotBars;
        [SerializeField] private int pos = -1;
       
        public bool TryUse(int pos)
        {         
            if (pos < 0 || pos >= HotBars.Count)
            {
                return false;
            }
            if (HotBars[pos].Active)
            {
                HotBars[pos].Active = bagSystem.TryTakeAny(HotBars[pos].Item, 1, out var _);
                return HotBars[pos].Active;
            }
            return false;
        }
        public bool TryPeek(int pos ,out ItemStat item)
        {
            item = null;
            if(pos< 0 || pos >= HotBars.Count)
            {
                return false;
            }
            if (HotBars[pos].Active)
            {
                item = HotBars[pos].Item;
                return true;
            }
            return false;
        }
        public void Blind(int pos,ItemStat item)
        {
            if ((pos < 0 && pos >= HotBars.Count)||item==null)
            {
                return;
            }
            HotBars[pos].Item = item;
            HotBars[pos].Active = true;
        }
        public void Blind(int pos, BagSystem bagSystem,int posInBag)
        {
            if ((pos < 0 && pos >= HotBars.Count)||bagSystem!=this.bagSystem)
            {
                return;
            }
            HotBars[pos].Item = bagSystem.Peek(pos).item;
            HotBars[pos].Active = true;
        }
        public void AddCellListener(Action<bool> action,int pos)
        {
            HotBars[pos].RegisterActiveSwtich(action);
        }
    }
}
