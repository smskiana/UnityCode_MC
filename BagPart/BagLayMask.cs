using Store;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bags
{
    [System.Serializable]
    public class BagLayMask : IEquatable<BagLayMask>
    {
        [SerializeField]private int layermask;
        public int Mask => layermask;

        private static BagLayMask allinstance;
        public static BagLayMask AllContainMask { get 
            { 
                if (allinstance != null)
                {
                    return allinstance.Copy();
                }
                else
                {
                    allinstance = new BagLayMask(-1);
                    return allinstance.Copy();
                }
            } }
        public BagLayMask() { }
        private BagLayMask(int mask) { this.layermask = mask; }
        public bool Contains(BagLayMask layMask)
        {
            return (layermask & layMask.Mask) == layMask.Mask;
        }
        public bool Contains(ItemType layer)
        {
            return (layermask & (1 << (int)layer)) != 0;
        }
        public void AddMask(BagLayMask layMask)
        {
            layermask |= layMask.Mask;
        }
        public void RemoveMask(BagLayMask layMask)
        {
            layermask &= ~layMask.Mask;
        }
        public void AddLayer(ItemType layer)
        {
            layermask |= 1 << (int)layer;
        }
        public void RemoveLayer(ItemType layer)
        {
            layermask &= ~(1 << (int)layer);
        }
        public BagLayMask Copy()
        {
            return new BagLayMask(layermask);
        }
        public List<ItemType> GetAllLayers()
        {
            var layers = new List<ItemType>();
            foreach (ItemType layer in Enum.GetValues(typeof(ItemType)))
            {
                int mask = 1 << (int)layer;
                if ((layermask & mask) != 0)
                {
                    layers.Add(layer);
                }
            }
            return layers;
        }
        public bool Equals(BagLayMask other)
        {
           return layermask == other.layermask;
        }
    }
}
