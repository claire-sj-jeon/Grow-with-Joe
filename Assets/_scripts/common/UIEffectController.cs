using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIEffectController : MonoBehaviour
{   
        private UIExtEffect uiEffect;

        [Header("Effect Options")]
        public bool playScaleEffect = false;
        public bool playAlphaEffect = false;
        public bool playScaleRepeatEffect = false;
        public bool playAlphaRepeatEffect = false;
        public bool playRotateRepeatEffect = false;
        public bool playMoveRepeatEffect = false;

    private void Awake()
        {
            uiEffect = GetComponent<UIExtEffect>(); 
        }

        private void OnEnable()
        {
        

            uiEffect = GetComponent<UIExtEffect>();

            if (playScaleEffect)
                uiEffect.ScaleEffect();

            if (playAlphaEffect)
                uiEffect.AlphaEffect();

            if (playScaleRepeatEffect)
                uiEffect.ScaleRepeatEffect();

            if (playAlphaRepeatEffect)
                uiEffect.AlphaRepeatEffect();

            if (playRotateRepeatEffect)
                uiEffect.RotateEffectRepeat();

            if (playMoveRepeatEffect)
                uiEffect.PositionEffectRepeat();
        }

        private void OnDisable()
        {
            uiEffect.ClearTimer();
        }

}
