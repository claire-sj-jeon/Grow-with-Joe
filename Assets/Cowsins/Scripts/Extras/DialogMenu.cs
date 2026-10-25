
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace cowsins2D
{
    public class DialogMenu : MonoBehaviour
    {
        public static DialogMenu Instance { get; private set; }        

        [SerializeField] private PlayerDependencies playerDependencies;
        [SerializeField] private UIEffect root;

        [SerializeField] DialogTextBox Textbox;
        [SerializeField] AudioClip correctSfx;
        [SerializeField] AudioClip wrongSfx;
        [SerializeField] AudioClip bgm;

        [SerializeField] 
        private int needclearCount;
        [SerializeField]
        List<DialogForm> listDialogintro;
        [SerializeField]
        List<DialogForm> listDialogEnd;

        IPlayerControl playerControl;
        Crosshair crosshair;
        UIController uiController;
        IPlayerStats playerState;

        List<DialogForm> story { get; set; }
        int storyIndex;
        bool isClear = false;

        void Awake()
        {   
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }

        void Start()
        {
            playerControl = playerDependencies.PlayerControl;
            uiController = playerDependencies._UIController;
            playerState = playerDependencies.PlayerStats;
            crosshair = playerDependencies.Crosshair;

            Textbox.Hide();
            root.Activity = false;
            root.Alpha = 0.0f;


            SoundManager.Instance.PlayBgm(bgm, 0.5f);
            QuestCountManager.OnQuestChange += QuestChange;
            RegisterFunction.SingleCall(1.0f, () => { QuestCountManager.Instance.Reset(); });                       
        }

        void Update()
        {
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                var sceneName = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(sceneName);
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
            {   isClear = false;
                ShowDialog(listDialogintro);
            }

            // 모든 아이템을 다 모았을때 마지막 다이알로그 보여주기
            if (count == needclearCount)
            {  
                isClear = true;
                ShowDialog(listDialogEnd);
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

            if (isClear == true)
                RegisterFunction.SingleCall(1.0f, () => ShowDialog(listDialogEnd));                
        }
        public void GameClearDialog(DialogTextBox textBox)
        {
            textBox.Hide();

            RegisterFunction.SingleCall(1.0f, () =>            
            {
                var scenename = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(scenename);                
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
                PlayerControlEnable(true);
        }

    }
}
 