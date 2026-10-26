
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
        }
        public DialogType type;
        [TextArea(5, 12)]
        public string textCaption;
        public string textName;
        public Sprite spritePortrait;
        public Sprite spriteDescript;
        public List<ChoiceOption> choices;        

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
            if (form.type == DialogType.PortraitAndText)
            {
                portrain_parent.Activity = true;
                prtrain_image.image.sprite = form.spritePortrait;
                prtrain_image.image.GetComponent<AspectRatioFitter>().aspectRatio 
                    = form.spritePortrait.rect.width / form.spritePortrait.rect.height;

                prtrain_image.image.SetNativeSize();
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
                                wideimage_image.image.sprite = choice.aftersprite;
                                wideimage_image.image.GetComponent<AspectRatioFitter>().aspectRatio
                                                    = choice.aftersprite.rect.width / choice.aftersprite.rect.height;

                                wideimage_image.image.SetNativeSize();
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
            if (form.type == DialogType.WideImageAndText)
            {
                wideimage_parent.Activity = true;
                wideimage_image.image.sprite = form.spriteDescript;
                wideimage_image.image.GetComponent<AspectRatioFitter>().aspectRatio
                                    = form.spriteDescript.rect.width / form.spriteDescript.rect.height;
                wideimage_image.image.SetNativeSize();
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
            
        }

    }
}


