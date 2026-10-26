
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

        // 대화 파일이 들어 있는 폴더 (Assets/Resources/ 기준).
        // 파일은 폴더 안에 섹션 이름별로 하나씩 있다: intro.json / tutorial.json / end.json
        [SerializeField]
        private string dialogFolder = DialogJsonLoader.DefaultFolder;

        public const string IntroId = "intro";
        public const string TutorialId = "tutorial";
        public const string EndId = "end";

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

        [Header("엔딩 연출")]
        [Tooltip("엔딩에서 주인공이 걸어갈 목표 지점")]
        [SerializeField]
        GameObject endingMoveTarget;

        [Tooltip("이 거리(가로) 안으로 들어오면 도착으로 본다")]
        [SerializeField]
        float endingArriveDistance = 0.5f;

        [Tooltip("지형에 막혀 도달하지 못할 때 연출이 멈추지 않도록 하는 제한 시간(초)")]
        [SerializeField]
        float endingMoveTimeout = 15.0f;

        [Tooltip("이 시간(초) 동안 앞으로 나아가지 못하면 막힌 것으로 보고 점프한다")]
        [SerializeField]
        float endingStuckTime = 0.35f;

        [Tooltip("막힘 판정 기준. 이 거리보다 적게 움직였으면 제자리 걸음으로 본다")]
        [SerializeField]
        float endingStuckDistance = 0.05f;

        [Tooltip("엔딩 와이드 이미지 대화창에 표시할 문구")]
        [SerializeField, TextArea(3, 10)]
        string endingDialogText;

        IPlayerControl playerControl;
        Crosshair crosshair;
        UIController uiController;
        IPlayerStats playerState;

        public static UnityAction actionDialogEnd = () => { };

        // 씬에 카메라 컨트롤러가 하나뿐이라 인스펙터 연결은 여기에만 두고,
        // 연출이 필요한 쪽(ItemQuest 등)은 이 싱글턴을 통해 빌려 쓴다.
        public CameraController GameCamera => controllerCamera;

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

        // 해당 파일이 없거나 비어 있으면 인스펙터 값을 그대로 둔다.
        // JSON 이행이 끝나지 않은 상태에서도 동작하게 하기 위함이다.
        void LoadDialogsFromJson()
        {
            listDialogintro = LoadSection(IntroId, listDialogintro);
            listDialogtutorial = LoadSection(TutorialId, listDialogtutorial);
            listDialogEnd = LoadSection(EndId, listDialogEnd);

            Debug.Log($"[DialogMenu] JSON 로드 완료 - intro:{listDialogintro?.Count} tutorial:{listDialogtutorial?.Count} end:{listDialogEnd?.Count}");
        }

        List<DialogForm> LoadSection(string id, List<DialogForm> fallback)
        {
            var loaded = DialogJsonLoader.Load(id, dialogFolder);
            return loaded != null && loaded.Count > 0 ? loaded : fallback;
        }

        // 플레이를 멈추지 않고 JSON 수정을 바로 확인하기 위한 개발용 기능.
        // 캐시를 비운 뒤 DialogMenu 의 섹션과 씬의 모든 ItemQuest 를 다시 읽는다.
        //
        // 이미 열려 있는 대화창에는 반영되지 않는다. story 가 이전 리스트를 참조한 상태라
        // 다음번에 대화를 새로 열 때부터 새 내용이 나온다.
        public void ReloadAllDialogs()
        {
#if UNITY_EDITOR
            // 에디터 밖에서 파일을 고쳤다면 다시 임포트해야 Resources.Load 가 새 내용을 돌려준다
            UnityEditor.AssetDatabase.Refresh();
#endif
            DialogJsonLoader.ClearCache();
            LoadDialogsFromJson();

            // 개발용 경로라 FindObjectsByType 비용은 감수한다 (매 프레임 호출이 아님)
            var quests = FindObjectsByType<ItemQuest>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var quest in quests) quest.ReloadDialog();

            Debug.Log($"[DialogMenu] 대화 JSON 재로드 완료 - ItemQuest {quests.Length}개 포함");
        }

#if UNITY_EDITOR
        // 익스포터 전용 접근자. 필드명을 문자열로 참조하지 않게 해서
        // 필드 이름이 바뀌면 익스포터가 조용히 빈 값을 뱉는 대신 컴파일 에러가 나도록 한다.
        public List<DialogForm> Editor_Intro => listDialogintro;
        public List<DialogForm> Editor_Tutorial => listDialogtutorial;
        public List<DialogForm> Editor_End => listDialogEnd;
        public string Editor_DialogFolder => dialogFolder;
#endif

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

            // R : 대화 JSON 전체 재로드
            if (Keyboard.current.rKey.wasPressedThisFrame)
                ReloadAllDialogs();

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

            // 4 : 엔딩 연출 전체를 처음부터 실행 (퀘스트를 다 깨지 않고 확인하기 위한 개발용)
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                StartEndingSequence();
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

                case "@crest":
                    {
                        Debug.Log("crest 시작");
                        controllerCamera.ChangeCameraOffset(new Vector3(0.0f, 1.44f, -10.0f));
                        uiController.ShowCrest();                        
                        break;
                    }
                case "@crestend":
                    {
                        Debug.Log("crest 종료");
                        controllerCamera.RecoverCameraOffset();
                        uiController.HideCrest();

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

            RegisterFunction.SingleCall(1.0f, StartEndingSequence);
        }

        // 엔딩 연출 전체 흐름
        // 지정 지점까지 자동 이동 → 와이드 이미지 대화 → (다음 버튼) → 엔딩 영상 → 씬 비하인드 메뉴
        public void StartEndingSequence() => StartCoroutine(coEndingSequence());

        IEnumerator coEndingSequence()
        {
            PlayerControlEnable(false);

            yield return coMovePlayerToEndingTarget();
            yield return new WaitForSeconds(0.5f);

            ShowEndingWideDialog();
        }

        // 순간이동이 아니라 평소 이동 로직으로 걷게 한다.
        // 턱이나 벽에 막히면 점프로 넘고, 그래도 못 가는 지형이 있을 수 있어 제한 시간을 둔다.
        IEnumerator coMovePlayerToEndingTarget()
        {
            if (endingMoveTarget == null)
            {
                Debug.LogWarning("[DialogMenu] endingMoveTarget 이 비어 있어 자동 이동을 건너뛴다");
                yield break;
            }

            var movement = playerDependencies.PlayerMovement;
            var playerTransform = playerDependencies.transform;

            float elapsed = 0.0f;
            float stuckTimer = 0.0f;
            float lastX = playerTransform.position.x;

            while (elapsed < endingMoveTimeout)
            {
                float distance = endingMoveTarget.transform.position.x - playerTransform.position.x;
                if (Mathf.Abs(distance) <= endingArriveDistance) break;

                movement.SetScriptedMove(Mathf.Sign(distance));

                // 막힘 판정은 벽 감지(WallLeft/WallRight)가 아니라 실제 이동 거리로 한다.
                // 벽 감지는 위아래 레이가 모두 맞아야 성립해서 낮은 턱을 놓친다.
                if (Mathf.Abs(playerTransform.position.x - lastX) > endingStuckDistance)
                {
                    lastX = playerTransform.position.x;
                    stuckTimer = 0.0f;
                }
                else
                {
                    stuckTimer += Time.deltaTime;

                    // 공중에서는 다시 점프해도 소용없으므로 착지한 뒤에만 시도한다
                    if (stuckTimer >= endingStuckTime && movement.IsGrounded)
                    {
                        movement.RequestScriptedJump();
                        stuckTimer = 0.0f;
                        lastX = playerTransform.position.x;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            movement.ClearScriptedMove();
        }

        // 인스펙터에 입력한 문구와 DialogTextBox 의 스프라이트 목록으로 한 장짜리 대화를 즉석에서 만든다.
        // JSON 을 거치지 않는 이유는 이 대화가 엔딩 연출에만 쓰이는 일회성 연출이기 때문이다.
        void ShowEndingWideDialog()
        {
            var form = new DialogForm
            {
                type = DialogForm.DialogType.WideImageAndText,
                textCaption = endingDialogText,
                spriteDescript = Textbox.WideLoopFirstSprite,
            };

            actionDialogEnd = () =>
            {
                actionDialogEnd = () => { };
                Textbox.StopWideImageLoop();

                // NextDialog 가 조작을 돌려주므로 영상 재생 전에 다시 막는다
                PlayerControlEnable(false);
                StartCoroutine(EndingCutScene());
            };

            ShowDialog(new List<DialogForm> { form });
            Textbox.StartWideImageLoop();
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
            videoPlayer.Activity = false;

            // 씬을 다시 로드하지 않고, 뒤로 씬이 비치는 모드의 일시정지 메뉴를 띄운다
            PauseMenu.Instance.PauseSceneBehind();
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
 
