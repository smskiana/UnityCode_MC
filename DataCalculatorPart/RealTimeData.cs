using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Stats
{
    public readonly struct RealTimeID:IEquatable<RealTimeID>
    {
        public readonly int SourceID;
        public readonly int InstanceIndex;
        private static readonly Dictionary<int, int> GUToGaDic = new();
        public readonly bool IsValid => SourceID != 0;
        public static readonly RealTimeID Invalid = default;
        public RealTimeID(int sourceid)
        {
            if (sourceid != 0)
            {
                SourceID = sourceid;
                if (GUToGaDic.TryGetValue(SourceID, out var id))
                {
                    InstanceIndex = id + 1;
                    GUToGaDic[SourceID] = InstanceIndex;
                }
                else
                {
                    InstanceIndex = 1;
                    GUToGaDic.Add(SourceID, InstanceIndex);
                }
            }
            else
            {
                SourceID = default;
                InstanceIndex = default;
            }
           
        }
        public RealTimeID(RealTimeID realTimeID) : this(realTimeID.SourceID) { }
        public override bool Equals(object other)
        {
            if (other is RealTimeID data)
                return SourceID == data.SourceID && InstanceIndex == data.InstanceIndex;
            return false;
        }
        public override int GetHashCode()
            => HashCode.Combine(SourceID, InstanceIndex);   
        public override string ToString()
            => $"{SourceID}#{InstanceIndex}";
        public static bool operator ==(RealTimeID a, RealTimeID b)
        {
            return a.SourceID == b.SourceID && a.InstanceIndex == b.InstanceIndex;
        }
        public static bool operator !=(RealTimeID a, RealTimeID b)
        {
            return !(a == b);
        }
        public bool Equals(RealTimeID other) =>
                SourceID == other.SourceID && InstanceIndex == other.InstanceIndex;


    }
    [System.Serializable]
    public class RealTimeData :IComparable<RealTimeData>, IEquatable<RealTimeData>
    {
        [SerializeField]protected Info info;
        [SerializeField]private RealTimeID id;
        [SerializeField]private string name;    
        public RealTimeData(Info info)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            this.info = info;
            id = new RealTimeID(info.ID);
            Reset();
        }
        public RealTimeData(RealTimeData data)
        {
            this.info = data.info;
            id = new RealTimeID(data.id.SourceID);
            Reset();
        }
        protected RealTimeData()
        {

        }
        public virtual RealTimeData CloneNewInstance() => null;
        public override string ToString()
        {
            return  $"{base.ToString()} ({nameof(id)}: {id})";
        }
        public int CompareTo(RealTimeData other)
        {
            if(other == null) return 1;
            return this.info.CompareTo(other.info);
        }

        [Button("重置")]
        public virtual void Reset()
        {
            if(info ==null)
            {
                Debug.LogError($"错误的状态重置：{this} 的{nameof(info)}为 null");
                return;
            }
            this.name = info.name;
        }
        public bool Equals(RealTimeData other)
        {
            if(other==null) return false;
           return id.Equals(other.id);
        }
    }
}


