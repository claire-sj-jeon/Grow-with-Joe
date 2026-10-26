using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 화면 오른쪽 위 구석을 연속으로 10번 터치(또는 마우스 클릭)하면 배경 음악을 끈다. 한 번 더 하면 다시 켠다.
// 배경 음악은 씬의 "bgms" 오브젝트에 붙은 AudioSource 다. 소리만 음소거하므로 곡의 재생 위치는 그대로 흘러간다.
public class BgmCornerTapToggle : MonoBehaviour
{
    const string BgmObjectName = "bgms";

    [SerializeField] int tapsRequired = 10;
    // 구석 영역의 크기. 화면 너비/높이에 대한 비율이다.
    [SerializeField] float cornerRatio = 0.15f;
    // 이 시간 안에 다음 터치가 없으면 연속이 끊긴 것으로 보고 처음부터 센다
    [SerializeField] float maxTapInterval = 0.6f;

    static bool bgmMuted;

    int tapCount;
    float lastTapTime;

    // 씬에 따로 붙이지 않아도 게임이 시작되면 한 번 만들어 계속 유지한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        bgmMuted = false;

        var go = new GameObject("BgmCornerTapToggle");
        DontDestroyOnLoad(go);
        go.AddComponent<BgmCornerTapToggle>();
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // 새 씬의 bgms 는 기본값(소리 켬)으로 시작하므로 현재 상태를 다시 입힌다
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyMute();

    void Update()
    {
        // Pointer 는 마지막으로 쓰인 마우스나 터치스크린(첫 번째 손가락)을 가리킨다
        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        // 일시정지 등으로 timeScale 이 0 이어도 시간이 흐르도록 실제 시간을 쓴다
        float now = Time.unscaledTime;

        // 구석 밖을 누르면 연속이 끊긴다
        if (!IsInTopRightCorner(pointer.position.ReadValue()))
        {
            tapCount = 0;
            return;
        }

        if (now - lastTapTime > maxTapInterval)
            tapCount = 0;

        lastTapTime = now;
        tapCount++;
        if (tapCount < tapsRequired) return;

        tapCount = 0;
        bgmMuted = !bgmMuted;
        ApplyMute();

        // 메시지 창이 없는 씬(메인 메뉴 등)에서는 소리만 바꾼다
        if (MessageBox.Instance != null)
            MessageBox.Instance.Show(bgmMuted ? "BGM OFF" : "BGM ON");
    }

    // 스크린 좌표는 왼쪽 아래가 (0, 0) 이다
    bool IsInTopRightCorner(Vector2 position)
    {
        return position.x >= Screen.width * (1.0f - cornerRatio)
            && position.y >= Screen.height * (1.0f - cornerRatio);
    }

    static void ApplyMute()
    {
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (source.gameObject.name == BgmObjectName)
                source.mute = bgmMuted;
    }
}
