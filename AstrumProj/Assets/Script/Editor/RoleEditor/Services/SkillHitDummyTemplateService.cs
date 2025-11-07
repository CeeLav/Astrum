using Astrum.Editor.RoleEditor.Core;
using Astrum.Editor.RoleEditor.Data;
using UnityEditor;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 木桩模板集合加载与保存服务
    /// </summary>
    public static class SkillHitDummyTemplateService
    {
        private const string LastTemplatePrefKey = "Astrum.SkillEditor.LastHitDummyTemplate";

        private static SkillHitDummyTemplateCollection _cachedCollection;

        /// <summary>
        /// 获取模板集合，如不存在则创建默认资产
        /// </summary>
        public static SkillHitDummyTemplateCollection GetCollection(bool createIfMissing = true)
        {
            if (_cachedCollection != null)
            {
                return _cachedCollection;
            }

            _cachedCollection = AssetDatabase.LoadAssetAtPath<SkillHitDummyTemplateCollection>(EditorConfig.HitDummyTemplateAssetPath);

            if (_cachedCollection == null && createIfMissing)
            {
                EnsureTemplateFolder();

                _cachedCollection = SkillHitDummyTemplateCollection.CreateDefault();
                AssetDatabase.CreateAsset(_cachedCollection, EditorConfig.HitDummyTemplateAssetPath);
                AssetDatabase.SaveAssets();
            }

            _cachedCollection?.EnsureDefaultTemplates();
            return _cachedCollection;
        }

        /// <summary>
        /// 保存资产
        /// </summary>
        public static void SaveCollection()
        {
            if (_cachedCollection == null)
            {
                return;
            }

            EditorUtility.SetDirty(_cachedCollection);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 强制刷新缓存
        /// </summary>
        public static void Reload()
        {
            _cachedCollection = null;
            GetCollection();
        }

        public static string GetLastTemplateId()
        {
            return EditorPrefs.GetString(LastTemplatePrefKey, string.Empty);
        }

        public static void SetLastTemplateId(string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
            {
                EditorPrefs.DeleteKey(LastTemplatePrefKey);
            }
            else
            {
                EditorPrefs.SetString(LastTemplatePrefKey, templateId);
            }
        }

        private static void EnsureTemplateFolder()
        {
            string directory = EditorConfig.HitDummyTemplateAssetPath;
            int lastSlash = directory.LastIndexOf('/');
            if (lastSlash < 0)
            {
                return;
            }

            directory = directory.Substring(0, lastSlash);
            directory = directory.Replace("\\", "/");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            var parts = directory.Split('/');
            string current = string.Empty;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(part)) continue;

                if (string.IsNullOrEmpty(current))
                {
                    current = part;
                    continue;
                }

                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }
                current = next;
            }
        }
    }
}


