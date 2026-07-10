namespace SmilingEclipse.STMImporter
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class StateMachine<T>
    {
        private T owner;
        private IState<T> currentState;
        private Dictionary<Type, IState<T>> states = new();

        public IState<T> CurrentState => currentState;

        public StateMachine(T owner)
        {
            this.owner = owner;
        }

        public void AddState(IState<T> state)
        {
            states[state.GetType()] = state;
        }

        public void ChangeState<U>() where U : IState<T>
        {
            if (currentState != null)
                currentState.Exit(owner);

            if (states.TryGetValue(typeof(U), out var newState))
            {
                if (currentState == newState) { return; }
                currentState = newState;
                currentState.Enter(owner);
            }
        }

        public void Update()
        {
            currentState?.Update(owner);
        }
    }

}