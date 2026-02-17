using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace MyGame.Player.Input
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "Player/Input Reader")]
    public class InputReader : ScriptableObject, PlayerInputActions.IPlayerActions
    {
        //Events for states
        public event UnityAction<Vector2> MoveEvent = delegate { };
        public event UnityAction SprtinEvent = delegate { };
        public event UnityAction JumpEvent = delegate { };
        public event UnityAction RollEvent = delegate { };
        public event UnityAction LightAttackEvent = delegate { };
        public event UnityAction HeavyAttackEvent = delegate { };
        public event UnityAction InteractEvent = delegate { };
        public event UnityAction PauseEvent = delegate { };

        private PlayerInputActions _inputActions;

        private void OnEnable()
        {
            if(_inputActions == null)
            {
                _inputActions = new PlayerInputActions();
                _inputActions.Player.SetCallbacks(this);
            }
            _inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }

        //Interface IPlayerActions implementation
        public void OnMove(InputAction.CallbackContext context) => MoveEvent.Invoke(context.ReadValue<Vector2>());
        public void OnSprint(InputAction.CallbackContext context) { if (context.performed) SprtinEvent.Invoke(); }
        public void OnJump(InputAction.CallbackContext context) { if(context.performed) JumpEvent.Invoke(); }
        public void OnRoll(InputAction.CallbackContext context) { if(context.performed) RollEvent.Invoke(); }
        public void OnLightAttack(InputAction.CallbackContext context) { if(context.performed) LightAttackEvent.Invoke(); }
        public void OnHeavyAttack(InputAction.CallbackContext context) { if(context.performed) HeavyAttackEvent.Invoke(); }
        public void OnInteract(InputAction.CallbackContext context) { if(context.performed) InteractEvent.Invoke(); }
        public void OnPause(InputAction.CallbackContext context) { if(context.performed) PauseEvent.Invoke(); }

        //Look-on/Look
        public void OnLook(InputAction.CallbackContext context) { }

    }
}