using MyGame.Player.StateMachine;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MyGame.Player.States
{
    public class PlayerGroundedState : PlayerBaseState
    {
        // Hashes for Optmization
        private readonly int SpeedHash = Animator.StringToHash("Speed");
        private readonly int LocomotionHash = Animator.StringToHash("Locomotion");

        // Input Config
        private const float MoveThreshold = 0.1f; // Deadzone
        private const float RunThreshold = 0.75f; // Walk vs Run
        private const float AnimationDampTime = 0.1f;

        // Idle logic
        private float _idleTimer;
        private const float TimeUntilVariant = 7.0f;
        private readonly List<int> _idleVariantHases; // Animations hash list

        public PlayerGroundedState(Core.PlayerController context, PlayerStateMachine stateMachine) : base(context, stateMachine)
        {
            Priority = StatePriority.Low;

            // Start variant list
            _idleVariantHases = new List<int>
            {
                Animator.StringToHash("Idle_Look"),
                Animator.StringToHash("Idle_Look_1"),
                Animator.StringToHash("Idle_Look_2"),
                Animator.StringToHash("Idle_Kick"),
            };
        }

        public override void EnterState()
        {
            // BlendTree Locomotion
            ctx.Animator.CrossFade("Locomotion", 0.05f);
            _idleTimer = 0f;
        }

        public override void UpdateState()
        {
            // 1. Check Inputs and define movement
            CheckMovementLogic();

            // 2. Check if needs to play Idle Variant
            CheckIdleVariantLogic();

            // 3. Check Transitions (Ex: Jump, Attack)
            CheckTransitions();
        }

        public override void FixedUpdateState()
        {
            // Apply gravity and movement
            ApplyMovement();
        }

        public override void ExitState()
        {
            // Reset flags
        }

        // --- Movement Logic ---
        private void CheckMovementLogic()
        {
            // 1. Input Detected?
            if (ctx.MoveInput.sqrMagnitude > MoveThreshold * MoveThreshold)
            {
                _idleTimer = 0f;

                // --- fix interrupt ---
                // Check animator state
                var stateInfo = ctx.Animator.GetCurrentAnimatorStateInfo(0);
                bool isLocomotion = stateInfo.shortNameHash == LocomotionHash;

                if (!isLocomotion && !ctx.Animator.IsInTransition(0))
                {
                    // Fest CrrosFade to cut idle animation
                    ctx.Animator.CrossFade(LocomotionHash, 0.05f);
                }

                // Normal velocity logic
                float inputMagnitude = ctx.MoveInput.magnitude;
                float targetSpeed = (inputMagnitude > RunThreshold) ? ctx.RunSpeed : ctx.WalkSpeed;

                // Defines animator value
                float animatorTargetValue = (inputMagnitude > RunThreshold) ? 1f : 0.5f;

                ctx.MoveSpeed = targetSpeed;

                // Update Animator
                ctx.Animator.SetFloat(SpeedHash, animatorTargetValue, AnimationDampTime, Time.deltaTime);

                // Rotate with camera
                RotateTowardsCamera();
            }
            else
            {
                // No Input
                ctx.MoveSpeed = 0f;

                // Animator value fix
                float currentSpeedParam = ctx.Animator.GetFloat(SpeedHash);

                // if near zero, clamp zero
                if (currentSpeedParam < 0.01f)
                {
                    ctx.Animator.SetFloat(SpeedHash, 0f);
                }
                else
                {
                    ctx.Animator.SetFloat(SpeedHash, 0f, AnimationDampTime, Time.deltaTime);
                }
            }
        }

        // --- Idle Variants Logic ---
        private void CheckIdleVariantLogic()
        {
            // Just runs if !input
            if (ctx.MoveInput.sqrMagnitude > 0.01f) return;

            // --- Timer Fix ---
            var stateInfo = ctx.Animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.shortNameHash == LocomotionHash)
            {
                // Idle
                _idleTimer += Time.deltaTime;

                if (_idleTimer >= TimeUntilVariant)
                {
                    PlayRandomIdle();
                    _idleTimer = 0f;
                }
            }
            else
            {
                // Estamos tocando uma variante (Idle_Look, etc.)
                // NÃO incrementa o timer. 
                // Assim, quando a animação acabar e voltar pro Locomotion, o timer começa do zero.
                _idleTimer = 0f;
            }
        }

        private void PlayRandomIdle()
        {
            if (_idleVariantHases.Count == 0) return;

            int randomIndex = Random.Range(0, _idleVariantHases.Count);
            // CrossFade to animation
            ctx.Animator.CrossFade(_idleVariantHases[randomIndex], 0.05f);
        }

        // --- Rotation / Movement / Transitions ---
        private void RotateTowardsCamera()
        {
            Vector3 cameraFoward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;

            cameraFoward.y = 0;
            cameraRight.y = 0;

            cameraFoward.Normalize();
            cameraRight.Normalize();

            // Direction is based on Input
            Vector3 targetDirection = (cameraFoward * ctx.MoveInput.y + cameraRight * ctx.MoveInput.x).normalized;

            if(targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                // Slerp to smooth rotation
                ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        private void ApplyMovement()
        {
            // Move to target look direction
            Vector3 movement = ctx.transform.forward * ctx.MoveSpeed;

            // Gravity
            movement.y = ctx.ForceVelocity.y;

            ctx.CharController.Move(movement * Time.fixedDeltaTime);
        }

        private void CheckTransitions()
        {
            // Change States
            // if (ctx.IsJumpPressed) stateMachine.ChangeState(new PlayerJumpState(ctx, stateMachine));
        }

    }

}
