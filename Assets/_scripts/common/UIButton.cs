using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler, IPointerClickHandler
{
    [SerializeField]
    float DownDelay = 0.2f;
    CanvasGroup canvasGr { get { return gameObject.GetComponentInChildren<CanvasGroup>(true); } }

    public float Alpha
    { get { return canvasGr.alpha; }
        set { canvasGr.alpha = value; }
    }
    public bool Activity
    { get { return canvasGr.gameObject.activeSelf; }
        set { canvasGr.gameObject.SetActive(value); }
    }
    public float TimeDown { get; set; }    
    public bool IsPointed { get; private set; }
    public int ValueInt { get; set; }
    public bool IsValid { get; set; }
    public bool IsEnable { get; set; }  
    public int CountDown { get; private set; }
    public int SelectIndex { get; set; }
    public PointerEventData EventData { get; private set; }
    public Image ImageComponent {  get { return gameObject.GetComponentInChildren<Image>(); } }
    public UIEffect Effect{ get { return gameObject.GetComponentInChildren<UIEffect>(); } }
    public Animator AnimatorComponent { get { return gameObject.GetComponentInChildren<Animator>(); } }
    public void InvokeDown() { /*ClickDownSound();*/  callbackDown.Invoke(this);  }
    public void InvokeUp() { callbackUp.Invoke( this ); }

    public UnityAction<UIButton>  callbackEnter { get; set; } = delegate (UIButton btn) { };
    public UnityAction<UIButton> callbackExit { get; set; } = delegate (UIButton btn) { };
    public UnityAction<UIButton> callbackDown { get; set; } = delegate (UIButton btn) { };
    public UnityAction<UIButton> callbackUp { get; set; } = delegate (UIButton btn) { };
    public UnityAction<UIButton> callbackClick { get; set; } = delegate (UIButton btn) { };

    public float clickDelay = 2.0f;

    void Awake()
    {
        IsEnable = true;
        IsPointed = false;
    }
    public void TimeDelayReset()
    {
        TimeDown = Time.time;
    }
    
    public void OnPointerUp(PointerEventData evdata)
    {
        
        if (IsEnable == false)
            return;
                
        EventData = evdata;
        callbackUp(this);
    }
    public void OnPointerDown(PointerEventData evdata)
    {        
        if (IsEnable == false)
            return;

        if ( Time.time < TimeDown + DownDelay)
            return;

        ++CountDown;        
           
        TimeDown = Time.time;
        EventData = evdata;
        callbackDown(this);
    }
    public void ClearCountDown()
    {
        CountDown = 0;
    }
    public void OnPointerClick(PointerEventData evdata)
    {
        if (IsEnable == false)
            return;

        //ClickDownSound();
        TimeDown = Time.time;
        EventData = evdata;
        callbackClick(this);
    }
    public void ResetTimeDelay()
    {
        TimeDown = 0.0f;
    }
    
    public void OnPointerEnter(PointerEventData evdata)
    {
        IsPointed = true;
        EventData = evdata;
        callbackEnter(this);
    }
    public void OnPointerExit(PointerEventData evdata)
    {
        IsPointed = false;
        EventData = evdata;
        callbackExit(this);
    }

    private void OnEnable()
    {
        IsPointed = false;
        TimeDown = Time.time - clickDelay;
    }

    bool lastbtnstate = false;
    private void Update()
    {

        //if( IsPointed == true)
        //{
        //    if(lastbtnstate == false  && WinInput.isLebtn() == true )
        //    {
        //        if(IsEnable == true )
        //            if (TimeDown + clickDelay < Time.time)
        //            {
        //                ++CountDown;

        //                TimeDown = Time.time;
        //                callbackDown(this);

        //                Debug.Log("UIBtn Click!!!");
        //            }
        //    }
        //    lastbtnstate = WinInput.isLebtn();
        //}
    }
}
