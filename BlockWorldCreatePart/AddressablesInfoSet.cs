using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[System.Serializable]
public class AddressablesInfoSet<T> where T : Info
{
    [ShowInInspector]
    [ReadOnly]
    private readonly Dictionary<int, T> dic = new();
    private AsyncOperationHandle<IList<T>> handle;
    [ShowInInspector]
    public bool IsLoaded { get => handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded; }
    private Task loadTask;
    public Task LoadAll(string label)
    {
        if (loadTask != null)
            return loadTask;

        loadTask = InternalLoadAll(label);
        return loadTask;
    }
    private async Task InternalLoadAll(string label)
    {
        dic.Clear();
        handle = Addressables.LoadAssetsAsync<T>(label, null);
        IList<T> assets = await handle.Task;

        foreach (var info in assets)
        {
            if (!dic.ContainsKey(info.ID))
                dic.Add(info.ID, info);
            else
                Debug.LogWarning($"重复 Id：{info.ID} in {info.name}");
        }
    }
    /// <summary>
    /// 查找 Info
    /// </summary>
    public bool TryFind(int id, out T info)
    {
        info = default;
        if(!IsLoaded) return false;
        return dic.TryGetValue(id, out info);
    }

    /// <summary>
    /// 卸载所有 Info
    /// </summary>
    public void UnloadAll()
    {
        if (handle.IsValid())
            Addressables.Release(handle);
        dic.Clear();
    }
}