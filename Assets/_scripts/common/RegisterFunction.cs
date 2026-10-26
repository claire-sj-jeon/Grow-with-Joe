using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

public class RegisterFunction
{
    public static AnimationCurve _defaultEaseIn { get; private set; }
    public static AnimationCurve defaultEaseIn
    {   get
        {   if (_defaultEaseIn == null)
                _defaultEaseIn = AnimationCurve.EaseInOut(0, 0, 1, 1);
            return _defaultEaseIn;
        }        
    }

    public static AnimationCurve EaseInOut( float time, float value ) 
        => AnimationCurve.EaseInOut(0, 0, time, value );
    public static AnimationCurve Loop(float time, float value)
        => new AnimationCurve(
        new Keyframe(0, 0, 0, 0),
        new Keyframe(time * 0.5f, value, 0, 0),
        new Keyframe(time, 0, 0, 0)
    );
    public static AnimationCurve Linear( float time, float value) 
        => AnimationCurve.Linear(0, 0, time, value);    
    public static AnimationCurve EaseIn( float time, float value ) 
        => new AnimationCurve(
        new Keyframe(0, 0, 0, 2), // Start keyframe
        new Keyframe(time, value, 0, 0)  // End keyframe
    );
    public static AnimationCurve EaseOut(float time, float value) 
        => new AnimationCurve(
        new Keyframe(0, 0, 2, 0), // Start keyframe
        new Keyframe(time, value, 0, 0)  // End keyframe
    );
    public static AnimationCurve Bounce( float time, float value ) 
        => new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(time * 0.25f, value),
        new Keyframe(time * 0.5f, value * 0.5f),
        new Keyframe(time * 0.75f, value),
        new Keyframe(time, value)
    );
    public class Element
    {
        public int id { get; private set; }
        public int category { get; set; }
        public bool isRepeat { get; private set; }
        public AnimationCurve curve { get; private set; }
        public bool isActive { get; private set; }
        public float multiply { get; private set; }
        public int count { get; private set; }
        public float time { get; set; }
        public float target{ get; private set; }
        public UnityAction<Element, float> callback { get; set; }
        public UnityAction<Element, int> callbackComplete { get; set; }
        public UnityAction<float> update { get; private set; }
        public bool isPause { get; set; }        
        public RegisterFunction manager { get; private set; }
        Element Ra( UnityAction action) { action(); return this; }
        public Element SetMultiply(float m) => 
            Ra(() => { multiply = m; } );
        public Element SetTarget(float t) => 
            Ra(() => { target = t; });
        public Element SetRepeat(bool r) =>
            Ra(() => { isRepeat = r; });
        public Element SetPause(bool ispause) => 
            Ra(() => { isPause = ispause; });
        public Element SetCategory(string category_ = "default") => 
            Ra(() => {  category = (category_ ?? "default").GetHashCode(); });
        public Element SetTime(float value ) =>
            Ra(() => { time = value; });
        public Element SetCurve(AnimationCurve curve_) => 
            Ra(() => { curve = curve_; });
        public Element SetCallback(UnityAction<Element, float> curve_) => 
            Ra(() => { callback = curve_ ?? ((ele, t) => { }); });
        public Element SetCallbackComplete(UnityAction<Element, int> curve_) =>
            Ra(() => { callbackComplete = curve_ ?? ((ele, c) => { }); });
        public Element SetCall(UnityAction func ) =>
            Ra(() => { callback = (ele, t) => func(); });
        public Element SetCallComplete(UnityAction func) =>
            Ra(() => { callbackComplete = (ele, c) => func(); });


        


        public Element(
            RegisterFunction manager_, 
            int id_,             
            bool isRepeat_ = false, 
            float target_ = default,
            AnimationCurve curve_ = default,
            UnityAction<Element, float> callback_ = default,
            UnityAction<Element, int> callbackComplete_ = default,
            UnityAction call_ = default,
            UnityAction callComplete_ = default,
            float multi_ = 1.0f,
            string category_ = default
            )
        {
            manager = manager_;
            id = id_;
            isActive = true;
            time = 0.0f;            
            count = 0;            
            
            SetRepeat(isRepeat_);
            SetCurve(curve_);
            SetCategory(category_);
            SetCurve(curve_);
            SetCallback(callback_);
            SetCallbackComplete(callbackComplete_);
            SetTarget(target_);
            SetMultiply(multi_);

            if( curve != null ) update = UpdateCurve;
            else update = UpdateTime;

            if (call_ != null) SetCall(call_);
            if (callComplete_ != null) SetCallComplete(callComplete_);            
        }
        public void Remove()
        {
            update = (t) => { };
            manager.Remove(this);
        }
        public void Clear()
        {
            update = null;
            callback = null;
            callbackComplete = null;
        }
        public void UpdateCurve(float deltaTime)
        {
            if (!isPause) time += deltaTime * multiply;

            if (curve.keys[curve.keys.Length - 1].time < time)
            {
                var value = curve.Evaluate(time);
                callback?.Invoke(this, value );
                callbackComplete?.Invoke(this, count++);
                Reset();
            }
            else
            {   
                var value = curve.Evaluate(time);
                callback?.Invoke(this, value);
            }
        }
        public void UpdateTime(float deltaTime)
        {
            if (!isPause) time += deltaTime * multiply;

            if (time > target)
            {   callbackComplete?.Invoke(this, count++);
                Reset();
            }
            else
                callback?.Invoke(this, time );
        }

        public void Reset()
        {
            if (!isRepeat) Remove();
            else time = 0.0f;
        }
    }

    public static RegisterFunction _instance;
    public static RegisterFunction instance 
    {   get 
        {
            if( _instance == null )
                _instance = new RegisterFunction();    

            return _instance; 
        }
    }    
    public bool IsClearReserved { get; private set; }
    public static int currentGenerateId { get; private set; }

    MonoActionObject monoObject;
    public static Element SingleCall(float time, UnityAction callback) => instance.CallSingle(callback, time);
    public static Element SingleCall(float time, UnityAction callback, string category) => instance.CallSingle(callback, time, category);
    public static void Remove(string name) => instance.RemoveCategory(name);

    public static int GetId() => ++currentGenerateId;

    List<Element> listElements = new List<Element>();
    List<Element> listAdd = new List<Element>();
    List<Element> listRemoves = new List<Element>();

    public void Destroy()
    {
        _instance = null;
    }
    RegisterFunction()
    {
        currentGenerateId = 0;
        monoObject = (new GameObject("RegisterFunction")).AddComponent<MonoActionObject>();
        monoObject.actionUpdate = Update;        
        monoObject.actionLateUpdate = LateUpdate;
        monoObject.actionDestroy = Destroy;
    }
    public Element Single(AnimationCurve curve_, 
        UnityAction<Element, float> callback_, 
        UnityAction<Element, int> callbackComplete_, 
        string cateogry = default )
    {
        var newElement  
            = new Element(
            this,
            GetId(),
            category_ : cateogry,
            isRepeat_: false,             
            curve_: curve_, 
            callback_: callback_, 
            callbackComplete_: callbackComplete_);            

        listAdd.Add(newElement);
        return newElement;
    }
    public Element Repeat(AnimationCurve curve_, 
        UnityAction<Element, float> callback_, 
        UnityAction<Element, int> callbackComplete_, 
        string cateogry = default)
    {
        var newElement
            = new Element(
            this,
            GetId(),
            category_: cateogry,
            isRepeat_: true,
            curve_: curve_,
            callback_: callback_,
            callbackComplete_: callbackComplete_)
            {

            };
        listAdd.Add(newElement);
        return newElement;
    }
    public Element Base()
    {
        var newElement
            = new Element(
            this,
            GetId()
            );

        listAdd.Add(newElement);
        return newElement;
    }
    //public Element CallSingle(UnityAction callback_, float target_ )
    //{
    //    var newElement
    //        = new Element(
    //        this,
    //        GetId(),
    //        target_ : target_,
    //        isRepeat_: false,
    //        callComplete_: callback_            
    //        );

    //    listAdd.Add(newElement);
    //    return newElement;
    //}
    public Element CallSingle(UnityAction callback_, float target_, string cateogry ="default" )
    {
        var newElement
            = new Element(
            this,
            GetId(),
            target_: target_,
            isRepeat_: false,
            category_: cateogry,
            callComplete_: callback_
            );

        listAdd.Add(newElement);
        return newElement;
    }

    

    public Element CallRepeat(UnityAction callback_, float target_ )
    {
        var newElement
            = new Element(
            this,
            GetId(),
            target_: target_,
            isRepeat_: false,
            callComplete_: callback_
            );

        listAdd.Add(newElement);
        return newElement;
    }
    public void RemoveCategory(string name)
    {
        foreach (var listOne in listElements)
            if (listOne.category == name.GetHashCode() )
                Remove(listOne);
    }
    public void Remove(Element target)
    {
        if (target == null) return;
        listRemoves.Add(target);
    }
    public void Clear()
    {
        IsClearReserved = true;
    }
    void Update( float delta )
    {
        CleanUp();

        foreach (var listOne in listElements)
            listOne.update(delta);
        AddUp();
    }
    void LateUpdate(float delta)
    {
        if (IsClearReserved == true)
        {   listElements.Clear();
            listAdd.Clear();
            listRemoves.Clear();
            IsClearReserved = false;
            Debug.Log("Clear ");
        }
    }
    void AddUp()
    {
        listElements.AddRange(listAdd);
        listAdd.Clear();
    }
    void CleanUp()
    {
        for (int i = 0; i < listElements.Count; ++i)
            if (listElements[i].update == null)
                listRemoves.Add(listElements[i]);

        if (listRemoves.Count > 0)
            foreach (var listOne in listRemoves)
            {   listOne.Clear();
                listElements.RemoveAll(data => data == listOne);
            }

        listRemoves.Clear();
    }
}
