using UnityEngine;

public class AnimationStarter : MonoBehaviour
{
    public string animationName;
    public float startTime;
    public float speed;
    Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        animator = GetComponent<Animator>();
        animator.speed = speed;
        animator.Play(animationName, -1, startTime);
    }

}
