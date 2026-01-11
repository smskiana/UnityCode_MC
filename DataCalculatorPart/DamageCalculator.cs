using Actors;
using Stats;
using System.Collections.Generic;
using UnityEngine;

namespace StatSystems
{
    public class DamageCalculator
    {
        private readonly ActorStat BasicStat;
        private readonly StatSystem system;
        public ActorStat FrameDataStat => BasicStat.CloneNewInstance() as ActorStat;    
        public DamageCalculator(StatSystem system)
        {
            this.system = system;
            BasicStat = new ActorStat();
        }
        public ActorStat SetDamage(DamagePackage package,ActorStat ThisFrameStat, IEnumerable<IModifyDamage> effects)
        {
            var buf = Calculate(package, ThisFrameStat, effects);
            system.ResetToBasicStat(BasicStat);
            BasicStat.currentHp = Mathf.Clamp(BasicStat.currentHp - buf.Damage, 0, BasicStat.hp);
            return BasicStat;
        }
        public static ActorStat PredictDamage(DamagePackage dmg, ActorStat stat, IEnumerable<IModifyDamage> effects)
        {
            var copy = stat.CloneNewInstance() as ActorStat;
            var result = Calculate(dmg, copy, effects);
            copy.currentHp = Mathf.Clamp(copy.currentHp - result.Damage, 0, copy.hp);
            return copy;
        }
        public static DamagePackage Calculate(DamagePackage dmg,ActorStat stat , IEnumerable<IModifyDamage> effects)
        {
            // 2. 遍历效果加成（比如易伤、减伤）
            foreach (var effect in effects)
            {
               dmg=effect.ModifyDamage(dmg);
            }
            dmg = ModifyDamage(dmg, stat);
            return dmg; 
        }
        public static DamagePackage ModifyDamage(DamagePackage dmg, ActorStat stat)
        {
            dmg.SetStat(stat.CloneNewInstance() as ActorStat);
            return dmg;
        }
    }
}
