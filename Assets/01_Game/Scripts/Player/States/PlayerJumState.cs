using MyGame.Player.StateMachine;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MyGame.Player.States
{
    public class PlayerJumpState : PlayerBaseState
    {
        private readonly int JumpHash = Animator.StringToHash("JumpStart");

        public PlayerJumpState(Core.PlayerController context, PlayerStateMachine machine) : base(context, machine)
        {
            Priority = StatePriority.Medium; // Jump has priority over locomotion
        }

        public override void EnterState()
        {
            // 1. Apply phisical force
            ctx.ForceVelocity.y = ctx.JumpForce;

            // 2. Play animation
            ctx.Animator.CrossFade(JumpHash, 0.1f);
        }

        public override void UpdateState()
        {
            // Allow air movement (Air Control)
            HandelAirMovement();

            // Change to fall
            if(ctx.ForceVelocity.y <= 0)
            {
                stateMachine.ChangeState(new PlayerFallState(ctx, stateMachine));
            }
        }

        public override void FixedUpdateState()
        {
            // Apply movement ( gravity + air control)
            ApplyAirPhisics();
        }

        public override void ExitState() { }

        private void HandleAirMovement()
        {
            // Logic for rotation in air
            if(ctx.MoveInput.sqrMagnitude > 0.1f)
            {
                Vector3 cameraFoward = Camera.main.transform.forward;
                Vector3 cameraRight = Camera.main.transform.right;

                cameraFoward.y = 0;
                cameraRight.y = 0;

                cameraFoward.Normalize();
                cameraRight.Normalize();

                // Direction is based on Input
                Vector3 targetDirection = (cameraFoward * ctx.MoveInput.y + cameraRight * ctx.MoveInput.x).normalized;

                if (targetDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    // Slerp to smooth rotation
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, targetRotation, 10f * Time.deltaTime);
                }
            }
        }

        private void ApplyAirPhisics()
        {
            // Calculates camera-based steering like Grounded, but multiplied by AirControl
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            cameraForward.y = 0; cameraRight.y = 0;
            cameraForward.Normalize(); cameraRight.Normalize();

            Vector3 targetDirection = (cameraForward * ctx.MoveInput.y + cameraRight * ctx.MoveInput.x).normalized;

            // Move using running or walking speed * AirControl
            Vector3 movement = targetDirection * ctx.RunSpeed * ctx.AirControl;
            movement.y = ctx.ForceVelocity.y; // Maintain gravity/jump

            ctx.CharController.Move(movement * Time.fixedDeltaTime);
        }

    }

}
