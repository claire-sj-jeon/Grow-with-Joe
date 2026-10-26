
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static cowsins2D.DialogForm;

namespace cowsins2D
{
    public enum TypeAnswer
    {
        Correct,
        Wrong,
        NotWrong,
    }

    [System.Serializable]
    public class ChoiceOption
    {
        public string text;
        public string afterText;
        public Sprite aftersprite;
        public Sprite sprite;
        public TypeAnswer answerType;
        public List<DialogForm> afterDialogs;
    }

    [System.Serializable]
    public class DialogForm
    {
        public enum DialogType
        {   
            PortraitAndText,
            TextOnly,
            QuestionWith3Choices,
            WideImageAndText,
            LastQuestionWith3Choices,
            WidePortraiText,
        }
        public DialogType type;
        [TextArea(5, 12)]
        public string textCaption;
        public string textName;
        public Sprite spritePortrait;
        public Sprite spriteDescript;
        public List<ChoiceOption> choices;

        // spritePortrait 는 초상화를 그리는 타입에서만 그려진다 (DialogTextBox.StepDialog 참고).
        // 다른 타입에 남아 있는 값은 인스펙터 편집 흔적일 뿐이므로 없는 것으로 취급한다.
        // 프로퍼티로 만들면 Json.NET 이 JSON 에 함께 써 버리므로 메서드로 둔다.
        public bool UsesPortrait() => type == DialogType.PortraitAndText
                                   || type == DialogType.WidePortraiText;

        // 와이드 이미지 패널을 켜는 타입
        public bool UsesWideImage() => type == DialogType.WideImageAndText
                                    || type == DialogType.WidePortraiText;

        // Json.NET 규약: 이 이름의 메서드가 false 를 반환하면 해당 필드를 직렬화하지 않는다.
        public bool ShouldSerializespritePortrait() => UsesPortrait();
    }

    public class DialogTextBox : MonoBehaviour
    {
        [SerializeField] UIEffect effectDim;

        [SerializeField] UIEffect root;

        [SerializeField] UiObject textNameBox_parent;
        [SerializeField] UiObject textNameBox;

        [SerializeField] UiObject portrain_parent;
        [SerializeField] UiObject prtrain_image;
        [SerializeField] UiObject portrain_textBox;

        [SerializeField] UiObject wideimage_parent;
        [SerializeField] UiObject wideimage_image;

        [SerializeField] UiObject normal_parent;
        [SerializeField] UiObject normal_textBox;

        [SerializeField] UIButton btnNext;
        [SerializeField] UIButton btnEnd;
        [SerializeField] AudioClip sndTyping;
        [SerializeField] float typeSpeed = 0.02f;
        [SerializeField] List<DialogChoiceOption> lists;

        [Header("엔딩 연출용 와이드 이미지 루프")]
        [SerializeField] List<Sprite> wideLoopSprites;
        [SerializeField] float wideLoopInterval = 0.15f;

        RegisterFunction.Element wideLoopTimer;

        // 대화창이 떠 있는 동안 Next / End / 선택지를 키보드로 고르고 누를 수 있게 등록한다
        void Awake()
        {
            UIKeyboardNavigator.RegisterScope(this, root.transform,
                () => root != null && root.gameObject.activeInHierarchy && root.Activity);
        }

        void OnDestroy()
        {
            UIKeyboardNavigator.UnregisterScope(this);
        }

        // 엔딩 대화를 만들 때 첫 프레임을 DialogForm.spriteDescript 로 넘기기 위한 접근자.
        public Sprite WideLoopFirstSprite =>
            wideLoopSprites != null && wideLoopSprites.Count > 0 ? wideLoopSprites[0] : null;

        // 스프라이트만 갈아 끼우면 되므로 커브 없이 인터벌 타이머로 돌린다.
        public void StartWideImageLoop()
        {
            StopWideImageLoop();

            if (wideLoopSprites == null || wideLoopSprites.Count == 0) return;

            SetWideImage(wideLoopSprites[0]);

            wideLoopTimer = RegisterFunction.instance.Base()
                .SetCategory("dialog")
                .SetTarget(wideLoopInterval)
                .SetRepeat(true)
                .SetCallbackComplete((element, count) =>
                    SetWideImage(wideLoopSprites[(count + 1) % wideLoopSprites.Count]));
        }

        public void StopWideImageLoop()
        {
            wideLoopTimer?.Remove();
            wideLoopTimer = null;
        }

        // portrain_parent 만 켜서는 부족하다. 그 아래 leftportrait 같은 중간 칸이나
        // 이미지 자체가 꺼져 있으면 화면에 안 나오므로 경로 전체를 켜 준다.
        void SetPortraitImage(Sprite sprite)
        {
            for (var t = prtrain_image.transform;
                 t != null && t.IsChildOf(portrain_parent.transform);
                 t = t.parent)
                t.gameObject.SetActive(true);

            var image = prtrain_image.image;

            // 빈 칸으로 조용히 넣어두면 원인을 찾기 어려워 경고를 남긴다.
            // GameObject 를 끄지 않고 Image 만 끈다. 꺼 놓으면 다음에 image 를 다시 찾지 못한다.
            if (sprite == null)
            {
                Debug.LogWarning("[DialogTextBox] 초상화 타입인데 spritePortrait 가 비어 있다");
                image.enabled = false;
                return;
            }

            image.enabled = true;
            image.sprite = sprite;
            image.GetComponent<AspectRatioFitter>().aspectRatio
                                = sprite.rect.width / sprite.rect.height;
            image.SetNativeSize();
        }

        void SetWideImage(Sprite sprite)
        {
            if (sprite == null) return;

            wideimage_image.image.sprite = sprite;
            wideimage_image.image.GetComponent<AspectRatioFitter>().aspectRatio
                                = sprite.rect.width / sprite.rect.height;
            wideimage_image.image.SetNativeSize();
        }

        public void HideChoice()
        {
            for (int i = 0; i < lists.Count; ++i)
                lists[i].Activity = false;
        }

        //IEnumerator TypeText(UiObject targetBox, string message, float typingSpeed)
        //{
        //    targetBox.Text.text = "";
        //    yield return new WaitForSeconds(0.01f);
        //    for (int i = 0; i < message.Length; i++)
        //    {
        //        targetBox.Text.text += message[i];
        //        yield return new WaitForSeconds(typingSpeed);
                
        //        if( i%2 == 0 )
        //            SoundManager.Instance.PlaySound(sndTyping, 0.4f);
        //    }
        //}

        //IEnumerator TypeText(DialogMenu menu, UiObject targetBox, string message, float typingSpeed)
        //{
        //    targetBox.Text.text = "";
        //    yield return new WaitForSeconds(0.01f);

        //    int visibleCharCount = 0;

        //    for (int i = 0; i < message.Length; i++)
        //    {
        //        // command 파싱
        //        if (message[i] == '@')
        //        {
        //            int commandStart = i;
        //            int commandEnd = i + 1;

        //            // 공백, 줄바꿈 전까지를 command로 간주
        //            while (commandEnd < message.Length &&
        //                   !char.IsWhiteSpace(message[commandEnd]))
        //            {
        //                commandEnd++;
        //            }

        //            string command = message.Substring(commandStart, commandEnd - commandStart);

        //            // command 처리
        //            //yield return HandleCommand(command, targetBox);

        //            menu.ExtraCommand(command, this);                    

        //            // command 부분은 출력하지 않고 건너뜀
        //            i = commandEnd - 1;
        //            continue;
        //        }

        //        // 일반 텍스트 출력
        //        targetBox.Text.text += message[i];
        //        visibleCharCount++;

        //        yield return new WaitForSeconds(typingSpeed);

        //        if (visibleCharCount % 2 == 0)
        //            SoundManager.Instance.PlaySound(sndTyping, 0.4f);
        //    }
        //}

        IEnumerator TypeText(DialogMenu menu, UiObject targetBox, string message, float typingSpeed)
        {
            targetBox.Text.text = "";
            yield return new WaitForSeconds(0.01f);

            int visibleCharCount = 0;

            for (int i = 0; i < message.Length; i++)
            {
                // 1. command 파싱 (@look 같은 것)
                if (message[i] == '@')
                {
                    int commandStart = i;
                    int commandEnd = i + 1;

                    while (commandEnd < message.Length &&
                           !char.IsWhiteSpace(message[commandEnd]))
                    {
                        commandEnd++;
                    }

                    string command = message.Substring(commandStart, commandEnd - commandStart);

                    menu.ExtraCommand(command, this);

                    i = commandEnd - 1;
                    continue;
                }

                // 2. RichText tag 파싱 (<color=orange>, </color>, <i> 등)
                if (message[i] == '<')
                {
                    int tagStart = i;
                    int tagEnd = i + 1;

                    while (tagEnd < message.Length && message[tagEnd] != '>')
                    {
                        tagEnd++;
                    }

                    // '>'까지 포함해서 tag 전체를 한 번에 추가
                    if (tagEnd < message.Length && message[tagEnd] == '>')
                    {
                        string tag = message.Substring(tagStart, tagEnd - tagStart + 1);
                        targetBox.Text.text += tag;

                        i = tagEnd;
                        continue;
                    }
                }

                // 3. 일반 텍스트 출력
                targetBox.Text.text += message[i];
                visibleCharCount++;

                yield return new WaitForSeconds(typingSpeed);

                if (visibleCharCount % 2 == 0)
                    SoundManager.Instance.PlaySound(sndTyping, 0.4f);
            }
        }


        IEnumerator HandleCommand(string command, UiObject targetBox)
        {
            switch (command.ToLower())
            {
                case "@look":
                    Debug.Log("LOOK 시작");
                    // 여기서 원하는 처리
                    // 예: 캐릭터 바라보기, 애니메이션 재생 등
                    break;

                case "@lookend":
                    Debug.Log("LOOK 종료");
                    // 여기서 원하는 처리
                    break;

                default:
                    Debug.LogWarning($"알 수 없는 command: {command}");
                    break;
            }

            yield break;
        }

        public void ShowEndButton()
        {
            btnNext.Activity = false;
            btnEnd.Activity = true;
            btnEnd.TimerAutoDown(6.0f);
        }
        public void ShowNextBtn(DialogMenu menu, bool isNext = true )
        {
            btnNext.Activity = true;
            btnEnd.Activity = false;            

            btnNext.callbackDown = (btn) =>
            {   btn.Effect.ScaleEffect();
                wideimage_parent.Activity = false;
                btn.Activity = false;

                if (isNext == true )
                    RegisterFunction.SingleCall(0.1f, () => menu.NextDialog(this));
            };

        }
        public void StepDialog(DialogForm form, DialogMenu menu )
        {
            // show dialogbox
            root.Alpha = 0.0f;
            root.Activity = true;
            root.AlphaEffect();
            
            ShowNextBtn(menu);


            portrain_parent.Activity = false;
            normal_parent.Activity = false;

            // 초상화와 텍스트 ========
            if (form.UsesPortrait())
            {
                portrain_parent.Activity = true;
                SetPortraitImage(form.spritePortrait);
                StopAllCoroutines();
                StartCoroutine(TypeText(menu, portrain_textBox, form.textCaption, typeSpeed));
            }
            else
            {   normal_parent.Activity = true;
                StopAllCoroutines();
                StartCoroutine(TypeText(menu, normal_textBox, form.textCaption, typeSpeed));
            }                

            

            // 이름 부분. =========================
            if ( string.IsNullOrEmpty( form.textName ) == false )
            {
                textNameBox_parent.Activity = true;
                textNameBox.Text.text = form.textName;
            }
            else
                textNameBox_parent.Activity = false;


            //  옵션 선택지 클리어 =========================
            for (int i = 0; i < lists.Count; ++i)
                lists[i].Activity = false;

            if (form.type == DialogType.QuestionWith3Choices ||
                form.type == DialogType.LastQuestionWith3Choices )
            {
                var choiceshuffle = form.choices;
                choiceshuffle.Suffle(6);

                for ( int i=0;i< choiceshuffle.Count; ++i)
                {
                    var choice = choiceshuffle[i];

                    lists[i].Activity = true;
                    lists[i].answerType = choice.answerType;
                    lists[i].SetAnswerType(
                        choice.sprite,
                        choice.text,
                        choice.answerType,
                        (answer) =>
                        {

                            if(choice.afterDialogs != null && choice.afterDialogs.Count > 0 )
                            {
                                var isLast = (form.type == DialogType.LastQuestionWith3Choices);

                                menu.SaveCurrentDialogPoint();
                                menu.ShowDialog(choice.afterDialogs);

                                for( int i=0;i< lists.Count;++i)

                                DialogMenu.actionDialogEnd = () =>
                                {
                                    menu.ReloadCurrentDialogPoint();
                                    //menu.VisibleDialog(true, false);
                                    Show();

                                    for (int i = 0; i < lists.Count; ++i)
                                        lists[i].Activity = true;

                                    portrain_parent.Activity = false;
                                    normal_parent.Activity = true;
                                    textNameBox_parent.Activity = false;
                                    btnNext.Activity = false;
                                    btnEnd.Activity = false;
                                    StartCoroutine(TypeText(menu, normal_textBox, form.textCaption, typeSpeed));

                                    DialogMenu.Instance.GetAnaswer(answer, this);
                                    
                                    if (isLast == true && answer == TypeAnswer.Correct )
                                        menu.GameClearDialog( this );

                                };

                                return;
                            }

                            if (choice.aftersprite != null)
                            {
                                wideimage_parent.Activity = true;
                                SetWideImage(choice.aftersprite);
                                ShowNextBtn(menu, false);
                            }

                            DialogMenu.Instance.GetAnaswer(answer, this);

                            if (form.type == DialogType.PortraitAndText)
                                StartCoroutine(
                                    TypeText(menu, portrain_textBox, choice.afterText, typeSpeed)
                                    );
                            else
                                StartCoroutine(
                                    TypeText(menu, normal_textBox, choice.afterText, typeSpeed)
                                    );
                        });
                }
                btnNext.Activity = false;
                btnEnd.Activity = false;
            }

            // 넓게 이미지가 나와서 설명을 도와주는 부분.
            wideimage_parent.Activity = false;
            if (form.UsesWideImage())
            {
                wideimage_parent.Activity = true;
                SetWideImage(form.spriteDescript);
            }

            btnNext.callbackDown = (btn) =>
            {
                btn.Effect.ScaleEffect();
                wideimage_parent.Activity = false;
                RegisterFunction.SingleCall(0.1f, () => menu.NextDialog(this));
            };
            btnEnd.callbackDown = (btn) =>
            {
                btn.Effect.ScaleEffect();
                wideimage_parent.Activity = false;

                if ( form.type == DialogType.LastQuestionWith3Choices)
                    RegisterFunction.SingleCall(0.1f, () => menu.GameClearDialog(this));
                else
                    RegisterFunction.SingleCall(0.1f, () => menu.EndDialog(this));
            };
        }

        public void Show()
        {
            root.Alpha = 1.0f;
            root.Activity = true;
        }
        public void Hide()
        {
            root.Alpha = 0.0f;
            root.Activity = false;

            // 대화창이 닫히면 루프가 계속 돌 이유가 없다
            StopWideImageLoop();
        }

    }
}


