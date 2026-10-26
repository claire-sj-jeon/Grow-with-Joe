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

        // 팝업 메뉴를 키보드(W/A 이전, S/D 다음, Enter 누르기)로 조작하는 기능. 끄면 마우스/터치로만 다룬다.
        [SerializeField] private bool uiKeyboardNavigation = true;

        public bool UIKeyboardNavigation
        {
            get => uiKeyboardNavigation;
            set => UIKeyboardNavigator.Enabled = uiKeyboardNavigation = value;
        }

        private void Awake()
        {
            if (inputActions == null)  inputActions = new PlayerActions();
        }

        // 플레이 중 인스펙터에서 체크를 바꿔도 바로 반영되게 한다
        private void OnValidate() => UIKeyboardNavigator.Enabled = uiKeyboardNavigation;

        private void OnEnable()
        {
            UIKeyboardNavigator.Enabled = uiKeyboardNavigation;

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
        // 재시작 확인창(RestartConfirm)이 떠 있는 동안에는 아무 키나 "닫기" 이므로 캐릭터 입력과 일시정지까지 막는다.
        private static bool GameplayBlocked => UIKeyboardNavigator.IsActive || RestartConfirm.IsOpen;

        // 일시정지(ESC)는 키보드 팝업 중에는 막지 않는다.
        private void OnDropStarted(InputAction.CallbackContext ctx) { if (!GameplayBlocked) onDrop?.Invoke(); }
        private void OnJumpStarted(InputAction.CallbackContext ctx) { if (!GameplayBlocked) onJump?.Invoke(); }
        private void OnJumpCanceled(InputAction.CallbackContext ctx) => onJumpCut?.Invoke();
        private void OnDashStarted(InputAction.CallbackContext ctx) { if (!GameplayBlocked) onDash?.Invoke(); }
        private void OnOpenInventoryStarted(InputAction.CallbackContext ctx) { if (!GameplayBlocked) onOpenInventory?.Invoke(); }
        private void OnPauseStarted(InputAction.CallbackContext ctx) { if (!RestartConfirm.IsOpen) onPause?.Invoke(); }

        // Update inputs in realtime
        private void Update() => PlayerInputs = GameplayBlocked ? BlockedInputs() : ReceiveInputs();

        // 이동 · 점프 · 웅크리기 등은 모두 0 으로 두고 마우스 위치와 일시정지만 살린다
        private PlayerInputs BlockedInputs()
        {
            return new PlayerInputs
            {
                MousePos = inputActions.GameControls.MousePosition.ReadValue<Vector2>(),
                Pausing = !RestartConfirm.IsOpen && inputActions.GameControls.Pause.WasPressedThisFrame()
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
