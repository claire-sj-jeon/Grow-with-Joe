using UnityEngine;
using UnityEngine.InputSystem;

namespace cowsins2D
{
    // 게임 중 R 키를 누르면 재시작 여부를 묻는 메시지 창을 띄우고 게임을 멈춘다.
    // P 를 누르면 처음부터 다시 시작하고, 다른 키(마우스 클릭/터치 포함)를 누르면 창을 닫고 이어서 한다.
    public class RestartConfirm : MonoBehaviour
    {
        const string ConfirmMessage =
            "Do you really want to start over? If so, press P again. If not, press any other button";

        // 창이 떠 있는 동안 InputManager 가 캐릭터 입력과 일시정지(ESC)를 막는다.
        // 닫는 키가 그대로 점프나 일시정지 메뉴로 이어지지 않게 하기 위함이다.
        public static bool IsOpen { get; private set; }

        float timeScaleBeforeOpen = 1.0f;

        // 연 프레임에는 R 자체가 "다른 키" 로 잡혀 곧바로 닫히므로 다음 프레임부터 판정한다
        int openedFrame;

        // 씬에 따로 붙이지 않아도 게임이 시작되면 한 번 만들어 계속 유지한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            IsOpen = false;

            var go = new GameObject("RestartConfirm");
            DontDestroyOnLoad(go);
            go.AddComponent<RestartConfirm>();
        }

        void OnDestroy()
        {
            if (IsOpen) Close();
        }

        void Update()
        {
            var keyboard = Keyboard.current;

            if (!IsOpen)
            {
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame && CanOpen()) Open();
                return;
            }

            if (Time.frameCount == openedFrame) return;

            if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
            {
                Close();
                PauseMenu.Instance.GameAgain();
            }
            else if (AnyOtherPressed(keyboard))
            {
                Close();
            }
        }

        // 실제로 플레이 중일 때만 연다. 타이틀/일시정지 메뉴, 엔딩 뒤 메뉴, 대화창 위에서는 무시한다.
        static bool CanOpen()
        {
            return MessageBox.Instance != null
                && PauseMenu.Instance != null
                && !PauseMenu.isPaused
                && PauseMenu.CurrentMode == PauseMenu.MenuMode.Normal
                && !UIKeyboardNavigator.AnyPopupOpen;
        }

        static bool AnyOtherPressed(Keyboard keyboard)
        {
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;

            var mouse = Mouse.current;
            if (mouse != null &&
                (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
                return true;

            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        }

        void Open()
        {
            IsOpen = true;
            openedFrame = Time.frameCount;

            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0.0f;

            // 시간이 0 이 되어도 MessageBox 타이머는 실제 시간으로 흐르므로 자동으로 닫히지 않게 한다
            MessageBox.Instance.Show(ConfirmMessage, 0.0f);
        }

        // 씬을 다시 읽기 전에도 반드시 불러야 한다. timeScale 은 씬을 넘어 유지된다.
        void Close()
        {
            IsOpen = false;
            Time.timeScale = timeScaleBeforeOpen;

            if (MessageBox.Instance != null) MessageBox.Instance.Hide();
        }
    }
}
