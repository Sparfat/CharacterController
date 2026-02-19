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

        private bool _isHardLanding;
        private float _landingCooldownTimer;
        private const float BasicLandingCooldown = 0.2f; // Tempo de inviabilidade de pulo na aterrissagem suave
        private const float HardLandingRecoveryTime = 1.5f; // Tempo travado no chão tomando dano

        public PlayerGroundedState(Core.PlayerController context, PlayerStateMachine stateMachine, bool isHardLanding = false) : base(context, stateMachine)
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
            ctx.CurrentJumpCount = 0; // Reseta os pulos ao tocar o chão!
            ctx.ResetGravity();

            if (_isHardLanding)
            {
                // Toca animação pesada de rolar no chão ou tomar dano
                ctx.Animator.CrossFade("Hard_Landing", 0.1f);
                _landingCooldownTimer = HardLandingRecoveryTime;

                // Aqui você chamaria ctx.TakeDamage(10) por exemplo!
            }
            else
            {
                // Se já estávamos com input, ele vai tocar o Follow Through automaticamente via Locomotion Blend Tree.
                // Se estiver parado, toca um "Soft_Landing" rápido.
                ctx.Animator.CrossFade("Locomotion", 0.1f);
                _landingCooldownTimer = BasicLandingCooldown; // Cooldown básico
            }

        }

        public override void UpdateState()
        {
            // Diminui o timer de recuperação
            if (_landingCooldownTimer > 0)
            {
                _landingCooldownTimer -= Time.deltaTime;

                // Se for um Hard Landing, NÃO PERMITE MOVIMENTO enquanto se recupera
                if (_isHardLanding) return;
            }

            // Check jump
            if (ctx.ConsumeJumpInput() && _landingCooldownTimer <= 0)
            {
                stateMachine.ChangeState(new PlayerJumpState(ctx, stateMachine));
                return; // Sai do Update para não processar movimento no mesmo frame
            }


            // 1. Check Inputs and define movement
            CheckMovementLogic();

            // 2. Check if needs to play Idle Variant
            CheckIdleVariantLogic();

            // 3. Check Transitions (Ex: Jump, Attack)
            CheckTransitions();

            // Check falling
            if (!ctx.CharController.isGrounded && ctx.ForceVelocity.y < -0.5f) // Reduzi de -2f para -0.5f para detectar mais rápido
            {
                ApplyLedgeNudge(); // Aplica o empurrãozinho
                stateMachine.ChangeState(new PlayerFallState(ctx, stateMachine));
            }

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

        private void ApplyLedgeNudge()
        {
            // Só empurra se o jogador estiver se movendo. Se ele foi empurrado parado por um inimigo, não fazemos nada.
            if (ctx.MoveInput.sqrMagnitude > 0.1f)
            {
                // Força extra parametrizável (você pode colocar isso no PlayerController depois)
                float nudgeForce = 2.0f;

                // Empurra o personagem na direção em que ele está olhando
                Vector3 nudgeVector = ctx.transform.forward * nudgeForce;

                // Aplica o movimento imediatamente usando o CharacterController para descolar da quina
                // Usamos Time.deltaTime puro aqui porque é um "micro-teleporte" de 1 frame de correção
                ctx.CharController.Move(nudgeVector * Time.deltaTime);
            }
        }

    }

}
