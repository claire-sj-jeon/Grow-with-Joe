using UnityEngine;

/// <summary>
/// SpriteRenderer 를 AnimationCurve 3개(X 이동 / Y 이동 / 알파)로 간단히 애니메이션한다.
/// 위치는 배치된 지점(원점)에 커브 값을 더한 오프셋으로 계산하므로, 씬에 놓인 자리를 그대로 유지한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteCurveAnimation : MonoBehaviour
{
    public enum PlayType
    {
        OnceOnAwake,
        Loop,
        PingPong,
    }

    [Header("재생 방식")]
    public PlayType playType = PlayType.Loop;

    [Header("커브 (가로축 = 초, 세로축 = 배율 적용 전 값)")]
    public AnimationCurve curveX = AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
    public AnimationCurve curveY = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    public AnimationCurve curveAlpha = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);

    [Header("커브 값에 곱해지는 상수")]
    public float multiplyX = 1.0f;
    public float multiplyY = 1.0f;

    [Header("켜짐 / 꺼짐")]
    public bool visibleOnEnable = true;

    // RegisterFunction.Remove(category) 로 여러 이펙트를 한꺼번에 정리할 수 있도록 분류를 노출한다.
    public string timerCategory = "effect";

    /// <summary>세 커브 중 가장 늦게 끝나는 시간. 이 값이 1회 재생 길이가 된다.</summary>
    public float Duration { get; private set; }
    public bool IsPlaying => current != null;
    public bool IsVisible => spriteRenderer != null && spriteRenderer.enabled;

    SpriteRenderer spriteRenderer;
    RegisterFunction.Element current;
    Vector3 originPosition;
    Color originColor;

    void Awake() => Cache();

    // Awake 보다 Show/Hide 가 먼저 불릴 수 있어(AddComponent 직후 등) 캐싱은 지연 호출로 처리한다.
    void Cache()
    {
        if (spriteRenderer != null) return;

        spriteRenderer = GetComponent<SpriteRenderer>();
        // 원점은 첫 프레임의 로컬 위치로 한 번만 잡는다. 재생 중에 갱신하면 오프셋이 누적된다.
        originPosition = transform.localPosition;
        originColor = spriteRenderer.color;
    }

    // 풀에서 재사용되는 오브젝트도 다시 활성화될 때 인스펙터에 지정한 상태로 돌아가도록 한다.
    void OnEnable() => SetVisible(visibleOnEnable);
    void OnDisable() => Stop();

    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);
    public void Toggle() => SetVisible(!IsVisible);

    /// <summary>
    /// SpriteRenderer.enabled 를 켜고 끈다.
    /// 안 보이는 동안 커브를 돌릴 이유가 없으므로 타이머도 같이 정리하고, 켤 때 처음부터 다시 재생한다.
    /// </summary>
    public void SetVisible(bool visible)
    {
        Cache();
        spriteRenderer.enabled = visible;

        if (visible) Play();
        else Stop();
    }

    /// <summary>커브 재생만 다시 시작한다. 꺼진 상태에서 호출해도 보이지는 않는다(Show 를 쓸 것).</summary>
    public void Play()
    {
        Cache();
        Stop();

        Duration = Mathf.Max(EndTime(curveX), EndTime(curveY), EndTime(curveAlpha));
        if (Duration <= 0.0f)
        {   Apply(0.0f);
            return;
        }

        bool isPingPong = playType == PlayType.PingPong;

        current = RegisterFunction.instance.Base()
            .SetCategory(timerCategory)
            .SetTarget(isPingPong ? Duration * 2.0f : Duration)
            .SetRepeat(playType != PlayType.OnceOnAwake)
            .SetCallback((element, time) => Apply(isPingPong ? PingPong(time) : time))
            .SetCallbackComplete((element, count) =>
            {   // UpdateTime 은 완주 시점의 값으로는 콜백하지 않으므로 마지막 프레임을 직접 찍어준다.
                Apply(isPingPong ? 0.0f : Duration);
                if (playType == PlayType.OnceOnAwake) current = null;
            });

        Apply(0.0f);
    }

    public void Stop()
    {
        current?.Remove();
        current = null;
    }

    /// <summary>커브 적용 이전 상태(원점 위치 / 원래 색)로 되돌린다.</summary>
    public void ResetToOrigin()
    {
        Cache();
        transform.localPosition = originPosition;
        spriteRenderer.color = originColor;
    }

    void Apply(float time)
    {
        transform.localPosition = originPosition + new Vector3(
            Evaluate(curveX, time, 0.0f) * multiplyX,
            Evaluate(curveY, time, 0.0f) * multiplyY,
            0.0f);

        var color = originColor;
        color.a = Evaluate(curveAlpha, time, 1.0f);
        spriteRenderer.color = color;
    }

    // 0 -> Duration -> 0 으로 접어서 왕복시킨다.
    float PingPong(float time) => time <= Duration ? time : Duration * 2.0f - time;

    // 키가 없는 커브는 "사용하지 않음"으로 보고 기본값을 돌려준다.
    static float Evaluate(AnimationCurve curve, float time, float defaultValue)
        => curve == null || curve.length == 0 ? defaultValue : curve.Evaluate(time);

    static float EndTime(AnimationCurve curve)
        => curve == null || curve.length == 0 ? 0.0f : curve[curve.length - 1].time;
}
