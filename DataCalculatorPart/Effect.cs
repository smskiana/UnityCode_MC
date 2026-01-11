using Sirenix.OdinInspector;
using Stats;
using StatSystems;
using System;
using UnityEngine;


namespace Effects
{
    public enum EffectState
    {
        New,
        Ready,
        Active,
        Pause,
        Over,
    }
    [System.Serializable]
    public struct EffectContext
    {
        public static EffectContext zero = new();
        public readonly bool IsValid => Source != null && Target != null;
        public IEffectSource Source;
        public IEffectTarget Target;
    }
    [System.Serializable]
    public class Effect :ScriptableObject,ITick
    {
        public EffectContext context;
        public EffectState State;
        [SerializeField]private int id = 0;
        [ShowInInspector]
        public int Id { get { 
            if(id == 0) return GetInstanceID();
            else return id;
            } }
        public void SetInfo(int id,IEffectSource orgin =null)
        {
            this.id = id;
            context = new EffectContext()
            {
                Source = orgin,
                Target = null
            };
        }
        public virtual void Add(IEffectTarget target)
        {
            context = new()
            {
                Source = context.Source,
                Target = target,
            };
        }
        public virtual void RepeatAdd(Effect effect)
        {
            //TODO 处理重复添加逻辑
        }
        public virtual void Remove()
        {
            //异常逻辑
        }
        public override bool Equals(object obj)
        {
            return obj is Effect e && Id.Equals(e.Id);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"{Id} [{State}] Source:{context.Source}";
        }
        public void Clear()
        {      
            context= EffectContext.zero;
        }
        public virtual void Tick()
        {
            
        }
    }
}
