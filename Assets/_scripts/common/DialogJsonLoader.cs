using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace cowsins2D
{
    // 대화는 id 하나당 파일 하나로 나뉘어 있다: Assets/Resources/Dialogs/<id>.json
    // 파일 내용은 DialogForm 배열이며, id 는 ItemQuest.DialogId 또는 DialogMenu 의 섹션 이름이다.
    public static class DialogJsonLoader
    {
        public const string DefaultFolder = "Dialogs";

        // 같은 파일을 여러 인스턴스가 반복 파싱하지 않도록 캐시한다.
        // 실패(파일 없음·파싱 오류)도 함께 기억해 같은 경고가 반복되지 않게 한다.
        static readonly Dictionary<string, List<DialogForm>> cache =
            new Dictionary<string, List<DialogForm>>();

        public static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
            };
            settings.Converters.Add(new SpriteJsonConverter());
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }

        public static string ResourcePathOf(string id, string folder = DefaultFolder)
            => string.IsNullOrEmpty(folder) ? id : folder + "/" + id;

        // 해당 id 의 대화를 반환한다. 파일이 없거나 비어 있으면 null
        // (호출부가 인스펙터 값으로 대체할 수 있도록).
        public static List<DialogForm> Load(string id, string folder = DefaultFolder)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            string path = ResourcePathOf(id, folder);
            if (cache.TryGetValue(path, out var hit)) return hit;

            List<DialogForm> result = null;
            var ta = Resources.Load<TextAsset>(path);

            if (ta == null)
            {
                Debug.LogWarning($"[DialogJsonLoader] 대화 파일이 없습니다: Resources/{path}");
            }
            else
            {
                try
                {
                    result = JsonConvert.DeserializeObject<List<DialogForm>>(ta.text, CreateSettings());
                }
                catch (JsonException e)
                {
                    Debug.LogError($"[DialogJsonLoader] JSON 파싱 실패: Resources/{path}\n{e.Message}");
                }
            }

            cache[path] = result;
            return result;
        }

        // 도메인 리로드를 끈 에디터 환경에서 이전 플레이의 캐시가 남는 것을 막는다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearCache() => cache.Clear();
    }
}
