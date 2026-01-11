using Actors;
using Sirenix.OdinInspector;
using Stats;
using Store;

namespace StatSystems
{
    [System.Serializable]
    public class StatContainer:IDamageSource
    {
        public StatSystem StatSystem {  get;private set; }
        public StatContainer(CharacterInfo charactorInfo,StatSystem statSystem)
        {
            ThisFrameStat = new ActorStat(charactorInfo);
            Stat = new ActorStat(charactorInfo);
            LastFrameStat = new ();
            StatSystem = statSystem;
        }
        public StatContainer(StatSystem statSystem, ActorStat stat)
        {
            StatSystem = statSystem;
            ThisFrameStat = stat;
            LastFrameStat = new();   
            Stat = stat.CloneNewInstance() as ActorStat;
            
        }
        [ShowInInspector]
        public ActorStat ThisFrameStat {  get; internal set; }
        [ShowInInspector]
        public ActorStat LastFrameStat {  get; internal set; }
        [ShowInInspector]
        public ActorStat Stat {  get; internal set; }
        public void Reset()
        {
            ThisFrameStat.Reset();
            Stat.Reset();
        }
        public virtual void SetChange(ActorStat stats)
        {
            LastFrameStat.Reset(ThisFrameStat);
            ThisFrameStat.Reset(stats);
            if( LastFrameStat.hp != ThisFrameStat.hp)
            {
                StatSystem.TriggerMaxHpChange( LastFrameStat.hp, ThisFrameStat.hp);
            }
            if( LastFrameStat.currentHp != ThisFrameStat.currentHp)
            {
                StatSystem.TriggerCurHpChange( LastFrameStat.currentHp, ThisFrameStat.currentHp);
            }
          
        }
        public virtual void SetBasicChange(ActorStat stat)
        {
            Stat.Reset(stat);
        }
        public DamagePackage GetDamage()
        {
            return new DamagePackage(ThisFrameStat.atackForce) {Source = this };
        }
    }
}

