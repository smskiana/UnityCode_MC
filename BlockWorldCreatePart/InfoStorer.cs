using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Store
{
    public class InfoStorer :MonoBehaviour
    {
        public static InfoStorer Instance;
        [SerializeField]private AddressablesInfoSet<BlocksInfo> blockInfos = new();
        private Task blockLoadTask;
        public bool IsBlockLoaded {  get => BlockInfos.IsLoaded; }
        public AddressablesInfoSet<BlocksInfo> BlockInfos { get => blockInfos;}

        public void Awake()
        {
            if (Instance)
            {
                Destroy(this.gameObject);
                return;
            }
                Instance = this;
           _=InitializeAsync();   
        }
        private async Task InitializeAsync()
        {
            try
            {
                await Initialize();
                blockLoadTask = null;
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
        public Task Initialize()
        {
            if (IsBlockLoaded) return Task.CompletedTask;
            if (blockLoadTask != null)
                return blockLoadTask;
            blockLoadTask = BlockInfos.LoadAll("BlockInfo");
            return blockLoadTask;
        }
        public bool TryFindBlock(int key,out BlocksInfo info)
        {
            return BlockInfos.TryFind(key,out info);
        }
     
     
    }
}
