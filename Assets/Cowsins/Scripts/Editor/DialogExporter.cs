using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace cowsins2D
{
    // 열린 씬의 DialogMenu 와 모든 ItemQuest 인스펙터 데이터를 id 별 JSON 파일로 내보낸다.
    //   Assets/Resources/Dialogs/intro.json / tutorial.json / end.json
    //   Assets/Resources/Dialogs/<ItemQuest.DialogId>.json
    //
    // 씬 전체를 훑는 이유: ItemQuest 의 대화는 프리팹이 아니라 씬 인스턴스마다
    // 오버라이드로 저장돼 있어서, 프리팹만 내보내면 실제 게임 대화가 누락된다.
    public static class DialogExporter
    {
        const string OutputDir = "Assets/Resources/" + DialogJsonLoader.DefaultFolder;

        [MenuItem("Tools/Dialog/Export Scene Dialogs -> JSON")]
        static void ExportSceneDialogs()
        {
            var dialogMenu = Object.FindAnyObjectByType<DialogMenu>(FindObjectsInactive.Include);
            if (dialogMenu == null)
            {
                EditorUtility.DisplayDialog("오류",
                    "씬에 DialogMenu 가 없습니다.\n게임 씬을 열고 다시 시도하세요.", "확인");
                return;
            }

            // 비활성 오브젝트도 포함한다. ItemQuest 는 퀘스트 진행도에 따라 꺼진 채 배치될 수 있다.
            var quests = Object.FindObjectsByType<ItemQuest>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (!ValidateDialogIds(quests, out string idError))
            {
                EditorUtility.DisplayDialog("dialogId 오류", idError, "확인");
                return;
            }

            // id -> 대화 목록. 파일 하나당 한 항목이 된다.
            var sections = new List<KeyValuePair<string, List<DialogForm>>>
            {
                Section(DialogMenu.IntroId, dialogMenu.Editor_Intro),
                Section(DialogMenu.TutorialId, dialogMenu.Editor_Tutorial),
                Section(DialogMenu.EndId, dialogMenu.Editor_End),
            };
            foreach (var quest in quests.OrderBy(q => q.DialogId))
                sections.Add(Section(quest.DialogId, quest.Editor_Dialog));

            Directory.CreateDirectory(OutputDir);

            var settings = DialogJsonLoader.CreateSettings();
            foreach (var section in sections)
            {
                string json = JsonConvert.SerializeObject(section.Value, settings);
                File.WriteAllText(FilePathOf(section.Key), json, new UTF8Encoding(true));
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("익스포트 완료", BuildReport(sections), "확인");
            Debug.Log($"[DialogExporter] {OutputDir} 에 {sections.Count}개 파일 저장 완료");
        }

        static KeyValuePair<string, List<DialogForm>> Section(string id, List<DialogForm> forms)
            => new KeyValuePair<string, List<DialogForm>>(id, forms ?? new List<DialogForm>());

        static string FilePathOf(string id) => $"{OutputDir}/{id}.json";

        // 비워두면 오브젝트 이름이 키로 쓰이지만, 그 상태에서 오브젝트를 rename 하면
        // JSON 매핑이 조용히 끊긴다. 현재 이름을 필드에 박아 두어 이름 변경과 무관하게 만든다.
        [MenuItem("Tools/Dialog/Assign dialogId from Object Name")]
        static void AssignDialogIdsFromObjectName()
        {
            var quests = Object.FindObjectsByType<ItemQuest>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int assigned = 0;
            foreach (var quest in quests)
            {
                if (quest.Editor_HasExplicitDialogId) continue;

                var so = new SerializedObject(quest);
                so.FindProperty("dialogId").stringValue = quest.gameObject.name;
                so.ApplyModifiedProperties();   // Undo 및 씬 dirty 처리를 함께 수행한다
                assigned++;
            }

            EditorUtility.DisplayDialog("dialogId 할당",
                assigned == 0
                    ? $"이미 모두 지정돼 있습니다. (ItemQuest {quests.Length}개)"
                    : $"{assigned}개에 dialogId 를 박았습니다.\n\n"
                      + string.Join("\n", quests.Select(q => $"  {q.DialogId}"))
                      + "\n\n씬을 저장하세요.",
                "확인");
        }

        // id 가 곧 파일 이름이 되므로 중복과 파일명 규칙을 함께 검사한다.
        // 중복이면 뒤엣것이 앞엣것의 파일을 덮어써 대화가 통째로 사라진다.
        static bool ValidateDialogIds(ItemQuest[] quests, out string error)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var invalid = quests
                .Where(q => q.DialogId.IndexOfAny(invalidChars) >= 0)
                .ToList();
            if (invalid.Count > 0)
            {
                error = "dialogId 에 파일 이름으로 쓸 수 없는 문자가 있습니다.\n"
                      + "오브젝트 이름을 바꾸거나 dialogId 를 직접 지정하세요.\n\n"
                      + string.Join("\n", invalid.Select(q => $"  [{q.DialogId}]  {GetScenePath(q.gameObject)}"));
                return false;
            }

            var duplicated = quests.GroupBy(q => q.DialogId).Where(g => g.Count() > 1).ToList();
            if (duplicated.Count > 0)
            {
                error = "dialogId 가 중복된 ItemQuest 가 있습니다.\n"
                      + "오브젝트 이름을 서로 다르게 하거나 dialogId 를 직접 지정하세요.\n\n"
                      + string.Join("\n\n", duplicated.Select(g =>
                            $"  [{g.Key}]\n" + string.Join("\n", g.Select(q => "    " + GetScenePath(q.gameObject)))));
                return false;
            }

            // 퀘스트 id 가 섹션 이름과 겹치면 서로의 파일을 덮어쓴다
            var reserved = new[] { DialogMenu.IntroId, DialogMenu.TutorialId, DialogMenu.EndId };
            var clashing = quests.Where(q => reserved.Contains(q.DialogId)).ToList();
            if (clashing.Count > 0)
            {
                error = $"dialogId 로 예약어({string.Join(", ", reserved)})를 쓸 수 없습니다.\n\n"
                      + string.Join("\n", clashing.Select(q => $"  [{q.DialogId}]  {GetScenePath(q.gameObject)}"));
                return false;
            }

            error = null;
            return true;
        }

        static string BuildReport(List<KeyValuePair<string, List<DialogForm>>> sections)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"저장 위치: {OutputDir}/");
            sb.AppendLine();

            foreach (var section in sections)
                sb.AppendLine($"  {section.Key}.json : {section.Value.Count}");

            var stale = FindStaleFiles(sections);
            if (stale.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[안내] 이번에 쓰이지 않은 파일이 폴더에 남아 있습니다.");
                sb.AppendLine("이름을 바꾸기 전의 파일이거나 예전 형식일 수 있습니다. 확인 후 지우세요.");
                sb.AppendLine();
                foreach (var name in stale) sb.AppendLine("  " + name);
            }

            var outside = CollectSpritesOutsideResources(sections);
            sb.AppendLine();
            if (outside.Count == 0)
            {
                sb.AppendLine("모든 스프라이트가 Resources 폴더 안에 있습니다.");
                return sb.ToString();
            }

            sb.AppendLine($"[중요] 스프라이트 {outside.Count}개가 Resources 폴더 밖에 있습니다.");
            sb.AppendLine("JSON 에 빈 값으로 기록됐으므로 런타임에 표시되지 않습니다.");
            sb.AppendLine();

            foreach (var entry in outside)
            {
                sb.AppendLine($"  {entry.AssetPath}");
                sb.AppendLine($"    스프라이트 : {entry.SpriteName}");
                foreach (var usage in entry.Usages)
                    sb.AppendLine($"    사용 위치  : {usage}");
                sb.AppendLine();
            }

            sb.AppendLine("Assets/Resources/ 아래로 옮기거나 해당 위치의 참조를 지운 뒤 다시 익스포트하세요.");
            sb.AppendLine("(Console 에도 출력했습니다. 클릭하면 해당 에셋이 선택됩니다.)");

            // 팝업은 길어지면 잘리므로 Console 에도 남긴다.
            // context 로 스프라이트를 넘기면 로그를 클릭했을 때 프로젝트 창에서 해당 에셋이 하이라이트된다.
            foreach (var entry in outside)
            {
                Debug.LogWarning(
                    $"[DialogExporter] Resources 밖 스프라이트: {entry.AssetPath} ({entry.SpriteName})\n"
                    + string.Join("\n", entry.Usages.Select(u => "    " + u)),
                    entry.Asset);
            }

            return sb.ToString();
        }

        // id 를 바꾸면 예전 이름의 파일이 그대로 남아 Resources 에 계속 포함된다.
        // 임의로 지우지는 않고 목록만 알려 준다.
        static List<string> FindStaleFiles(List<KeyValuePair<string, List<DialogForm>>> sections)
        {
            if (!Directory.Exists(OutputDir)) return new List<string>();

            var written = new HashSet<string>(sections.Select(s => s.Key));
            return Directory.GetFiles(OutputDir, "*.json")
                            .Select(Path.GetFileNameWithoutExtension)
                            .Where(name => !written.Contains(name))
                            .OrderBy(name => name)
                            .Select(name => name + ".json")
                            .ToList();
        }

        // 에셋 경로만으로는 대화의 어느 항목을 고쳐야 할지 알 수 없어 참조 위치까지 함께 모은다
        class SpriteUsage
        {
            public string AssetPath;
            public string SpriteName;
            public Sprite Asset;
            public List<string> Usages = new List<string>();
        }

        static List<SpriteUsage> CollectSpritesOutsideResources(
            List<KeyValuePair<string, List<DialogForm>>> sections)
        {
            var found = new Dictionary<Sprite, SpriteUsage>();
            foreach (var section in sections)
                Walk(section.Value, section.Key, found);

            return found.Values.OrderBy(e => e.AssetPath).ThenBy(e => e.SpriteName).ToList();
        }

        static void Walk(List<DialogForm> forms, string path, Dictionary<Sprite, SpriteUsage> into)
        {
            if (forms == null) return;

            for (int i = 0; i < forms.Count; i++)
            {
                var form = forms[i];
                if (form == null) continue;

                string at = $"{path}[{i}]";

                // 타입상 그려지지 않는 초상화는 JSON 에도 나가지 않으므로 경고하지 않는다
                if (form.UsesPortrait())
                    Check(form.spritePortrait, $"{at}.spritePortrait", into);

                Check(form.spriteDescript, $"{at}.spriteDescript", into);

                if (form.choices == null) continue;
                for (int j = 0; j < form.choices.Count; j++)
                {
                    var choice = form.choices[j];
                    if (choice == null) continue;

                    string choiceAt = $"{at}.choices[{j}]";
                    Check(choice.sprite, $"{choiceAt}.sprite", into);
                    Check(choice.aftersprite, $"{choiceAt}.aftersprite", into);
                    Walk(choice.afterDialogs, $"{choiceAt}.afterDialogs", into);
                }
            }
        }

        static void Check(Sprite sprite, string usage, Dictionary<Sprite, SpriteUsage> into)
        {
            if (sprite == null || SpriteJsonConverter.IsInsideResources(sprite)) return;

            if (!into.TryGetValue(sprite, out var entry))
            {
                entry = new SpriteUsage
                {
                    AssetPath = AssetDatabase.GetAssetPath(sprite),
                    SpriteName = sprite.name,
                    Asset = sprite,
                };
                into[sprite] = entry;
            }
            entry.Usages.Add(usage);
        }

        static string GetScenePath(GameObject go)
        {
            var path = go.name;
            for (var t = go.transform.parent; t != null; t = t.parent)
                path = t.name + "/" + path;
            return path;
        }
    }
}
