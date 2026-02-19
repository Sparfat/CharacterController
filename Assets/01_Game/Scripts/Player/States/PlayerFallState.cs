using MyGame.Player.StateMachine;
using UnityEngine;

namespace MyGame.Player.States
{
    public class PlayerFallState : PlayerBaseState
    {
        private readonly int FallShortHash = Animator.StringToHash("Fall_Short");
        private readonly int FallLargeHash = Animator.StringToHash("Fall_Large");
        private readonly int LandAnticipationHash = Animator.StringToHash("Land_Anticipation");

        private float _peakY; // Tracks maximum height reached
        private bool _isHardFall = false;
        private bool _isAnticipating = false;

        public PlayerFallState(Core.PlayerController context, PlayerStateMachine machine) : base (context, machine)
        {
            Priority = StatePriority.Medium;
        }

        public override void EnterState()
        {
            _peakY = ctx.transform.position.y;
            ctx.Animator.CrossFade(FallShortHash, 0.2f);

        }

        public override void ExitState()
        {

        }

        public override void UpdateState()
        {
            // 1. Rastreia a distância da queda
            if (ctx.transform.position.y > _peakY) _peakY = ctx.transform.position.y;
            float fallDistance = _peakY - ctx.transform.position.y;

            // 2. Troca para Fall Large se passar do limite parametrizável
            if (fallDistance > ctx.HardFallDistance && !_isHardFall)
            {
                _isHardFall = true;
                ctx.Animator.CrossFade(FallLargeHash, 0.2f);
            }

            // 3. Raycast de Antecipação de Aterrissagem (Sensação AAA)
            if (!_isAnticipating && Physics.Raycast(ctx.transform.position, Vector3.down, out RaycastHit hit, ctx.LandingAnticipationDistance, ctx.GroundLayer))
            {
                _isAnticipating = true;
                // Toca a animação com os pés se preparando para tocar o chão
                ctx.Animator.CrossFade(LandAnticipationHash, 0.1f);
            }

            if (ctx.ConsumeJumpInput() && ctx.CurrentJumpCount < ctx.MaxJumps)
            {
                stateMachine.ChangeState(new PlayerJumpState(ctx, stateMachine));
                return; 
            }

            // 4. Tocou no chão de verdade?
            if (ctx.CharController.isGrounded && ctx.ForceVelocity.y < 0.1f)
            {
                // Passamos para o estado de Grounded, informando se foi uma queda dura
                stateMachine.ChangeState(new PlayerGroundedState(ctx, stateMachine, _isHardFall));
            }
        }
        public override void FixedUpdateState()
        {
            // Same jump phisics logic
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            cameraForward.y = 0; cameraRight.y = 0;
            cameraForward.Normalize(); cameraRight.Normalize();

            Vector3 targetDirection = (cameraForward * ctx.MoveInput.y + cameraRight * ctx.MoveInput.x).normalized;

            Vector3 movement = targetDirection * ctx.WalkSpeed * ctx.AirControl; // Use WalkSpeed to fall slowly
            movement.y = ctx.ForceVelocity.y;

            ctx.CharController.Move(movement * Time.fixedDeltaTime);
        }

        private void TryDoubleJump()
        {
            if (ctx.CurrentJumpCount < ctx.MaxJumps)
            {
                stateMachine.ChangeState(new PlayerJumpState(ctx, stateMachine));
            }
        }


    }

}

