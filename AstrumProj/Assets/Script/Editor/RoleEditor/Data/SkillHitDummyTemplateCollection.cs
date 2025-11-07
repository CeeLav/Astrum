using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrum.Editor.RoleEditor.Data
{
    /// <summary>
    /// 技能动作编辑器木桩模板集合
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillHitDummyTemplateCollection",
        menuName = "Astrum/Skill Editor/Hit Dummy Templates",
        order = 1000)]
    public class SkillHitDummyTemplateCollection : ScriptableObject
    {
        [SerializeField]
        private List<SkillHitDummyTemplate> templates = new List<SkillHitDummyTemplate>();

        public List<SkillHitDummyTemplate> Templates => templates;

        public void EnsureDefaultTemplates()
        {
            if (templates == null)
            {
                templates = new List<SkillHitDummyTemplate>();
            }

            if (templates.Count == 0)
            {
                templates.Add(SkillHitDummyTemplate.CreateDefault("默认木桩模板"));
            }
        }

        public void AddTemplate(SkillHitDummyTemplate template)
        {
            if (templates == null)
            {
                templates = new List<SkillHitDummyTemplate>();
            }

            templates.Add(template);
        }

        public void RemoveTemplate(SkillHitDummyTemplate template)
        {
            if (templates == null)
            {
                return;
            }

            templates.Remove(template);
        }

        public SkillHitDummyTemplate GetTemplateById(string templateId)
        {
            if (string.IsNullOrEmpty(templateId) || templates == null)
            {
                return null;
            }

            return templates.Find(t => t.TemplateId == templateId);
        }

        public static SkillHitDummyTemplateCollection CreateDefault()
        {
            var collection = CreateInstance<SkillHitDummyTemplateCollection>();
            collection.EnsureDefaultTemplates();
            return collection;
        }
    }

    [Serializable]
    public class SkillHitDummyTemplate
    {
        public string TemplateId = Guid.NewGuid().ToString("N");
        public string DisplayName = "木桩模板";
        public string Notes = string.Empty;
        public GameObject BasePrefab;
        public bool FollowAnchorPosition = false;
        public bool FollowAnchorRotation = false;
        public bool LockY = true;
        public Vector3 RootOffset = new Vector3(0f, 0f, 2f);
        public Vector3 RootRotation = Vector3.zero;
        public Vector3 RootScale = Vector3.one;
        public List<SkillHitDummyPlacement> Placements = new List<SkillHitDummyPlacement>();

        public static SkillHitDummyTemplate CreateDefault(string displayName)
        {
            var template = new SkillHitDummyTemplate
            {
                TemplateId = Guid.NewGuid().ToString("N"),
                DisplayName = displayName,
                Notes = "自动生成的默认木桩模板",
                FollowAnchorPosition = false,
                FollowAnchorRotation = false,
                LockY = true,
                RootOffset = new Vector3(0f, 0f, 2f),
                RootRotation = Vector3.zero,
                RootScale = Vector3.one
            };

            template.Placements.Add(SkillHitDummyPlacement.CreateDefault("默认木桩"));
            return template;
        }

        public SkillHitDummyTemplate DeepCopy(string suffix = " (复制)")
        {
            var clone = new SkillHitDummyTemplate
            {
                TemplateId = Guid.NewGuid().ToString("N"),
                DisplayName = DisplayName + suffix,
                Notes = Notes,
                BasePrefab = BasePrefab,
                FollowAnchorPosition = FollowAnchorPosition,
                FollowAnchorRotation = FollowAnchorRotation,
                LockY = LockY,
                RootOffset = RootOffset,
                RootRotation = RootRotation,
                RootScale = RootScale,
                Placements = new List<SkillHitDummyPlacement>(Placements.Count)
            };

            foreach (var placement in Placements)
            {
                clone.Placements.Add(placement?.DeepCopy() ?? SkillHitDummyPlacement.CreateDefault("木桩"));
            }

            return clone;
        }
    }

    [Serializable]
    public class SkillHitDummyPlacement
    {
        public string Name = "木桩";
        public GameObject OverridePrefab;
        public Vector3 Position = Vector3.zero;
        public Vector3 Rotation = Vector3.zero;
        public Vector3 Scale = Vector3.one;
        public Color DebugColor = new Color(0.58f, 0.37f, 0.22f, 1f);

        public static SkillHitDummyPlacement CreateDefault(string name)
        {
            return new SkillHitDummyPlacement
            {
                Name = name,
                OverridePrefab = null,
                Position = Vector3.zero,
                Rotation = Vector3.zero,
                Scale = Vector3.one,
                DebugColor = new Color(0.58f, 0.37f, 0.22f, 1f)
            };
        }

        public SkillHitDummyPlacement DeepCopy()
        {
            return new SkillHitDummyPlacement
            {
                Name = Name,
                OverridePrefab = OverridePrefab,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                DebugColor = DebugColor
            };
        }
    }
}


