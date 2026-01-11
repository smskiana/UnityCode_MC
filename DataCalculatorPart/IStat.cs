using Effects;
using Stats;

namespace StatSystems
{
    public interface IEffectSource
    {
        public abstract Effect GetEffects();
        public abstract void TriggerMyEffectExcute(Effect effect);
    }
    public interface IEffectTarget
    {
        public abstract void TriggerEffectExcuteInMe(Effect effect);
        public abstract void AddEffect(Effect effect);
        public abstract void RemoveEffect(int effectId);
    }
    public interface IDamageSource 
    {
        public abstract DamagePackage GetDamage();  
    
    }
    public interface IDamageTarget
    {
        public abstract void SetDamage(DamagePackage damage);    
    }
    public interface IEffectApply
    {
        public abstract void Apply(ActorStat stat);
        public float GetApplyOrder() => 0;
    }
    public interface ITick
    {
        public abstract void Tick();
    }
    public interface IModifyDamage 
    {
        public abstract DamagePackage ModifyDamage(DamagePackage damage);
        public virtual float GetModifyOrder() => 0;
    }
}
