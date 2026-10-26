using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace cowsins2D
{
    // Sprite 참조를 Resources 상대경로 문자열로 상호 변환한다.
    // 스프라이트 시트를 잘라낸 서브에셋은 파일 경로만으로 특정할 수 없어 "경로#스프라이트명" 형태로 기록한다.
    public class SpriteJsonConverter : JsonConverter<Sprite>
    {
        public const char SubAssetSeparator = '#';

        public override void WriteJson(JsonWriter writer, Sprite value, JsonSerializer serializer)
        {
            writer.WriteValue(value == null ? string.Empty : ToResourcesPath(value));
        }

        public override Sprite ReadJson(JsonReader reader, Type objectType, Sprite existingValue,
                                        bool hasExistingValue, JsonSerializer serializer)
        {
            return Load(reader.Value as string);
        }

        public static Sprite Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            int sep = path.IndexOf(SubAssetSeparator);
            if (sep < 0)
            {
                var single = Resources.Load<Sprite>(path);
                if (single == null)
                    Debug.LogWarning($"[SpriteJsonConverter] 스프라이트를 찾을 수 없습니다: Resources/{path}");
                return single;
            }

            string file = path.Substring(0, sep);
            string spriteName = path.Substring(sep + 1);

            foreach (var s in Resources.LoadAll<Sprite>(file))
                if (s.name == spriteName) return s;

            Debug.LogWarning($"[SpriteJsonConverter] 스프라이트를 찾을 수 없습니다: Resources/{file} 안의 '{spriteName}'");
            return null;
        }

#if UNITY_EDITOR
        // 에셋 경로 -> Resources 상대경로(확장자 제외). 에디터에서만 의미가 있다.
        public static string ToResourcesPath(Sprite sprite)
        {
            if (sprite == null) return string.Empty;

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;

            const string marker = "/Resources/";
            int idx = assetPath.IndexOf(marker, StringComparison.Ordinal);

            // Resources 밖이면 런타임에 Resources.Load 로 찾을 방법이 없다.
            // 그럴듯한 가짜 경로를 지어내면 조용히 실패하므로 빈 값으로 두고 익스포터가 목록을 경고한다.
            if (idx < 0) return string.Empty;

            string basePath = Path.ChangeExtension(assetPath.Substring(idx + marker.Length), null)
                                  .Replace('\\', '/');

            // 시트에서 잘라낸 서브에셋이면 파일명과 스프라이트명이 다르다
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            return sprite.name == fileName ? basePath : basePath + SubAssetSeparator + sprite.name;
        }

        public static bool IsInsideResources(Sprite sprite)
        {
            if (sprite == null) return true;
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            return assetPath.IndexOf("/Resources/", StringComparison.Ordinal) >= 0;
        }
#else
        // 런타임에서는 직렬화(쓰기)를 하지 않는다. AssetDatabase 가 없어 경로를 만들 수 없다.
        public static string ToResourcesPath(Sprite sprite) => string.Empty;
#endif
    }
}
