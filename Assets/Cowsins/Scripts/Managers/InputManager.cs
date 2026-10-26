using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace cowsins2D
{
    [DefaultExecutionOrder(-1000)]
    public class InputManager : MonoBehaviour
    {      
        public PlayerActions inputActions;

        public PlayerInputs PlayerInputs;

        public event Action onDrop, onJump, onJumpCut, onDash, onOpenInventory, onPause;

        private void Awake()
        {
            if (inputActions == null)  inputActions = new PlayerActions();
        }

        private void OnEnable()
        {
            // Initialize player inputs
            inputActions.Enable();

            inputActions.GameControls.Drop.started += OnDropStarted;
            inputActions.GameControls.Jumping.started += OnJumpStarted;
            inputActions.GameControls.Jumping.canceled += OnJumpCanceled;
            inputActions.GameControls.Dash.started += OnDashStarted;
            inputActions.GameControls.OpenInventory.started += OnOpenInventoryStarted;
            inputActions.GameControls.Pause.started += OnPauseStarted;
        }

        private void OnDisable()
        {
            inputActions.Disable();

            inputActions.GameControls.Drop.started -= OnDropStarted;
            inputActions.GameControls.Jumping.started -= OnJumpStarted;
            inputActions.GameControls.Jumping.canceled -= OnJumpCanceled;
            inputActions.GameControls.Dash.started -= OnDashStarted;
            inputActions.GameControls.OpenInventory.started -= OnOpenInventoryStarted;
            inputActions.GameControls.Pause.started -= OnPauseStarted;
        }

        private void OnDestroy()
        {
            if (inputActions != null) inputActions.Dispose();
        }

        // 팝업이 키보드를 가져간 동안(UIKeyboardNavigator.IsActive)에는 WASD 와 Enter 가 UI 조작에만 쓰인다.
        // 일시정지(ESC)는 막지 않는다.
        private void OnDropStarted(InputAction.CallbackContext ctx) { if (!UIKeyboardNavigator.IsActive) onDrop?.Invoke(); }
        private void OnJumpStarted(InputAction.CallbackContext ctx) { if (!UIKeyboardNavigator.IsActive) onJump?.Invoke(); }
        private void OnJumpCanceled(InputAction.CallbackContext ctx) => onJumpCut?.Invoke();
        private void OnDashStarted(InputAction.CallbackContext ctx) { if (!UIKeyboardNavigator.IsActive) onDash?.Invoke(); }
        private void OnOpenInventoryStarted(InputAction.CallbackContext ctx) { if (!UIKeyboardNavigator.IsActive) onOpenInventory?.Invoke(); }
        private void OnPauseStarted(InputAction.CallbackContext ctx) => onPause?.Invoke();

        // Update inputs in realtime
        private void Update() => PlayerInputs = UIKeyboardNavigator.IsActive ? BlockedInputs() : ReceiveInputs();

        // 이동 · 점프 · 웅크리기 등은 모두 0 으로 두고 마우스 위치와 일시정지만 살린다
        private PlayerInputs BlockedInputs()
        {
            return new PlayerInputs
            {
                MousePos = inputActions.GameControls.MousePosition.ReadValue<Vector2>(),
                Pausing = inputActions.GameControls.Pause.WasPressedThisFrame()
            };
        }

        private PlayerInputs ReceiveInputs()
        {
            // Returns all the necessary inputs in-game
            return new PlayerInputs
            {
                HorizontalMovement = inputActions.GameControls.Movement.ReadValue<float>(),
                VerticalMovement = -inputActions.GameControls.VerticalMovement.ReadValue<float>(),
                AimDirection = inputActions.GameControls.Aiming.ReadValue<Vector2>(),
                Crouch = inputActions.GameControls.Crouch.IsPressed(),
                JumpingDown = inputActions.GameControls.Jumping.WasPressedThisFrame(),
                JumpingUp = inputActions.GameControls.Jumping.WasReleasedThisFrame(),
                Jump = inputActions.GameControls.Jumping.IsPressed(),
                Run = inputActions.GameControls.Sprint.IsPressed(),
                Interact = inputActions.GameControls.Interact.IsPressed(),
                OpenInventory = inputActions.GameControls.OpenInventory.WasPressedThisFrame(),
                MousePos = inputActions.GameControls.MousePosition.ReadValue<Vector2>(),
                Shoot = inputActions.GameControls.Shoot.WasPressedThisFrame(),
                ShootHold = inputActions.GameControls.Shoot.IsPressed(),
                Reload = inputActions.GameControls.Reload.IsPressed(),
                Dash = inputActions.GameControls.Dash.WasPressedThisFrame(),
                MouseWheel = inputActions.GameControls.MouseWheel.ReadValue<Vector2>(),
                UINavigation = inputActions.GameControls.UINavigation.ReadValue<Vector2>(),
                UISelect = inputActions.GameControls.UISelect.WasPressedThisFrame(),
                InventoryDrop = inputActions.GameControls.DropInventory.WasPressedThisFrame(),
                InventoryUse = inputActions.GameControls.UseInventory.WasPressedThisFrame(),
                Drop = inputActions.GameControls.Drop.WasPressedThisFrame(),
                NextWeapon = inputActions.GameControls.NextWeapon.WasPressedThisFrame(),
                PreviousWeapon = inputActions.GameControls.PreviousWeapon.WasPressedThisFrame(),
                Pausing = inputActions.GameControls.Pause.WasPressedThisFrame()
            };
        }
    }
}
