
using System.Collections.Generic;
using UnityEngine;
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

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }

        private void Start()
        {
            playerControl = playerDependencies.PlayerControl;
            uiController = playerDependencies._UIController;
            playerState = playerDependencies.PlayerStats;
            crosshair = playerDependencies.Crosshair;

            Textbox.Hide();
            root.Activity = false;
            root.Alpha = 0.0f;

            QuestCountManager.OnQuestChange += QuestChange;
            RegisterFunction.SingleCall(1.0f, () => { QuestCountManager.Instance.Reset(); });           
            
        }


        void QuestChange(int count)
        {
            Debug.Log($" Quest ==> {count} ");

            if (count == 0)
            {   isClear = false;
                ShowDialog(listDialogintro);
            }
            if (count == needclearCount)
            {  
                isClear = true;
                ShowDialog(listDialogEnd);
            }

            //if (needQuestCount <= QuestCountManager.Instance.questCount)
            //    if (activity == false)
            //        activity = true;
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

        public void ShowDialog(List<DialogForm> form)
        {
            story = form;
            storyIndex = 0; 
            root.Activity = true;
            root.Alpha = 0.0f;
            root.AlphaEffect();

            StepDialog(story[storyIndex]);

        }
        public void StepDialog(DialogForm form)
        {
            Textbox.Hide();
            Textbox.StepDialog(form, this);
        }
        public void EndDialog(DialogTextBox textBox)
        {
            textBox.Hide();

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
        }

    }
}
 