using UnityEngine;

namespace MyGame.Player.StateMachine
{
    public class PlayerStateMachine
    {
        public PlayerBaseState CurrentState { get; private set; }

        public void Initialize(PlayerBaseState initialState)
        {
            CurrentState = initialState;
            CurrentState.EnterState();
        }

        public void ChangeState(PlayerBaseState newState)
        {
            if (CurrentState.CanBeInterruptedBy(newState.Priority))
            {
                CurrentState.ExitState();
                CurrentState = newState;
                CurrentState.EnterState();
            }
        }
    }
}