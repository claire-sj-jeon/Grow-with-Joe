
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace cowsins2D
{
    public class DialogChoiceOption : MonoBehaviour
    {
        [SerializeField] UIButton btn;
        [SerializeField] UiObject image;
        [SerializeField] UiObject caption;
        public TypeAnswer answerType { get; set; }

        public bool Activity
        {   get { return gameObject.activeSelf; }
            set { gameObject.SetActive(value); }
        }
        public bool Enable
        {   get { return btn.IsEnable; }
            set { btn.IsEnable = value; }
        }

        public void Hide()
        {
            btn.Effect.ReverseAlphaEffect();
        }

        public void SetAnswerType( 
            Sprite sprite, 
            string captiontext, 
            TypeAnswer ansType, 
            UnityAction<TypeAnswer> answerCallback )
        {
            image.image.sprite = sprite;
            image.image.SetNativeSize();            
            caption.Text.text = captiontext;
            answerType = ansType;

            btn.callbackDown = (btn) =>
            {
                btn.Effect.ScaleEffect();
                RegisterFunction.SingleCall(0.5f, () => { answerCallback?.Invoke(answerType); });

                
            };
        }



    }
}

