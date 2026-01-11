using Actors;
using Effects;
using Stats;
using System.Collections;
using System.Collections.Generic;

namespace StatSystems
{
    public class StatCalculator
    {
        public StatSystem StatSystem {  get;private set; }
        public StatCalculator(StatSystem statSystem)
        {
            this.StatSystem = statSystem;
        }

        protected  ActorStat FrameDataStat = new();
        
        public ActorStat Calculator(IEnumerable<IEffectApply> effects)
        {
            StatSystem.ResetToBasicStat(FrameDataStat);
            foreach (IEffectApply effect in effects)
            {
                effect.Apply(FrameDataStat);
            }
            return FrameDataStat;
        }
    }
}
