
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace cowsins2D
{
    public class DialogMenu : MonoBehaviour
    {

        
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebEnterFullscreen();

    [DllImport("__Internal")]
    private static extern void WebExitFullscreen();
#endif
        public static DialogMenu Instance { get; private set; }

        [SerializeField] private PlayerDependencies playerDependencies;
        [SerializeField] private UIEffect root;
        
        [SerializeField] DialogTextBox Textbox;
        [SerializeField] CameraController controllerCamera;
        [SerializeField] AudioClip correctSfx;
        [SerializeField] AudioClip wrongSfx;

        [SerializeField]
        private int needclearCount;

        // JSON 파일 경로 (Assets/Resources/ 기준, 확장자 제외)
        [SerializeField]
        private string dialogJsonPath = "Dialogs/dialogs";

        [SerializeField]
        List<DialogForm> listDialogintro;

        [SerializeField]
        List<DialogForm> listDialogtutorial;

        [SerializeField]
        List<DialogForm> listDialogEnd;

        [SerializeField]
        Animator wrongDino;

        [SerializeField]
        VideoRawImagePlayer videoPlayer;

        IPlayerControl playerControl;
        Crosshair crosshair;
        UIController uiController;
        IPlayerStats playerState;

        public static UnityAction actionDialogEnd = () => { };

        List<DialogForm> story { get; set; }
        int storyIndex;
        bool isClear = false;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }
        public void ShowDinoWrong( bool bis )
        {
            wrongDino.gameObject.SetActive(bis);

            if (bis == true)
                RegisterFunction.SingleCall(1.0f, () => { wrongDino.gameObject.SetActive(false); });

        }

        void LoadDialogsFromJson()
        {
            var data = DialogJsonLoader.Load(dialogJsonPath);
            if (data == null) return;

            if (data.intro != null && data.intro.Count > 0)
                listDialogintro = DialogJsonLoader.ToDialogForms(data.intro);

            if (data.tutorial != null && data.tutorial.Count > 0)
                listDialogtutorial = DialogJsonLoader.ToDialogForms(data.tutorial);

            if (data.end != null && data.end.Count > 0)
                listDialogEnd = DialogJsonLoader.ToDialogForms(data.end);

            Debug.Log($"[DialogMenu] JSON 로드 완료 - intro:{listDialogintro?.Count} tutorial:{listDialogtutorial?.Count} end:{listDialogEnd?.Count}");
        }

        void Start()
        {
            playerControl = playerDependencies.PlayerControl;
            uiController = playerDependencies._UIController;
            playerState = playerDependencies.PlayerStats;
            crosshair = playerDependencies.Crosshair;

            LoadDialogsFromJson();

            

            Textbox.Hide();
            videoPlayer.Hide();
            root.Activity = false;
            root.Alpha = 0.0f;
            ShowDinoWrong(false);

            QuestCountManager.OnQuestChange += QuestChange;
            RegisterFunction.SingleCall(0.1f, () => { QuestCountManager.Instance.Reset(); });
        }

        public void StartIntroduceDialog()
        {            

            RegisterFunction.SingleCall(1.0f, () =>
            {
                ShowDialog(listDialogintro);
                actionDialogEnd = () =>
                {

                    if( PauseMenu.TutorialState == 1 )
                        RegisterFunction.SingleCall(1.0f, 
                            () => 
                            { 
                                ShowDialog(listDialogtutorial);
                                actionDialogEnd = () => { };
                                PauseMenu.TutorialState = 2;
                            }
                        );
                };
            }
            );
        }
        void Update()
        {
#if UNITY_EDITOR
            // 개발용 단축키. 빌드에 포함되면 플레이어가 씬 리로드/컷신을 임의로 트리거할 수 있고,
            // 키보드가 없는 모바일·WebGL 환경에서는 Keyboard.current 가 null 이라 매 프레임 예외가 난다.
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                PauseMenu.TutorialState = 0;
                var sceneName = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(sceneName);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                //videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2FbabyjoeVideo.mp4?alt=media&token=8195f766-4c49-4d17-8a22-742af639fd8d");

                videoPlayer.Stop();
                videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2FlastScene.mp4?alt=media&token=b32f4f31-0855-4e5f-b881-29d9ff1e9f6e");

            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                //videoPlayer.Stop();
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {

                

                RegisterFunction.SingleCall(5.0f, () => ShowDialog(listDialogEnd));
                //videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fjoeplay.mp4?alt=media&token=67c9e8aa-0e33-4ba3-b99c-0b7be6f0942e");
            }
#endif
        }
        private void OnDestroy()
        {
            QuestCountManager.OnQuestChange -= QuestChange;            
        }

        void QuestChange(int count)
        {
            Debug.Log($" Quest ==> {count} ");

            //  처음 일때 인트로, 튜토리얼 보여주기 
            if (count == 0)
            {   
                isClear = false;                
                //ShowDialog(listDialogintro);
            }

            // 모든 아이템을 다 모았을때 마지막 다이알로그 보여주기
            //if (count == needclearCount)
            ////if (count == 1)
            //{  
            //    isClear = true;
            //    RegisterFunction.SingleCall(5.0f, () => ShowDialog(listDialogEnd));                
            //}
        }

        public void GetAnaswer(TypeAnswer answer, DialogTextBox textBox)
        {
            if (answer == TypeAnswer.Correct )
            {   
                textBox.HideChoice();
                textBox.ShowEndButton();
                textBox.Hide();
                playerState.Heal(1);
                SoundManager.Instance.PlaySound(correctSfx, 1);
                QuestCountManager.Instance.AddQuest(1);
            }
            if (answer == TypeAnswer.NotWrong)
            {                
                
            }
            if (answer == TypeAnswer.Wrong)
            {
                playerState.Damage(1);
                uiController.InvokeOnUpdateHealth();
                SoundManager.Instance.PlaySound(wrongSfx, 1);
                //ShowDinoWrong(true);
            }
        }

        public void ExtraCommand(string command, DialogTextBox textBox)
        {
            switch (command.ToLower())
            {
                case "@look":
                    {   Debug.Log("LOOK 시작");
                        controllerCamera.ChangeCameraOffset(new Vector3(0.0f,1.44f,-10.0f) ) ;
                        break;                    
                    }
                case "@lookend":
                    {
                        Debug.Log("LOOK 종료");
                        controllerCamera.RecoverCameraOffset();
                        break;
                    }
                default:
                    Debug.LogWarning($"알 수 없는 command: {command}");
                    break;
            }
        }
        public void VisibleDialog( bool bis, bool isanimation = true )
        {
            root.Activity = bis;
            root.Alpha = 1.0f;

            if( bis == true )
                if (isanimation == true)
                {
                    root.Alpha = 0.0f;
                    root.AlphaEffect();
                }
        }

         // 처음 대화창보여주기 시작.
        public void ShowDialog(List<DialogForm> form, bool bisStart = true)
        {
            story = form;
            storyIndex = 0;
            VisibleDialog(true, bisStart);            

            StepDialog(story[storyIndex]);
            PlayerControlEnable(false);            
        }

        List<DialogForm> saveDialog;
        int saveStoryIndex;

        public void SaveCurrentDialogPoint()
        {
            saveDialog = story;
            saveStoryIndex = storyIndex;
        }
        public void ReloadCurrentDialogPoint()
        {
            story = saveDialog;
            storyIndex = saveStoryIndex;
        }

        public void PlayerControlEnable( bool isEnable)
        {
            if (isEnable == true)
                playerControl.GrantControl();
            else
                playerControl.LoseControl();
        }
        public void StepDialog(DialogForm form)
        {
            Textbox.Hide();
            Textbox.StepDialog(form, this);
        }

        // 대화창 끝
        public void EndDialog(DialogTextBox textBox)
        {
            textBox.Hide();
            PlayerControlEnable(true);
            actionDialogEnd();

            if (isClear == true)
                RegisterFunction.SingleCall(1.0f, () => ShowDialog(listDialogEnd));                
        }
        public void GameClearDialog(DialogTextBox textBox)
        {
            textBox.Hide();

            RegisterFunction.SingleCall(1.0f, () =>            
            {
                StartCoroutine(EndingCutScene() );                
            }
            );
        }
        public void NextDialog(DialogTextBox textBox)
        {
            textBox.Hide();

            if (storyIndex + 1 < story.Count)
                RegisterFunction.SingleCall(0.1f, () =>
                {
                    StepDialog(story[++storyIndex]);
                });
            else
            {   PlayerControlEnable(true);
                actionDialogEnd();
            }
        }
        IEnumerator EndingCutScene()
        {
            yield return new WaitForSeconds(1.0f);

            videoPlayer.Activity = true;

        //https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2FlastScene.mp4?alt=media&token=b32f4f31-0855-4e5f-b881-29d9ff1e9f6e
        //https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2FbabyjoeVideo.mp4?alt=media&token=8195f766-4c49-4d17-8a22-742af639fd8d
        //https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fending.mp4?alt=media&token=39562907-a115-4430-b2da-9f0ad42fd99f
        //https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fending_sd.mp4?alt=media&token=21d36444-3cd1-41f5-9480-6786e0420055
        //https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fending_sd.mp4?alt=media&token=e47ce83a-74a8-4250-82f1-e2f8b017659a

        

            videoPlayer.Stop();                                 
            videoPlayer.PlayUrl("https://cdn.jsdelivr.net/gh/syjang120-maker/BabyJoeEndingCredit@main/ending%20credit.mp4");
            yield return new WaitForSeconds(6.0f);

            // 3초 후 또는 아무 입력 시 다음으로
            yield return WaitForSecondsOrAnyInput(30.0f);           
            videoPlayer.Stop();
            yield return new WaitForSeconds(2.0f);

            var scenename = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(scenename);
        }

        IEnumerator coResourcesCutScene()
        {
            yield return new WaitForSeconds(1.0f);
            videoPlayer.Activity = true;

            videoPlayer.Stop();
            //videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fresources.mp4?alt=media&token=46702710-064b-48c9-a375-832d1dc57e9d");
            videoPlayer.PlayUrl("https://cdn.jsdelivr.net/gh/syjang120-maker/BabyJoeResources@main/okokokokok.mp4");
        
            yield return new WaitForSeconds(6.0f);

            // 3초 후 또는 아무 입력 시 다음으로
            yield return WaitForSecondsOrAnyInput(30.0f);
            videoPlayer.Stop();
            yield return new WaitForSeconds(2.0f);
            videoPlayer.Activity = false;

        }

        public void ResourcesCutScene()
        {
            StartCoroutine(coResourcesCutScene());
        }

        IEnumerator WaitForSecondsOrAnyInput(float waitTime)
        {
            float timer = 0f;

            while (timer < waitTime)
            {
                if (IsAnyInputDetected())
                    yield break;

                if( videoPlayer.IsFinished == true )
                    yield break;

                timer += Time.deltaTime;
                yield return null;
            }
        }

        bool IsAnyInputDetected()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame ||
                    Mouse.current.middleButton.wasPressedThisFrame)
                    return true;
            }

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                        return true;
                }
            }

            return false;
        }

    }

}
 