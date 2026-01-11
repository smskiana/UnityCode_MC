using Actors;
using Effects;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace StatSystems
{
    [System.Serializable]
    public class EffectSystem:ITick
    {
        protected StatSystem system;
        public EffectSystem(StatSystem system)
        {
            this.system = system;
        }
        [ShowInInspector]
        private readonly Dictionary<int, Effect> effects = new();
        private readonly List<Dictionary<int, IEffectApply>> applys = new();
        private readonly List<float> applyOrderList = new();
        private readonly List<Dictionary<int, IModifyDamage>> modifys = new();
        private readonly List<float> modifyOrderList = new();
        private readonly List<Effect> pendingAdd = new();
        private readonly List<int> pendingRemove = new();
        public IEnumerable<IEffectApply> Applylist
        {
            get
            {
                foreach (var dict in applys)
                {
                    foreach (var v in dict.Values)
                    {
                        yield return v;
                    }
                }
            }
        }
        public IEnumerable<IModifyDamage> ModifyDamageList
        {
            get
            {
                foreach (var dict in modifys)
                {
                    foreach (var v in dict.Values)
                    {
                        yield return v;
                    }
                }
            }
        }
        public void Tick()
        {
            foreach(var effect in pendingAdd)
            {
                Add(effect);
            }
            foreach (var (key,value) in effects)
            {
                if (value.State==EffectState.Over)
                {
                    pendingRemove.Add(key);
                    continue;
                }
                if(value.State==EffectState.Active)
                    value.Tick();
            }
            foreach (var key in pendingRemove)
            {
                Remove(key);
            }
            pendingAdd.Clear();
            pendingRemove.Clear();       
        }
        public void Register(Effect effect)
        {
            if (effect.State != EffectState.Ready) return;
            pendingAdd.Add(effect);
        }
        public void Unregist(int id)
        {
            if (effects.TryGetValue(id, out var e))
            {
                e.State = EffectState.Over;
            }
        }
        private void Add(Effect effect)
        {
            int id = effect.Id;
            if (effects.TryGetValue(id, out var e))
            {
                e.RepeatAdd(effect);
            }
            else
            {
                effect.State = EffectState.Active;
                effect.Add(this.system);
                effects.Add(id, effect);
                ApplyTest(effect, id);
                ModifyTest(effect, id);
            }      
        }
        private void Remove(int id)
        {
            if (effects.TryGetValue(id, out var e))
            {
                effects.Remove(id);
                if (e is IEffectApply)
                {
                    for (int i = 0; i < applys.Count; i++)
                    {
                        if (applys[i].Remove(id))
                        {
                            if(applys[i].Count == 0)
                            {
                                applys.RemoveAt(i);
                                applyOrderList.RemoveAt(i);
                            }
                                
                            break;
                        }
                    }
                }                 
                if (e is IModifyDamage)
                    for (int i = 0;i < modifys.Count; i++)
                    {
                        if (modifys[i].Remove(id))
                        {
                            if(modifys[i].Count == 0)
                            {
                                modifys.RemoveAt(i);
                                modifyOrderList.RemoveAt(i);   
                            }
                            break;
                        }
                    }
                e.Remove();
            }         
        }
        private void ApplyTest(Effect effect, int id)
        {
            if (effect is IEffectApply a)
            {
                float order = a.GetApplyOrder();
                if (applys.Count == 0)
                {
                    Dictionary<int, IEffectApply> pairs = new()
                        {
                            { id, a }
                        };
                    applyOrderList.Add(order);
                    applys.Add(pairs);
                    return;
                }
                int i = 0;
                for (; i < applys.Count; i++)
                {
                    if (applyOrderList[i] >= order)
                        break;
                }

                if (i < applys.Count && applyOrderList[i] == order)
                {
                    applys[i].Add(id, a);
                }
                else
                {
                    applyOrderList.Insert(i, order);
                    applys.Insert(i, new Dictionary<int, IEffectApply> { { id, a } });
                }
            }

        }
        private void ModifyTest(Effect effect, int id)
        {
            if (effect is IModifyDamage a)
            {

                float order = a.GetModifyOrder();
                if (modifys.Count == 0)
                {
                    Dictionary<int, IModifyDamage> pairs = new()
                        {
                            { id, a }
                        };
                    modifyOrderList.Add(order);
                    modifys.Add(pairs);
                    return;
                }
                int i = 0;
                for (; i < modifys.Count; i++)
                {
                    if (modifyOrderList[i] >= order) break;
                }
                if (i < modifys.Count && modifyOrderList[i] == order)
                {
                    modifys[i].Add(id, a);
                }
                else
                {
                    modifyOrderList.Insert(i, order);
                    modifys.Insert(i, new Dictionary<int, IModifyDamage> { { id, a } });
                }
            }

        }

    }
}
