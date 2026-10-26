using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiObject : MonoBehaviour
{
    public bool Activity
    {
        get { return gameObject.activeSelf; }
        set { gameObject.SetActive(value); }
    }
    public float alpha
    {
        get { return image.color.a; }
        set
        { var color = image.color;
            color.a = value;
            image.color = color;
        }
    }
    public float alphaCanvasGroup
    {
        get { return canvasGroup.alpha; }
        set { canvasGroup.alpha = value; }
    }
    public RawImage rawimage
    {
        get { return GetComponentInChildren<RawImage>(); }
    }
    public Image image
    {
        get { return GetComponentInChildren<Image>(); }
    }
    public TMPro.TextMeshProUGUI Text
    {
        get { return GetComponentInChildren<TMPro.TextMeshProUGUI>(); }
    }
    public CanvasGroup canvasGroup
    {
        get { return GetComponentInChildren<CanvasGroup>(); }
    }
    public float fillAmount
    {
        set { image.fillAmount = value; }
    }
    public bool particleActivity
    {
        set
        { if (particle != null)
                particle.gameObject.SetActive(value);
        }
    }
    public void TransScale(float time, float nextScale)
    {
        //Timer.Instance.
    }
    public ParticleSystem particle
    {
        get { return GetComponentInChildren<ParticleSystem>(true); }
    }
    public Animator animator
    {
        get { return GetComponent<Animator>(); }
    }
    public string lastAnimation { get; set; }
    public void animation(string animation )
    {
        var _animator = animator;
        
        if (_animator == null) 
            { lastAnimation = null; return; }

        _animator.Play(animation, -1, 0.0f);        
        lastAnimation = animation;
    }

}
