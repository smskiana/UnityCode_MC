using Stats;

namespace StatSystems
{
    public class DamagePackage
    {
        public int Id;
        private float damage;
        public DamagePackage(float damage)
        {
            this.damage = damage;
        }
        public IDamageSource Source {  get; set; }
        public IDamageTarget Target {  get; set; }
        public float Damage { get => damage;}
        
        public virtual void SetStat(ActorStat stat)
        {
            damage -=stat.defence;
            damage = damage==0?1:damage;
        }
    }
}
