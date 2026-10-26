using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace cowsins2D
{
    public class PauseMenu : MonoBehaviour
    {

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebEnterFullscreen();

    [DllImport("__Internal")]
    private static extern void WebExitFullscreen();
#endif



        public static PauseMenu Instance { get; private set; }

        public static bool isPaused { get; private set; }

        [SerializeField] private PlayerDependencies playerDependencies;

        [SerializeField] private CanvasGroup menu;

        [SerializeField] private float fadeSpeed;


        [SerializeField] Sprite sprTitle;
        [SerializeField] UiObject targetTitle;
        [SerializeField] UIButton btnResume;
        [SerializeField] UIButton btnStart;

        [SerializeField] Button btnrResume;
        [SerializeField] Button btnrStart;
        [SerializeField] Button btnrTutorial;

        // 엔딩 뒤 메뉴 전용 버튼 3종. 일반 모드에서는 셋 다 감춘다.
        [SerializeField] Button btnrResources;
        [FormerlySerializedAs("btnrSceneBehind")]
        [SerializeField] Button btnSceneBehind;
        [SerializeField] Button btnReturnToMain;

        [SerializeField, Tooltip("버튼을 누를 때 커지는 비율. 0 이면 연출하지 않는다.")]
        float pressScalePunch = 0.15f;

        [SerializeField, Tooltip("커졌다 원래 크기로 돌아오기까지의 시간(초).")]
        float pressScaleTime = 0.2f;

        // 버튼마다 진행 중인 연출을 하나만 유지한다. 연타로 곡선이 겹치면 스케일이 계속 커진다.
        readonly Dictionary<Button, RegisterFunction.Element> pressEffects
            = new Dictionary<Button, RegisterFunction.Element>();

        public enum MenuMode
        {
            Normal,
            SceneBehind,
        }

        // 엔딩 영상이 끝난 뒤의 메뉴 상태. SceneBehind 에서는 전용 버튼 3개만 남는다.
        public static MenuMode CurrentMode { get; private set; } = MenuMode.Normal;

        public static int TutorialState
        {
            get => PlayerPrefs.GetInt("istutorial", 0);
            set { PlayerPrefs.SetInt("istutorial", value); }
        }

        private IPlayerControl playerControl;
        private Crosshair crosshair;

        private void OnEnable()
        {
            playerDependencies.InputManager.onPause += TogglePause;
        }

        private void OnDisable()
        {
            playerDependencies.InputManager.onPause -= TogglePause;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }
        public void FullScreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebEnterFullscreen();
            Debug.Log("callFullScreen");
#endif
            UnPause();
        }
        public void FullAndStart()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebEnterFullscreen();
            Debug.Log("callFullScreen");
#endif
            UnPause();
            btnrTutorial.gameObject.SetActive(false);
            DialogMenu.Instance.StartIntroduceDialog();
        }

        public void TutorialStart()
        {
            TutorialState = 1;            
            FullAndStart();
            
        }
        public void GoResources()
        {
            //Application.OpenURL("https://www.google.com");
            DialogMenu.Instance.ResourcesCutScene();
        }

        // 씬을 다시 읽는 사이에 살아남아야 하는 재시작 예약이라 static 으로 둔다.
        static bool autoStartOnLoad;

        // 0 씬을 다시로드해서 처음부터. 타이틀을 거치지 않고 Start 버튼을 누른 것과 같은 상태로 들어간다.
        public void GameAgain()
        {
            autoStartOnLoad = true;
            ExitSceneBehindMode();
        }

        // 0 씬을 다시로드해서 처음부터. 이쪽은 타이틀 메뉴가 그대로 뜬다.
        public void ReturntoMainMode() => ExitSceneBehindMode();

        // SceneBehind 에서 빠져나오는 유일한 길. 씬 0 을 다시 읽어 처음 상태로 되돌린다.
        // CurrentMode 는 static 이지만 Start() 에서 Normal 로 초기화되므로 따로 풀어 줄 필요가 없다.
        public void ExitSceneBehindMode() => LoadScene(0);

        private void Start()
        {
            playerControl = playerDependencies.PlayerControl;
            crosshair = playerDependencies.Crosshair;

            // static 이라 씬을 다시 로드해도 남는다. 새 판은 항상 일반 모드로 시작한다.
            CurrentMode = MenuMode.Normal;

            isPaused = false;
            menu.gameObject.SetActive(false);
            menu.alpha = 0;
            //UnPause();
            Pause();

            btnStart.callbackDown = (btn) =>
            {
#if UNITY_WEBGL && !UNITY_EDITOR
               
                WebEnterFullscreen();
                Debug.Log("callFullScreen");
                        
#endif
                UnPause();
                DialogMenu.Instance.StartIntroduceDialog();

            };
            btnResume.callbackDown = (btn) =>
            {
#if UNITY_WEBGL && !UNITY_EDITOR

                WebEnterFullscreen();
                Debug.Log("callFullScreen");
#endif
                UnPause();

            };

            targetTitle.image.sprite = sprTitle;
            targetTitle.image.GetComponent<AspectRatioFitter>().aspectRatio = sprTitle.rect.width / sprTitle.rect.height;
            targetTitle.image.SetNativeSize();

            btnResume.Activity = false;
            btnStart.Activity = true;

            Debug.Log($"Tutorial State ==>{TutorialState} ");

            AddPressEffect(btnrResources);
            AddPressEffect(btnSceneBehind);
            AddPressEffect(btnReturnToMain);


            // 엔딩 전용 버튼 3종은 위의 Pause() → ApplyMenuMode() 가 이미 감췄다
            SetActive(btnrResume, false);
            SetActive(btnrStart, true);
            //SetActive(btnrTutorial, TutorialState == 0);
            SetActive(btnrTutorial, true);

            // GameAgain 으로 들어온 판은 타이틀을 건너뛴다. Pause() 로 떠 있는 메뉴를 여기서 풀어 준다.
            if (autoStartOnLoad)
            {
                autoStartOnLoad = false;
                UnPause();
                DialogMenu.Instance.StartIntroduceDialog();
            }
        }

        // Button 에는 눌린 순간을 알리는 이벤트가 없어 EventTrigger 를 코드로 붙인다.
        // onClick(손을 뗄 때)에 걸면 곧바로 씬을 읽는 버튼에서는 연출이 한 프레임도 보이지 않는다.
        void AddPressEffect(Button button)
        {
            if (button == null) return;

            var target = button.transform;

            // 연출 도중에 다시 읽으면 커진 값을 기준으로 잡으므로 눌리기 전 크기를 여기서 기억한다.
            var baseScale = target.localScale;

            var trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => PlayPressEffect(button, target, baseScale));
            trigger.triggers.Add(entry);
        }

        void PlayPressEffect(Button button, Transform target, Vector3 baseScale)
        {
            if (pressScalePunch <= 0.0f || pressScaleTime <= 0.0f) return;

            if (pressEffects.TryGetValue(button, out var running)) running?.Remove();

            pressEffects[button] = RegisterFunction.instance.Single(
                RegisterFunction.Loop(pressScaleTime, pressScalePunch),
                (element, value) => target.localScale = baseScale * (1.0f + value),
                (element, count) =>
                {   target.localScale = baseScale;
                    pressEffects.Remove(button);
                },
                "menuPress");
        }

        // btnReturnToMain 처럼 씬에서 아직 연결되지 않았을 수 있는 참조가 있어 널 가드를 둔다.
        static void SetActive(Button button, bool value)
        {
            if (button != null) button.gameObject.SetActive(value);
        }
        private void Update()
        {
            if (isPaused)
            {
                if (menu.alpha < 1)
                {
                    //menu.alpha += Time.deltaTime * fadeSpeed;
                    menu.alpha = 1.0f;
                }

                if (!menu.gameObject.activeSelf)
                {
                    menu.gameObject.SetActive(true);
                    menu.alpha = 0;
                }

            }
            else
            {
                menu.alpha -= Time.deltaTime * fadeSpeed;
                if (menu.alpha <= 0) menu.gameObject.SetActive(false);
            }
        }

        public void QuitGame() => Application.Quit();

        public void TogglePause()
        {
            // 엔딩이 끝난 상태라 돌아갈 게임이 없다. 일시정지 해제를 막는다.
            if (CurrentMode == MenuMode.SceneBehind) return;

            if (!isPaused) Pause();
            else UnPause();
        }

        // 엔딩 영상이 끝난 뒤 호출된다. 뒤로 씬이 비치고 두 버튼만 남는 모드로 메뉴를 연다.
        public void PauseSceneBehind()
        {
            CurrentMode = MenuMode.SceneBehind;
            Pause();
        }

        public void Pause()
        {
            isPaused = true;
            if(playerControl != null) playerControl.LoseControl();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //btnResume.Activity = true;
            //btnStart.Activity = false;

            ApplyMenuMode();

            crosshair.Hide(true);

            Debug.Log("==PauseMenuCall===");
        }

        void ApplyMenuMode()
        {
            bool sceneBehind = CurrentMode == MenuMode.SceneBehind;

            SetActive(btnrResume, !sceneBehind);
            SetActive(btnrStart, false);
            SetActive(btnrTutorial, true);

            // 이 3개는 SceneBehind 에서만 보인다
            SetActive(btnrResources, sceneBehind);
            SetActive(btnSceneBehind, sceneBehind);
            SetActive(btnReturnToMain, sceneBehind);

            if (sceneBehind) SetActive(btnrTutorial, false);
        }

        public void UnPause()
        {
            isPaused = false;
            if (playerControl != null) playerControl.CheckIfCanGrantControl();
            if (crosshair.CheckIfCanShow()) crosshair.Show();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }

        public void LoadScene(int id)
        {
            SceneManager.LoadScene(id);
        }
    }
}
