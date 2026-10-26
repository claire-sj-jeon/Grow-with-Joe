
using System.Collections.Generic;
using UnityEngine;

namespace cowsins2D
{    
    public static class DialogJsonLoader
    {
        // resourcePath: Assets/Resources/ 기준 상대경로 (확장자 제외)
        // 예) "Dialogs/dialogs"
        public static DialogJsonRoot Load(string resourcePath)
        {



            var ta = Resources.Load<TextAsset>(resourcePath);
            if (ta == null)
            {
                Debug.LogWarning($"[DialogJsonLoader] JSON 파일을 찾을 수 없습니다: Resources/{resourcePath}");
                return null;
            }

            var root = JsonUtility.FromJson<DialogJsonRoot>(ta.text);
            if (root == null)
                Debug.LogError($"[DialogJsonLoader] JSON 파싱 실패: Resources/{resourcePath}");

            return root;
        }

        public static List<DialogForm> ToDialogForms(List<DialogFormJson> jsonList)
        {
            var result = new List<DialogForm>();
            if (jsonList == null) return result;
            foreach (var j in jsonList)
                result.Add(ToDialogForm(j));
            return result;
        }

        static DialogForm ToDialogForm(DialogFormJson j)
        {
            var form = new DialogForm
            {
                type = ParseEnum<DialogForm.DialogType>(j.type),
                textCaption = j.textCaption,
                textName = j.textName,
                spritePortrait = LoadSprite(j.spritePortrait),
                spriteDescript = LoadSprite(j.spriteDescript),
                choices = new List<ChoiceOption>()
            };

            if (j.choices != null)
                foreach (var c in j.choices)
                    form.choices.Add(ToChoiceOption(c));

            return form;
        }

        static ChoiceOption ToChoiceOption(ChoiceOptionJson j)
        {
            return new ChoiceOption
            {
                text = j.text,
                afterText = j.afterText,
                aftersprite = LoadSprite(j.aftersprite),
                sprite = LoadSprite(j.sprite),
                answerType = ParseEnum<TypeAnswer>(j.answerType),
                afterDialogs = ToDialogForms(j.afterDialogs)
            };
        }

        // path: Assets/Resources/ 기준 상대경로 (확장자 제외)
        // 예) "Dialogs/Sprites/doctor_portrait"
        static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[DialogJsonLoader] 스프라이트를 찾을 수 없습니다: Resources/{path}");
            return sprite;
        }

        static T ParseEnum<T>(string value) where T : struct
        {
            if (System.Enum.TryParse<T>(value, out var result))
                return result;
            Debug.LogWarning($"[DialogJsonLoader] 알 수 없는 enum 값 '{value}' (타입: {typeof(T).Name})");
            return default;
        }
    }
}
