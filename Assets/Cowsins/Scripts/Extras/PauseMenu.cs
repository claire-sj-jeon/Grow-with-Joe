using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] Button btnrResources;
        
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
            Application.OpenURL("https://www.google.com");
        }

        private void Start()
        {
            playerControl = playerDependencies.PlayerControl;
            crosshair = playerDependencies.Crosshair;

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

            btnrResume.gameObject.SetActive(false);
            btnrStart.gameObject.SetActive(true);
            btnrTutorial.gameObject.SetActive(TutorialState == 0);
            btnrResources.gameObject.SetActive(true);

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
            if (!isPaused) Pause();
            else UnPause(); 
        }

        public void Pause()
        {
            isPaused = true;
            if(playerControl != null) playerControl.LoseControl();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //btnResume.Activity = true;
            //btnStart.Activity = false;

            btnrResume.gameObject.SetActive(true);
            btnrStart.gameObject.SetActive(false);

            crosshair.Hide(true);
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
