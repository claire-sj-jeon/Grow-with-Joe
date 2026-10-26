
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
        [SerializeField] AudioClip correctSfx;
        [SerializeField] AudioClip wrongSfx;

        [SerializeField]
        private int needclearCount;
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

        void Start()
        {
            playerControl = playerDependencies.PlayerControl;
            uiController = playerDependencies._UIController;
            playerState = playerDependencies.PlayerStats;
            crosshair = playerDependencies.Crosshair;

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
                    RegisterFunction.SingleCall(1.0f, 
                        () => 
                        { 
                            ShowDialog(listDialogtutorial);
                            actionDialogEnd = () => { };
                        }
                        );
                };
            }
            );
        }
        void Update()
        {
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                var sceneName = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(sceneName);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2FbabyjoeVideo.mp4?alt=media&token=8195f766-4c49-4d17-8a22-742af639fd8d");
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                videoPlayer.Stop();
            }


            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fjoeplay.mp4?alt=media&token=67c9e8aa-0e33-4ba3-b99c-0b7be6f0942e");                
            }
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
            if (count == needclearCount)
            //if (count == 1)
            {  
                isClear = true;
                RegisterFunction.SingleCall(5.0f, () => ShowDialog(listDialogEnd));                
            }
        }

        public void GetAnaswer(TypeAnswer answer, DialogTextBox textBox)
        {
            if (answer == TypeAnswer.Correct )
            {   
                textBox.HideChoice();
                textBox.ShowEndButton();
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

                ShowDinoWrong(true);

            }
        }

         // 처음 대화창보여주기 시작.
        public void ShowDialog(List<DialogForm> form)
        {
            story = form;
            storyIndex = 0; 
            root.Activity = true;
            root.Alpha = 0.0f;
            root.AlphaEffect();

            StepDialog(story[storyIndex]);
            PlayerControlEnable(false);            
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

            videoPlayer.Stop();
            videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2FbabyjoeVideo.mp4?alt=media&token=8195f766-4c49-4d17-8a22-742af639fd8d");
            yield return new WaitForSeconds(5.0f);

            // 3초 후 또는 아무 입력 시 다음으로
            yield return WaitForSecondsOrAnyInput(10.0f);

            videoPlayer.Stop();

            yield return new WaitForSeconds(1.0f);

            videoPlayer.PlayUrl("https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fjoeplay.mp4?alt=media&token=67c9e8aa-0e33-4ba3-b99c-0b7be6f0942e");
            yield return new WaitForSeconds(5.0f);

            // 3초 후 또는 아무 입력 시 다음으로
            yield return WaitForSecondsOrAnyInput(10.0f);

            videoPlayer.Stop();

            var scenename = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(scenename);
        }

        IEnumerator WaitForSecondsOrAnyInput(float waitTime)
        {
            float timer = 0f;

            while (timer < waitTime)
            {
                if (IsAnyInputDetected())
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
 