using _3C.Actors;
using Effects;
using Sirenix.OdinInspector;
using Stats;
using StatSystems;
using System;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Actors { 
    public class StatSystem : MonoBehaviour,IDamageSource,IDamageTarget,IEffectTarget 
    { 
        [SerializeField] private Actor actor; 
        [SerializeField] private Store.CharacterInfo characterInfo;
        [ShowInInspector] private StatContainer container; 
        [ShowInInspector] private EffectSystem effectSystem;
        private StatCalculator calculator;
        private DamageCalculator damageCalculator;
        public event Action<StatSystem, float, float> MaxHpChange; 
        public event Action<StatSystem, float, float> CurHpChange;
        public event Action<StatSystem, Effect> EffectExcuteInMe; 
        public event Action<StatSystem> CurEqZero; 
        public float MoveSpeed { get=>container.ThisFrameStat.moveSpeed;}
        public void Awake() 
        { 
            container = new(characterInfo, this); 
            effectSystem = new EffectSystem(this); 
            calculator = new StatCalculator(this); 
            damageCalculator = new DamageCalculator(this);
        }
        public ActorStat  Stat { get => container.ThisFrameStat.CloneNewInstance() as ActorStat; }
        public void TriggerMaxHpChange(float oldvalue, float newValue) => MaxHpChange?.Invoke(this, oldvalue, newValue); 
        public void TriggerEffectExcuteInMe(Effect effect) => EffectExcuteInMe?.Invoke(this, effect); 
        public void TriggerCurHpChange(float oldvalue, float newValue) => CurHpChange?.Invoke(this, oldvalue, newValue); 
        public void ResetToBasicStat(ActorStat stat)
        {
            stat?.Reset(container.Stat);
        }
        public void LateUpdate()
        { 
            effectSystem.Tick();
            var buf = calculator.Calculator(effectSystem.Applylist);
            container.SetChange(buf);
        }
        public DamagePackage GetDamage()
        {
            var damage = container.GetDamage();      
            damage.Source = this;
            return DamageCalculator.Calculate(damage, container.ThisFrameStat, effectSystem.ModifyDamageList);
        }
        public void SetDamage(DamagePackage damage)
        {
            var buf = damageCalculator.SetDamage(damage, container.ThisFrameStat, effectSystem.ModifyDamageList);
            container.SetBasicChange(buf);
        }
        [Button("普通攻击")]
        public void GetDamage(float value)
        {
            DamagePackage damage = new(value);
            SetDamage(damage);
        }
        public void AddEffect(Effect effect) => effectSystem.Register(effect);
        [Button("移除效果")]
        public void RemoveEffect(Effect effect)=>effectSystem.Unregist(effect.Id);
        [Button("移除效果（ID）")]
        public void RemoveEffect(int effectId)=>effectSystem.Unregist(effectId);

#if UNITY_EDITOR
        /// <summary>
        /// 测试用
        /// </summary>
        [Button("添加效果")]
        public void SetEffectClone(Effect effect)
        {
            var buf = Object.Instantiate(effect);
            effectSystem.Register(buf);
        }
#endif

    } 
}