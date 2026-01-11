using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Store
{
    public class InfoStorer :MonoBehaviour
    {
        public static InfoStorer Instance;
        [SerializeField]private AddressablesInfoSet<BlocksInfo> blockInfos = new();
        public Task BlockLoadTask { get; private set; }
        public bool IsBlockLoaded {  get => BlockInfos.IsLoaded; }
        public AddressablesInfoSet<BlocksInfo> BlockInfos { get => blockInfos;}

        public async void Awake()
        {
            Instance = this;
            BlockLoadTask = BlockInfos.LoadAll("BlockInfo");
            await BlockLoadTask;
            BlockLoadTask = null;
        }
        public bool TryFindBlock(int key,out BlocksInfo info)
        {
            return BlockInfos.TryFind(key,out info);
        }
     
     
    }
}
