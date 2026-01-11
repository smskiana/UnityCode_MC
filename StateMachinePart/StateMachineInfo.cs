using Sirenix.OdinInspector;
using UnityEngine;

namespace _3C.StateMachines
{
    [CreateAssetMenu(fileName = "StateMachine", menuName = "State/StateMachine")]
    [System.Serializable]
    public class StateMachineInfo:Info
    {
        public SerializableDic<string,State> SerializableDic = new();
        [Button("添加")]
        public void Add(State state)
        {
            SerializableDic.Add(state.ID, state);
        }
        [Button("移除")]
        public void Remove(State state)
        {
            SerializableDic.Remove(state.ID);
        }
    }
}
