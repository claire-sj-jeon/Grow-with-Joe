using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 팝업(메뉴, 대화창)이 떠 있는 동안 키보드만으로 버튼을 고르고 누르게 한다.
// W / A : 이전 버튼, S / D : 다음 버튼, Enter / O : 누르기
// Enabled 가 false 면(InputManager 의 옵션) 아무것도 하지 않는다.
// 팝업은 RegisterScope 로 등록한다. 여러 개가 열려 있으면 priority 가 큰 쪽이 키를 가져간다.
public class UIKeyboardNavigator : MonoBehaviour
{
    class Scope
    {
        public object owner;
        public Transform root;
        public Func<bool> isOpen;
        public int priority;
    }

    static readonly List<Scope> scopes = new List<Scope>();
    static UIKeyboardNavigator instance;

    // 팝업이 키보드를 가져간 상태. InputManager 가 이 동안 캐릭터 입력을 막는다.
    public static bool IsActive { get; private set; }

    // 키보드 UI 조작 사용 여부. InputManager 의 옵션으로 켜고 끈다.
    // 끄면 포커스 표시도 사라지고 키 입력도 캐릭터 쪽으로 그대로 간다.
    public static bool Enabled { get; set; } = true;

    [SerializeField] Color frameColor = new Color(1.0f, 0.62f, 0.1f, 1.0f);
    [SerializeField] float frameThickness = 8.0f;
    [SerializeField] float framePadding = 8.0f;

    // 밝은 선택지 카드 위에서도 주황 테두리가 묻히지 않게 바깥에 짙은 외곽선을 두른다
    [SerializeField] Color frameOutlineColor = new Color(0.25f, 0.12f, 0.02f, 0.9f);
    [SerializeField] float frameOutlineWidth = 2.0f;

    readonly List<GameObject> items = new List<GameObject>();
    Scope currentScope;
    GameObject focused;

    RectTransform frame;
    Image[] frameBars;

    // 씬을 다시 읽어도 팝업 쪽이 다시 등록하므로, 도메인 리로드를 끈 에디터에서도 깨끗하게 시작하도록 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        scopes.Clear();
        instance = null;
        IsActive = false;
        Enabled = true;
    }

    public static void RegisterScope(object owner, Transform root, Func<bool> isOpen, int priority = 0)
    {
        UnregisterScope(owner);
        scopes.Add(new Scope { owner = owner, root = root, isOpen = isOpen, priority = priority });
        EnsureInstance();
    }

    public static void UnregisterScope(object owner)
    {
        scopes.RemoveAll(s => s.owner == owner);
    }

    static void EnsureInstance()
    {
        if (instance != null) return;

        var go = new GameObject("UIKeyboardNavigator");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UIKeyboardNavigator>();
    }

    void Awake()
    {
        BuildFrame();
    }

    void Update()
    {
        var scope = Enabled ? FindOpenScope() : null;
        IsActive = scope != null;

        // EventSystem 자체의 키보드 탐색(WASD 이동, Enter 제출)이 살아 있으면 같은 키로 버튼이 두 번 눌린다.
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = !IsActive;

        if (scope == null)
        {
            currentScope = null;
            focused = null;
            frame.gameObject.SetActive(false);
            return;
        }

        if (scope != currentScope)
        {
            currentScope = scope;
            focused = null;
        }

        CollectItems(scope.root);
        if (items.Count == 0)
        {
            frame.gameObject.SetActive(false);
            return;
        }

        // 버튼 구성이 바뀌어 포커스 대상이 사라졌으면 첫 버튼부터 다시 잡는다
        int index = Mathf.Max(0, items.IndexOf(focused));

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                index = (index - 1 + items.Count) % items.Count;
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                index = (index + 1) % items.Count;
        }

        focused = items[index];
        UpdateFrame(focused);

        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.oKey.wasPressedThisFrame))
            Submit(focused);
    }

    // 키보드 조작 옵션(Enabled)과 상관없이 등록된 팝업이 하나라도 떠 있는지
    public static bool AnyPopupOpen => FindOpenScope() != null;

    static Scope FindOpenScope()
    {
        Scope best = null;
        for (int i = scopes.Count - 1; i >= 0; --i)
        {
            var s = scopes[i];

            // 씬이 바뀌며 파괴된 팝업은 등록만 남아 있을 수 있다
            if (s.root == null) { scopes.RemoveAt(i); continue; }
            if (!s.isOpen()) continue;

            if (best == null || s.priority > best.priority) best = s;
        }
        return best;
    }

    // 보이고 누를 수 있는 버튼만 모아 화면 위치 순서(위 -> 아래, 왼쪽 -> 오른쪽)로 정렬한다.
    void CollectItems(Transform root)
    {
        items.Clear();

        foreach (var button in root.GetComponentsInChildren<Button>(false))
            if (button.IsInteractable() && IsVisible(button.transform))
                items.Add(button.gameObject);

        // UIButton.Activity 는 자기 자신이 아니라 자식 CanvasGroup 오브젝트를 켜고 끈다
        foreach (var button in root.GetComponentsInChildren<UIButton>(false))
            if (button.IsEnable && button.Activity && IsVisible(button.transform))
                items.Add(button.gameObject);

        items.Sort((a, b) =>
        {
            var pa = ScreenRect(a).center;
            var pb = ScreenRect(b).center;

            // 같은 줄로 보이는 높이 차이는 무시하고 왼쪽부터
            if (Mathf.Abs(pa.y - pb.y) > 20.0f) return pb.y.CompareTo(pa.y);
            return pa.x.CompareTo(pb.x);
        });
    }

    static bool IsVisible(Transform target)
    {
        for (var t = target; t != null; t = t.parent)
        {
            var group = t.GetComponent<CanvasGroup>();
            if (group == null) continue;
            if (group.alpha <= 0.01f || !group.interactable) return false;
            if (group.ignoreParentGroups) break;
        }
        return true;
    }

    // 마우스로 누른 것과 똑같이 down -> up -> click 을 보낸다.
    // Button 의 onClick, 메뉴의 EventTrigger(PointerDown) 연출, UIButton 의 callbackDown 이 그대로 동작한다.
    static void Submit(GameObject target)
    {
        var data = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = ScreenRect(target).center,
            pointerPress = target,
            rawPointerPress = target,
        };

        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);

        // pointerDown 이 Button 을 선택 상태로 만든다. 남겨 두면 다음 Enter 가 EventSystem 쪽에서도 처리된다.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // 버튼 오브젝트 자체는 크기가 0 인 경우가 있어, 실제로 그려지는 Graphic 들을 합친 영역을 쓴다.
    static Rect ScreenRect(GameObject target)
    {
        var corners = new Vector3[4];
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;

        foreach (var graphic in target.GetComponentsInChildren<Graphic>(false))
        {
            if (!graphic.enabled || graphic.canvas == null) continue;

            var canvas = graphic.canvas.rootCanvas;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            graphic.rectTransform.GetWorldCorners(corners);
            foreach (var c in corners)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, c);
                if (!any) { min = max = p; any = true; }
                else { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            }
        }

        if (!any)
        {
            ((RectTransform)target.transform).GetWorldCorners(corners);
            min = max = corners[0];
            foreach (var c in corners) { min = Vector2.Min(min, c); max = Vector2.Max(max, c); }
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // 포커스 표시용 테두리. 어떤 Canvas 위에 있는 버튼이든 덮을 수 있게 최상단 오버레이 캔버스에 그린다.
    void BuildFrame()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        frame = new GameObject("FocusFrame", typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(transform, false);
        frame.anchorMin = frame.anchorMax = Vector2.zero;
        frame.pivot = Vector2.zero;

        // 위, 아래, 왼쪽, 오른쪽 막대 네 개로 사각 테두리를 만든다
        var anchors = new[]
        {
            (new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1.0f)),
            (new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.0f)),
            (new Vector2(0, 0), new Vector2(0, 1), new Vector2(0.0f, 0.5f)),
            (new Vector2(1, 0), new Vector2(1, 1), new Vector2(1.0f, 0.5f)),
        };

        frameBars = new Image[anchors.Length];
        for (int i = 0; i < anchors.Length; ++i)
        {
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bar.SetParent(frame, false);
            bar.anchorMin = anchors[i].Item1;
            bar.anchorMax = anchors[i].Item2;
            bar.pivot = anchors[i].Item3;
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = i < 2 ? new Vector2(0, frameThickness) : new Vector2(frameThickness, 0);

            frameBars[i] = bar.GetComponent<Image>();
            frameBars[i].raycastTarget = false;

            var outline = bar.gameObject.AddComponent<Outline>();
            outline.effectColor = frameOutlineColor;
            outline.effectDistance = new Vector2(frameOutlineWidth, -frameOutlineWidth);
        }

        frame.gameObject.SetActive(false);
    }

    void UpdateFrame(GameObject target)
    {
        var rect = ScreenRect(target);
        frame.gameObject.SetActive(true);
        frame.anchoredPosition = new Vector2(rect.xMin - framePadding, rect.yMin - framePadding);
        frame.sizeDelta = new Vector2(rect.width + framePadding * 2, rect.height + framePadding * 2);

        // 눈에 띄도록 천천히 깜박인다. 메뉴가 timeScale 을 건드리지 않지만 혹시 몰라 실제 시간을 쓴다.
        var color = frameColor;
        color.a *= 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 6.0f);
        foreach (var bar in frameBars) bar.color = color;
    }
}
