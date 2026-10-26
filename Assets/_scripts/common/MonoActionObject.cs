using UnityEngine;
using UnityEngine.Events;

public class MonoActionObject : MonoBehaviour
{
    public UnityAction actionStart = () => { };
    public UnityAction actionDestroy = () => { };
    public UnityAction<float> actionUpdate = (t) => { };
    public UnityAction<float> actionLateUpdate = (t) => { };

    float lastTime;
    float deltaTime;

    public bool activity
    {   get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    void Start() 
    {   actionStart(); 
        lastTime = Time.realtimeSinceStartup;
        Debug.Log($" start {lastTime}");
    }
    void Update() {
        deltaTime = Time.realtimeSinceStartup - lastTime;
        lastTime = Time.realtimeSinceStartup;        
        actionUpdate(deltaTime);    
    }
    private void LateUpdate() { actionLateUpdate(deltaTime); }

    private void OnDestroy()
    {
        actionDestroy();
    }
}
