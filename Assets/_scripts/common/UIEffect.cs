using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class UIEffect : MonoBehaviour
{
    public AnimationCurve curveSpellMoveEffect;
    public AnimationCurve curveAlphaEffect;
    public AnimationCurve curveScaleEffect;        
    public Transform positionTarget;
    public Transform positionStart;
    public UnityAction EffectComplete { get; set; }
    RegisterFunction.Element currentMove, currentScale, currentAlpha;                
    internal CanvasGroup canvasRoot => GetComponentInChildren<CanvasGroup>(true);
    public bool Activity
    {   get => canvasRoot.gameObject.activeSelf;
        set => canvasRoot.gameObject.SetActive(value);
    }
    public float Scale
    {   get => canvasRoot.transform.localScale.x;
        set => canvasRoot.transform.localScale = new Vector3(value, value, 1.0f); 
    }
    public float Alpha
    {   get => canvasRoot.alpha;
        set => canvasRoot.alpha = value;
    }
    public bool IsEnable
    {   get => canvasRoot.interactable;
        set => canvasRoot.interactable = canvasRoot.blocksRaycasts = value;
    }
    public virtual void ClearTimer()
    {
        currentMove?.Remove();
        currentScale?.Remove();
        currentAlpha?.Remove();
    }
    public virtual void Pause( bool bIs)
    {
        currentMove?.SetPause(bIs);
        currentScale?.SetPause(bIs);
        currentAlpha?.SetPause(bIs);
    }
    // 투명효과 담당
    public void AlphaCurve(CanvasGroup target, bool isReverse = false)
    {            
        currentAlpha?.Remove();
        currentAlpha = RegisterFunction.instance.Single(
            curveAlphaEffect,
            (time, value) => { target.alpha = isReverse ? 1.0f - value : value; },
            (time, count) => { currentAlpha = null; EffectComplete?.Invoke(); },
            "effect" );
    }
    void AlphaCurveRepeat(CanvasGroup target, bool isReverse = false)
    {
        currentAlpha?.Remove();
        currentAlpha = RegisterFunction.instance.Repeat(
            curveAlphaEffect,
            (time, value) => { target.alpha = isReverse ? 1.0f - value : value; },
            (time, count) => {},
            "effect" );
    }
    void MoveCurveStartAndEnd(RectTransform startRect, RectTransform endRect, bool isReverse)
    {
        currentMove?.Remove();

        var rectTrans = transform as RectTransform;
        Vector3 start = startRect.anchoredPosition;
        Vector2 end = endRect.anchoredPosition;

        Debug.Log($"MoveCurveStartAndEnd==>{transform.name}");

        currentMove = RegisterFunction.instance.Single(
        curveSpellMoveEffect,
        (time, value) => { rectTrans.anchoredPosition = Vector2.Lerp(start, end, isReverse ? 1.0f - value : value); },
        (time, count) => { currentMove = null; EffectComplete?.Invoke(); },
        "effect");
    }
    void MoveCurveStartAndEndRepeat(RectTransform startRect, RectTransform endRect, bool isReverse)
    {
        currentMove?.Remove();

        var rectTrans = transform as RectTransform;
        Vector3 start = startRect.anchoredPosition;
        Vector2 end = endRect.anchoredPosition;

        //Debug.Log($"MoveCurveStartAndEnd==>{transform.name}");

        currentMove = RegisterFunction.instance.Repeat(
        curveSpellMoveEffect,
        (time, value) => { rectTrans.anchoredPosition = Vector2.Lerp(start, end, isReverse ? 1.0f - value : value); },
        (time, count) => {  },
        "effect");
    }

    void ScaleCurveRepeat(CanvasGroup target )
    {
        currentScale?.Remove();

        currentScale = RegisterFunction.instance.Repeat(
            curveScaleEffect,
            (time, value) => target.transform.localScale = Vector3.one * value,
            (time, count) => { },
            "effect");
    }
    void ScaleCurve(CanvasGroup target)
    {
        currentScale?.Remove();

        currentScale = RegisterFunction.instance.Single(
            curveScaleEffect,
            (time, value) => target.transform.localScale = Vector3.one * value,
            (time, count) => { currentScale = null; EffectComplete?.Invoke(); },
            "effect");
    }
    void ScaleCurveValue(CanvasGroup target, float multiply = 1.0f)
    {
        currentScale?.Remove();

        var startScale = target.transform.localScale;
        var targetScale = Vector3.one * multiply;
        currentScale = RegisterFunction.instance.Single(
            curveScaleEffect,
            (time, value) => { target.transform.localScale = Vector3.Lerp(startScale, targetScale, value); },
            (time, count) => { currentScale = null; EffectComplete?.Invoke(); },
            "effect");
    }
    internal UIEffect StartEffect(UnityAction actionStart)
    {
        EffectComplete = () => { };
        actionStart?.Invoke();
        return this;
    }
    private void OnEnable() { }
    private void OnDisable() { }
    private void Pause() => Pause(true);
    private void UnPause() =>  Pause(false);
    public UIEffect PositionEffectStartEndEffect( Transform start, Transform end )  => 
            StartEffect(delegate () { MoveCurveStartAndEnd(start as RectTransform, end as RectTransform, false); });
    public UIEffect PositionEffect() =>
            StartEffect(delegate () { MoveCurveStartAndEnd(canvasRoot.transform as RectTransform, positionTarget as RectTransform, false); });
    public UIEffect PositionEffectRepeat() =>
            StartEffect(delegate () { MoveCurveStartAndEndRepeat(positionStart as RectTransform, positionTarget as RectTransform, false); });
    public UIEffect ScaleEffect() => StartEffect(() => ScaleCurve(canvasRoot));
    public UIEffect ScaleEffect(float scale) => StartEffect(() => ScaleCurveValue(canvasRoot, scale));
    public UIEffect AlphaEffect() => StartEffect(() => AlphaCurve(canvasRoot));
    public UIEffect ReverseAlphaEffect() => StartEffect(() => AlphaCurve(canvasRoot, true));
    public UIEffect AlphaRepeatEffect() => StartEffect(() => AlphaCurveRepeat(canvasRoot));
    public UIEffect ScaleRepeatEffect() => StartEffect(() => ScaleCurveRepeat(canvasRoot));
    public UIEffect SetAlpha(float value) { Alpha = value; return this; }
    public UIEffect SetActive(bool value) { Activity = value; return this; }
}
