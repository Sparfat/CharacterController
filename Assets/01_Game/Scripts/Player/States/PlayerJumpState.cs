using MyGame.Player.StateMachine;
using UnityEngine;

namespace MyGame.Player.States
{
    public class PlayerJumpState : PlayerBaseState
    {
        private readonly int JumpHash = Animator.StringToHash("JumpTree");

        public PlayerJumpState(Core.PlayerController context, PlayerStateMachine machine) : base(context, machine)
        {
            Priority = StatePriority.High; // Jump has priority over falling for double jump
        }

        public override void EnterState()
        {
            // Consume 1 jump
            ctx.CurrentJumpCount++;

            // Reset the vertical speed before applying the new force (Crucial for Double Jump not to crash)
            ctx.ForceVelocity.y = 0f;
            ctx.ForceVelocity.y = ctx.JumpForce;

            // Play Jump BlendTree
            ctx.Animator.CrossFade(JumpHash, 0.1f);
        }

        public override void UpdateState()
        {
            // Allow air movement (Air Control)
            HandleAirMovement();

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
