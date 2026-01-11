using System;
using UnityEngine;

namespace Bags
{
    public class CellBack : MonoBehaviour
    {
        public int Index;
        public BagSystemUI UI;
        public BagSystem  BagSystem{ get{ if (!UI) return null;return UI.BagSystem; } }
    }
}
