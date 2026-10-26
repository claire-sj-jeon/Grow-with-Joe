using UnityEngine;

/// <summary>
/// 간단한 알림용 메시지 창. MessageBox.Instance.Show("...") 로 호출한다.
/// </summary>
public class MessageBox : MonoBehaviour
{
    public static MessageBox Instance { get; private set; }

    [SerializeField] UiObject uiMessage;
    [SerializeField] float defaultDuration = 2.0f;

    // Show 가 연달아 불리면 이전 타이머가 새 메시지를 지워버리므로 매번 교체한다.
    RegisterFunction.Element hideTimer;

    // 0 번째 자식만 껐다 켠다. 자기 자신을 끄면 이후 Show 를 받을 수 없다.
    Transform Body => (Instance != null && Instance.transform.childCount > 0)
        ? Instance.transform.GetChild(0)
        : null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {   Destroy(gameObject);
            return;
        }

        Instance = this;
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string message) => Show(message, defaultDuration);

    public void Show(string message, float duration)
    {
        var body = Body;
        if (body == null)
        {   Debug.LogWarning("[MessageBox] uiMessage 또는 0번째 자식이 없습니다.");
            return;
        }

        // UiObject.Text 는 GetComponentInChildren 이라 꺼진 상태에서는 찾지 못한다. 먼저 켠다.
        body.gameObject.SetActive(true);

        var text = uiMessage.Text;
        if (text != null) text.text = message;

        RegisterFunction.instance.Remove(hideTimer);
        hideTimer = RegisterFunction.SingleCall(duration, Hide);
    }

    public void Hide()
    {
        RegisterFunction.instance.Remove(hideTimer);
        hideTimer = null;

        var body = Body;
        if (body != null) body.gameObject.SetActive(false);
    }
}
