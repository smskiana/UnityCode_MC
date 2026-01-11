using CharacterInfo = Store.CharacterInfo;

namespace Stats
{
    [System.Serializable]
    public class ActorStat : RealTimeData
    {
        public float moveSpeed = 2f;
        public float hp;
        public float defence;
        public float atackForce;
        public float breakForce;
        public float atackspeed;
        public float currentHp;
        public CharacterInfo CharacterInfo { get => info as CharacterInfo; }
        public ActorStat() { }
        public ActorStat(CharacterInfo info) : base(info)
        {
           
        }
        protected ActorStat(ActorStat stat) : base(stat) 
        {
            moveSpeed = stat.moveSpeed;
            hp = stat.hp;
            defence = stat.defence;
            atackForce = stat.atackForce;
            breakForce = stat.breakForce;
            currentHp = stat.currentHp;
        }
        public override RealTimeData CloneNewInstance()
        {
           return new ActorStat(this);
        }
        public override void Reset()
        {
            base.Reset();
            var buf = CharacterInfo;
            moveSpeed = buf.MoveSpeed;
            hp = buf.Hp;
            defence = buf.Defence;
            atackForce = buf.AtackForce;
            breakForce = buf.BreakForce;
            currentHp = hp;
            atackspeed = buf.Atackspeed;
        }
        public virtual void Reset(ActorStat stat)
        {
           
            moveSpeed = stat.moveSpeed;
            hp = stat.hp;
            defence = stat.defence;
            atackForce = stat.atackForce;
            breakForce = stat.breakForce;
            currentHp = stat.currentHp;
            atackspeed = stat.atackspeed;
        }
    }
}
