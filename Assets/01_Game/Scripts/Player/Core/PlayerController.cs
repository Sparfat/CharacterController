using MyGame.Player.Input;
using MyGame.Player.StateMachine;
using UnityEngine;

namespace MyGame.Player.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Animator _animator;

        //Internal references
        public Animator Animator => _animator;
        public InputReader InputReader => _inputReader;
        public CharacterController CharController { get; private set; }
        public PlayerStateMachine StateMachine { get; private set; }

        [Header("Movement Settings")]
        public float WalkSpeed = 2f;
        public float RunSpeed = 6f;
        public float Gravity = -9.81f;
        public float RotationSpeed = 15f;

        //Movement control variables
        [HideInInspector] public Vector2 MoveInput;
        [HideInInspector] public float MoveSpeed; // State defines if is Walk or Run
        [HideInInspector] public Vector3 ForceVelocity; //For gravity and force

        [Header("Jump Settings")]
        public float JumpForce = 5f;
        [Range(0, 1)] public float AirControl = 0.5f; // 0 = No control; 1 = Full control

        private void Awake()
        {
            CharController = GetComponent<CharacterController>();
            StateMachine = new PlayerStateMachine();
            //Here we instatiate the first state

        }

        private void Start()
        {
            // Starts at GroundedState (Idle/Walk/Run)
            StateMachine.Initialize(new States.PlayerGroundedState(this, StateMachine));
        }

        private void OnEnable()
        {
            // Assining the events to the input reader
            _inputReader.MoveEvent += OnMove;
        }

        private void OnDisable()
        {
            // Unsubscribing the events to avoid memory leaks
            _inputReader.MoveEvent -= OnMove;
        }

        private void Update()
        {
            // State Machine comands logic
            StateMachine.CurrentState?.UpdateState();

            ApplyGravity();
        }

        private void FixedUpdate()
        {
            // State Machine physics comands 
            StateMachine.CurrentState?.FixedUpdateState();
        }

        private void OnMove(Vector2 input) => MoveInput = input;

        private void ApplyGravity()
        {
            if(CharController.isGrounded && ForceVelocity.y < 0)
            {
                ForceVelocity.y = -2f; //Small value to keep the player grounded
            }

            ForceVelocity.y += Gravity * Time.deltaTime;
            
        }

        // To reset gravity after jump
        public void ResetGravity()
        {
            ForceVelocity.y = -2f;
        }

    }
}