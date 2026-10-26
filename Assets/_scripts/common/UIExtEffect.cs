using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class UIExtEffect : UIEffect
{
    public AnimationCurve curveRotate;
    public float rotateZ;

    RegisterFunction.Element currentRotate;

    public override void ClearTimer()
    {
        currentRotate?.Remove();
    }
    public override void Pause( bool bIs)
    {
        currentRotate?.SetPause(bIs);
    }
    // 투명효과 담당
    public void RotateCurve(CanvasGroup target, bool isReverse = false)
    {
        currentRotate?.Remove();
        currentRotate = RegisterFunction.instance.Single(
            curveRotate,
            (time, value) => 
            {
                var euler = transform.localRotation.eulerAngles;
                var alpha =  isReverse ? 1.0f - value : value;
                euler.z = rotateZ * alpha;
                transform.localRotation = Quaternion.Euler( euler );
            },
            (time, count) => { currentRotate = null; EffectComplete?.Invoke(); },
            "effect" );
    }
    void RotateCurveRepeat(CanvasGroup target, bool isReverse = false)
    {

        currentRotate?.Remove();
        currentRotate = RegisterFunction.instance.Repeat(
            curveRotate,
            (time, value) =>
            {
                var euler = transform.localRotation.eulerAngles;
                var alpha = isReverse ? 1.0f - value : value;
                euler.z = rotateZ * alpha;
                transform.localRotation = Quaternion.Euler(euler);
            },
            (time, count) => { },
            "effect");

    }

    public UIEffect RotateEffect() => StartEffect(() => RotateCurve(canvasRoot));
    public UIEffect RotateEffectRepeat() => StartEffect(() => RotateCurveRepeat(canvasRoot));

}
