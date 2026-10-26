using System.Runtime.InteropServices;
using UnityEngine;
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

            // 엔딩 전용 버튼 3종은 위의 Pause() → ApplyMenuMode() 가 이미 감췄다
            SetActive(btnrResume, false);
            SetActive(btnrStart, true);
            SetActive(btnrTutorial, TutorialState == 0);

            // GameAgain 으로 들어온 판은 타이틀을 건너뛴다. Pause() 로 떠 있는 메뉴를 여기서 풀어 준다.
            if (autoStartOnLoad)
            {
                autoStartOnLoad = false;
                UnPause();
                DialogMenu.Instance.StartIntroduceDialog();
            }
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
                if (!menu.gameObject.activeSelf)
                {
                    menu.gameObject.SetActive(true);
                    menu.alpha = 0;
                }
                if (menu.alpha < 1) menu.alpha += Time.deltaTime * fadeSpeed;
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
        }

        void ApplyMenuMode()
        {
            bool sceneBehind = CurrentMode == MenuMode.SceneBehind;

            SetActive(btnrResume, !sceneBehind);
            SetActive(btnrStart, false);

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
