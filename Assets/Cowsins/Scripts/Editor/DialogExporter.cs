
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace cowsins2D
{
    public class DialogExporter : EditorWindow
    {
        // 메뉴: Tools > Dialog > Export Inspector → JSON
        [MenuItem("Tools/Dialog/Export Inspector → JSON")]
        static void ExportDialogJson()
        {
            var dialogMenu = FindObjectOfType<DialogMenu>();
            if (dialogMenu == null)
            {
                EditorUtility.DisplayDialog("오류", "씬에 DialogMenu 오브젝트가 없습니다.\n게임 씬을 열고 다시 시도하세요.", "확인");
                return;
            }

            var so = new SerializedObject(dialogMenu);

            var root = new DialogJsonRoot
            {
                intro    = ExtractDialogForms(so.FindProperty("listDialogintro")),
                tutorial = ExtractDialogForms(so.FindProperty("listDialogtutorial")),
                end      = ExtractDialogForms(so.FindProperty("listDialogEnd"))
            };

            string json     = JsonUtility.ToJson(root, prettyPrint: true);
            string dirPath  = "Assets/Resources/Dialogs";
            string filePath = $"{dirPath}/dialogs.json";

            Directory.CreateDirectory(dirPath);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            AssetDatabase.Refresh();

            // 스프라이트 이동 안내 메시지 생성
            var notInResources = CollectSpritesNotInResources(so);
            string spriteWarning = notInResources.Count > 0
                ? $"\n\n[중요] 아래 스프라이트가 Resources 폴더 밖에 있습니다.\n" +
                  $"Assets/Resources/Dialogs/Sprites/ 로 이동한 뒤 게임을 실행하세요:\n\n" +
                  string.Join("\n", notInResources)
                : "\n\n모든 스프라이트가 Resources 폴더 안에 있습니다.";

            EditorUtility.DisplayDialog(
                "익스포트 완료",
                $"저장 위치: {filePath}{spriteWarning}",
                "확인"
            );
        }

        // ──────────────────────────────────────────────────
        //  SerializedProperty → JSON 데이터 변환
        // ──────────────────────────────────────────────────

        static List<DialogFormJson> ExtractDialogForms(SerializedProperty listProp)
        {
            var result = new List<DialogFormJson>();
            if (listProp == null) return result;

            for (int i = 0; i < listProp.arraySize; i++)
                result.Add(ExtractDialogForm(listProp.GetArrayElementAtIndex(i)));

            return result;
        }

        static DialogFormJson ExtractDialogForm(SerializedProperty prop)
        {
            return new DialogFormJson
            {
                type          = ((DialogForm.DialogType)prop.FindPropertyRelative("type").enumValueIndex).ToString(),
                textCaption   = prop.FindPropertyRelative("textCaption").stringValue,
                textName      = prop.FindPropertyRelative("textName").stringValue,
                spritePortrait = GetResourcesPath(prop.FindPropertyRelative("spritePortrait")),
                spriteDescript = GetResourcesPath(prop.FindPropertyRelative("spriteDescript")),
                choices        = ExtractChoices(prop.FindPropertyRelative("choices"))
            };
        }

        static List<ChoiceOptionJson> ExtractChoices(SerializedProperty listProp)
        {
            var result = new List<ChoiceOptionJson>();
            if (listProp == null) return result;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var e = listProp.GetArrayElementAtIndex(i);
                result.Add(new ChoiceOptionJson
                {
                    text        = e.FindPropertyRelative("text").stringValue,
                    afterText   = e.FindPropertyRelative("afterText").stringValue,
                    aftersprite = GetResourcesPath(e.FindPropertyRelative("aftersprite")),
                    sprite      = GetResourcesPath(e.FindPropertyRelative("sprite")),
                    answerType  = ((TypeAnswer)e.FindPropertyRelative("answerType").enumValueIndex).ToString(),
                    afterDialogs = ExtractDialogForms(e.FindPropertyRelative("afterDialogs"))
                });
            }
            return result;
        }

        // 스프라이트 에셋 경로 → Resources 상대 경로 변환
        // 에셋이 Resources 폴더 안에 있으면: "Dialogs/Sprites/filename"
        // 없으면 권장 이동 경로를 그대로 반환 (런타임에 못 찾음)
        static string GetResourcesPath(SerializedProperty spriteProp)
        {
            if (spriteProp == null || spriteProp.objectReferenceValue == null)
                return "";

            string assetPath = AssetDatabase.GetAssetPath(spriteProp.objectReferenceValue);
            const string marker = "/Resources/";
            int idx = assetPath.IndexOf(marker);

            if (idx >= 0)
            {
                // Resources 폴더 기준 상대경로 (확장자 제거)
                string relative = assetPath.Substring(idx + marker.Length);
                return Path.ChangeExtension(relative, null);
            }

            // Resources 밖 → 권장 이동 경로로 기록
            return "Dialogs/Sprites/" + Path.GetFileNameWithoutExtension(assetPath);
        }

        // ──────────────────────────────────────────────────
        //  스프라이트 경고 메시지용 목록
        // ──────────────────────────────────────────────────

        static List<string> CollectSpritesNotInResources(SerializedObject so)
        {
            var list = new List<string>();
            AddMissingSprites(so.FindProperty("listDialogintro"), list);
            AddMissingSprites(so.FindProperty("listDialogtutorial"), list);
            AddMissingSprites(so.FindProperty("listDialogEnd"), list);
            return list;
        }

        static void AddMissingSprites(SerializedProperty listProp, List<string> list)
        {
            if (listProp == null) return;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var form = listProp.GetArrayElementAtIndex(i);
                CheckSprite(form.FindPropertyRelative("spritePortrait"), list);
                CheckSprite(form.FindPropertyRelative("spriteDescript"), list);

                var choices = form.FindPropertyRelative("choices");
                if (choices == null) continue;

                for (int j = 0; j < choices.arraySize; j++)
                {
                    var c = choices.GetArrayElementAtIndex(j);
                    CheckSprite(c.FindPropertyRelative("sprite"), list);
                    CheckSprite(c.FindPropertyRelative("aftersprite"), list);
                }
            }
        }

        static void CheckSprite(SerializedProperty prop, List<string> list)
        {
            if (prop == null || prop.objectReferenceValue == null) return;
            string path = AssetDatabase.GetAssetPath(prop.objectReferenceValue);
            if (!path.Contains("/Resources/"))
                list.Add($"  {path}");
        }
    }
}
