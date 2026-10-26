using UnityEngine;
using UnityEngine.InputSystem;
namespace cowsins2D {
public class DeviceDetection : MonoBehaviour
{
    public enum InputMode
    {
        Keyboard, Controller, Mobile
    }
    public InputMode mode { get; private set; }

    public static DeviceDetection Instance;

    bool controllerInputReceived = false;

    [SerializeField] private GameObject touchInputUI; 

    private void Awake()
    {
        // Makes sure there is only once instance of this script in the game
        if (Instance == null) Instance = this;

    }

    private void Start()
    {
            // 실행하는 platform 이 모바일 (webbuild 에서도 모바일플랫폼 브라우저상태라면
            // touchInputUI 을 setactive true 하고 
            // 아니면 setactive 를 false 한다.

            bool isMobileDevice = IsMobileDevice();

            if (touchInputUI != null)
                touchInputUI.SetActive(isMobileDevice);

            if (isMobileDevice)
                mode = InputMode.Mobile;


    }

    private bool IsMobileDevice()
    {
        // 1. Android / iOS 앱 빌드
        if (Application.isMobilePlatform)
            return true;

        // 2. WebBuild에서 모바일 브라우저 판별
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // 터치스크린이 있으면 모바일 브라우저일 가능성이 높음
            if (Touchscreen.current != null)
                return true;

            // 모바일 브라우저에서 마우스 없이 터치만 있는 경우 대응            
            foreach (var device in InputSystem.devices)
            {
                if (device is Touchscreen)
                    return true;
            }           
        }
        return false;
    }

        private void Update() => DetectInputs(); 

    public void DetectInputs()
    {
        // Detects if any input has been received from the keyboard or the mouse
        bool KeyboardInputReceived = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame || (Mouse.current.delta.ReadValue() != Vector2.zero || Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed);
            
        if (KeyboardInputReceived) controllerInputReceived = false;
        else
        {
                // If no input was received from the keyboard, check from the controller
                Gamepad gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    // Loop through each control on the gamepads to see if that is pressed
                    foreach (InputControl control in gamepad.allControls)
                    {
                        if (control.IsPressed())
                        {
                            controllerInputReceived = true;
                            break;
                        }
                    }
                }
        }
        

        // Update the current device detected accordingly
        if (controllerInputReceived) mode = InputMode.Controller;
        else if (KeyboardInputReceived)  mode = InputMode.Keyboard;
    }
}
}