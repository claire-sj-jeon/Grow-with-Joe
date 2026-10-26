using UnityEngine;
using UnityEngine.InputSystem;

// 마우스를 일정 시간(기본 5초) 쓰지 않으면 커서를 숨기고, 움직이거나 누르면 다시 보인다.
// 키보드로 메뉴를 다룰 때 화면에 커서가 남아 있지 않게 하려는 것이다.
// 커서를 켜고 끄는 원래 코드(PauseMenu 등)는 그대로 두고, 그 위에서 숨김만 더한다.
public class MouseCursorAutoHide : MonoBehaviour
{
    [SerializeField] float hideDelay = 5.0f;

    float idleTime;
    bool wasVisible;

    // 씬에 따로 붙이지 않아도 게임이 시작되면 한 번 만들어 계속 유지한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("MouseCursorAutoHide");
        DontDestroyOnLoad(go);
        go.AddComponent<MouseCursorAutoHide>();
    }

    void Update()
    {
        // 다른 코드가 방금 커서를 켰으면(메뉴 열림 등) 그 순간부터 다시 센다. 켜자마자 숨겨 버리지 않기 위해서다.
        if (Cursor.visible && !wasVisible) idleTime = 0.0f;

        if (MouseUsedThisFrame())
        {
            idleTime = 0.0f;
            Cursor.visible = true;
        }
        else
        {
            // 메뉴가 timeScale 을 건드리지 않지만, 혹시 멈춘 상태에서도 시간이 흐르도록 실제 시간을 쓴다
            idleTime += Time.unscaledDeltaTime;
            if (idleTime >= hideDelay && Cursor.visible) Cursor.visible = false;
        }

        wasVisible = Cursor.visible;
    }

    static bool MouseUsedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse == null) return false;

        return mouse.delta.ReadValue().sqrMagnitude > 0.0f
            || mouse.scroll.ReadValue().sqrMagnitude > 0.0f
            || mouse.leftButton.isPressed
            || mouse.rightButton.isPressed
            || mouse.middleButton.isPressed;
    }
}
