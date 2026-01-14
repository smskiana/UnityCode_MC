using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
namespace _3C.StateMachines
{
    public interface IControl
    {
        public abstract bool IsActive(string name);
    }
    public class StateMachine :MonoBehaviour
    {   
        private string nextState;
        private string pushState;
        private bool needPop;
        [SerializeField]
        private StateMachineInfo info;
        [ShowInInspector]
        private IControl controller;
        [ShowInInspector]
        [SerializeField]
        private string DefaultStateName;
        [SerializeField]
        [ReadOnly]
        private bool switchLocked = false;
        [ShowInInspector]
        [ReadOnly]
        private Dictionary<string, State> Allstate = null;
        [ReadOnly]
        private readonly Stack<State> statesStack = new();
        [ShowInInspector]
        public State Current => statesStack.Count > 0 ? statesStack.Peek() : null;
        public string NextState { get=>nextState; set {
                if (value == null)
                {
                    nextState = null;
                    return;
                }
                if (Current.CanBeNextstate(value))
                 {
                     if (Current.CanBeInterrupt(value))
                     {
                         switchLocked = false ;
                     }
                         nextState = value;
                }    
            } }
        public string PushState { get =>pushState; set {
                if (value == null)
                {
                    pushState = null;
                    return;
                }
                 if (Current.CanBeNextstate(value))
                 {
                     if (Current.CanBeInterrupt(value))
                     {
                         switchLocked = false ;
                     }
                     pushState = value;
                 }
            } }
        public bool NeedPop {  get =>needPop; set {
                if (statesStack.Count > 1)
                {
                    needPop = value;
                    return;
                }
                else needPop = false;
            } }
        public bool SwitchLocked { get => switchLocked;internal set => switchLocked = value; }
        private void Awake()
        {
            controller ??= GetComponent<IControl>();   
          
        }
        private void Start()
        {
            Init();
        }
        private void Update()
        {
            if(Current == null)
            {
                Debug.LogError($"{this}:状态栈为空为null");
                return;
            }
            Current.TryUpdateState();
            Current.Update();
        }
        public void Init()
        {
            Allstate = new Dictionary<string, State>();

            foreach (var (key, value) in info.SerializableDic.Pairs)
            {
                var buffer = Object.Instantiate(value);
                buffer.Init(controller, this);
                Allstate.Add(key, buffer);
            }
           
            if (string.IsNullOrEmpty(DefaultStateName)||!Allstate.ContainsKey(DefaultStateName))
            {
                Debug.LogError($"{this}:异常的{nameof(Init)}，{nameof(DefaultStateName)} 为null");
                return;
            }
            Exit();
            statesStack.Push(Allstate[DefaultStateName]);
            Current.Enter();
        }
        private void Exit()
        {
            foreach (var state in statesStack)
            {
                state.Exit();
            }
            statesStack.Clear();
        }
        internal void StateSwitch(string name)
        {
            if(!Allstate.TryGetValue(name,out var state)) return;
            statesStack.Pop().Exit();
            statesStack.Push(state);
            state.Enter();          
        }
        internal void StatePush(string name)
        {
            if(!Allstate.TryGetValue(name, out var state)) return;
            Current.Pause();
            statesStack.Push(state);
            state.Enter();
        }
        internal void StatePop()
        {       
            if (statesStack.Count > 1)
            {
                statesStack.Pop().Exit();
                Current.Resume();
            }
        }
    }
}
