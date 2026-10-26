using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class ExtensionClass
{
    public static bool IsIn(this LayerMask layer, LayerMask maskOhter)
    {
        return (layer & 1 << layer) != 0;
    }
    public static void CrossFadeTransPlay(this Animator anim, string nameState)
    {   
        anim.CrossFade(nameState, 0.2f, -1, 0.0f);
    }
    public static void CrossFadePlay(this Animator anim, string nameState)
    {
        anim.CrossFade(nameState, 0.1f, -1, 0.0f);
    }
    public static void CrossFadePlayShort(this Animator anim, string nameState)
    {
        anim.CrossFade(nameState, 0.02f, -1, 0.0f);
    }
    //public static void CrossShortFadePlay(this Animator anim, string nameState)
    //{
    //    anim.CrossFade(nameState, 0.02f, -1, 0.0f);
    //}
    public static void CrossShortFadePlay(this Animator anim, string nameState, int layer = -1)
    {
        anim.CrossFade(nameState, 0.02f, layer, 0.0f);
    }    
    public static void CPlay(this Animator anim, string nameState)
    {
        anim.Play(nameState, -1, 0.0f);
    }
    public static void CPlayTime(this Animator anim, string nameState)
    {
        anim.Play(nameState, -1, 0.1f);
    }
    public static bool IsProbailty(this int intData, float rate)
    {
        return intData < (int)(rate * 100);
    }
    public static float Clamp(this float value, float min, float max)
    {
        value = value < min ? min : value > max ? max : value;
        return value;
    }
    public static int Clamp(this int value, int min, int max)
    {
        value = value < min ? min : value > max ? max : value;
        return value;
    }
    public static int FloatMultyPly(this int value, float multyply)
    {
        var floatData = (float)value * multyply;
        return (int)floatData;
    }
    public static string GetModeName( this int mode )
    {   switch( mode )
        {   case 0: return "숙제하기";
            case 1: return "책읽기";
            case 2: return "집중하기";
            case 3: return "쉬기";
        }
        return "알수없는모드";
    }
    public static string GetDateString(this string mode)
    {
        string[] lists = mode.Split("-"[0]);
        return string.Format("{0}년 {1}월 {2}일 {3}시 {4}분",
            int.Parse(lists[0]),
            int.Parse(lists[1]),
            int.Parse(lists[2]),
            int.Parse(lists[3]),
            int.Parse(lists[4]));
    }
    public static string GetDateStringSimple(this string mode)
    {
        string[] lists = mode.Split("-"[0]);

        return string.Format("(시작일자:{1}월{2}일)",
            int.Parse(lists[0]),
            int.Parse(lists[1]),
            int.Parse(lists[2]));

        //return string.Format("(시작일자:{1}월{2}일 {3}:{4})",
        //    int.Parse(lists[0]),
        //    int.Parse(lists[1]),
        //    int.Parse(lists[2]),
        //    int.Parse(lists[3]),
        //    int.Parse(lists[4]));
    }
    public static void SetParentOrigin(this Transform root, Transform parentRoot )
    {
        root.SetParent( parentRoot );
        root.localPosition = Vector3.zero;
        root.localScale = Vector3.one;
        root.SetAsLastSibling();
    }
    public static void SetActive( this Animator anim, bool isAcitivty )
    {
        anim.gameObject.SetActive(isAcitivty);
    }
    public static void Suffle<T>(this List<T> value, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            var random1 = UnityEngine.Random.Range(0, value.Count / 2);
            var random2 = UnityEngine.Random.Range(value.Count / 2, value.Count);            
            if (random1 == random2) continue;

            T swap = value[random2];
            value[random2] = value[random1];
            value[random1] = swap;
        }
    }
    public static T RandomList<T>(this List<T> value)
    {
        return value[Random.Range(0, value.Count)];
    }
    public static T RotateOne<T>(this List<T> lists)
    {
        var value = lists[0];
        lists.RemoveAt(0);
        lists.Add(value);

        return value;
    }
    public static void Identity(this Transform value)
    {
        value.localPosition = Vector3.zero;
        value.localRotation = Quaternion.identity;
        value.localScale = Vector3.one;
    }

    public static bool isValid(this string msg)
    {
        return string.IsNullOrEmpty(msg) == false;
    }
    public static void SetSliderValue(this Slider slider, float min, float max, float start_ )
    {
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = start_;
    }
    public static bool IsUnityObjectAlive( UnityEngine.Object obj)
    {
        // Checks if the UnityEngine.Object is NOT null and not destroyed
        return obj != null && !obj.Equals(null);
    }

    public static void ClearChilds( this Transform parent )
    {
        var children = new List<GameObject>();

        foreach (Transform child in parent)
            children.Add(child.gameObject);

        foreach (var child in children)
            GameObject.DestroyImmediate(child);
    }

    public static double getDouble(this object value )
    {
        if (value is double)
            return (double)value;

        if (value is float)
            return (double)((float)value);

        if (value is int)
            return (double)((int)value);

        return double.MinValue;
    }
    public static float getFloat(this object value)
    {
        if (value is double)
            return (float)((double)value);

        if (value is float)
            return (float)value;

        if (value is int)
            return (float)((int)value);
                
        return float.Parse( value.ToString() );        
    }

    public static int getInt(this object value)
    {
        if (value is double)
            return (int)((double)value);

        if (value is float)
            return (int)((float)value);

        if (value is int)
            return (int)value;

        return int.MinValue;
    }

}





