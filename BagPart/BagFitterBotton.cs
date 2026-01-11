using MyGUI;
using Store;

namespace Assets.Scripts.Bags
{
    public class BagFitterBotton :SwtichBotton
    {
        public ItemType type;
        public BagSystemUI BagSystemUI;
        public BagSystem BagSystem{ get {
                if( BagSystemUI!=null) return BagSystemUI.BagSystem;
                else return null;
            } }
        protected override void Enable()
        {
            if(BagSystem!=null) BagSystem.AddFitter(type);
            base.Enable();
        }
        protected override void Disable()
        {
            if (BagSystem != null) BagSystem.RemoveFitter(type);
            base.Disable();
        }
    }
}
