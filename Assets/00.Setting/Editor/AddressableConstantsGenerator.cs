using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace Framework
{
    public static class AddressableConstantsGenerator
    {
        [MenuItem("Tools/Addressable")]
        public static void Generate()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if(settings == null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine($"//자동으로 생성되는 코드입니다.");
            sb.AppendLine($"//생성 날짜 : {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}\n\n");
            sb.AppendLine("public static class Address");
            sb.AppendLine("{");

            foreach (var group in settings.groups)
            {
                if (group == null || group.entries == null)
                    continue;

                foreach (var entry in group.entries)
                {
                    var assetPath = entry.AssetPath;
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        var guids = AssetDatabase.FindAssets("", new[] { assetPath });
                        foreach (var guid in guids)
                        {
                            var filePath = AssetDatabase.GUIDToAssetPath(guid);
                            if(AssetDatabase.IsValidFolder(filePath))
                                continue;

                            AppendConstant(sb, filePath);
                        }
                    }
                }
            }

            sb.AppendLine("}");

            var path = "Assets/02.Game/Scripts/Addressable/Address.cs";
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[Address Generated]");
        }

        private static void AppendConstant(StringBuilder sb, string assetPath)
        {
            var relativePath = assetPath;
            if (relativePath.StartsWith("Assets/01.Addressable/"))
            {
                relativePath = relativePath.Substring("Assets/01.Addressable/".Length);
            }
            else if (relativePath.StartsWith("Assets/"))
            {
                relativePath = relativePath.Substring("Assets/".Length);
            }

            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var folderName = Path.GetDirectoryName(relativePath)?.Replace("\\", "/");
            var keyName = $"{fileName}_{folderName}"
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .ToUpper();
            
            sb.AppendLine($"\tpublic const string {keyName} = \"{relativePath}\";");
        }
    }
}