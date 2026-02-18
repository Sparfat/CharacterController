using MyGame.Player.StateMachine;
using MyGame.Player.Core;
using UnityEngine;

namespace MyGame.Player.StateMachine
{
    public abstract class PlayerBaseState
    {
        protected PlayerController ctx;
        protected PlayerStateMachine stateMachine;

        public StatePriority Priority { get; protected set; }

        public PlayerBaseState(PlayerController context, PlayerStateMachine machine)
        {
            ctx = context;
            stateMachine = machine;
        }

        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void ExitState();

        public virtual bool CanBeInterruptedBy(StatePriority nextPriority)
        {
            return (int)nextPriority >= (int)Priority;
        }
    }
}