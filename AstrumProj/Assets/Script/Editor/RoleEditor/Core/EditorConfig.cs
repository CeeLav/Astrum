namespace Astrum.Editor.RoleEditor.Core
{
    /// <summary>
    /// 编辑器配置 - 存储路径、设置等常量
    /// </summary>
    public static class EditorConfig
    {
        // ===== 路径配置 =====
        
        public const string CONFIG_ROOT = "AstrumConfig/Tables/Datas";
        
        /// <summary>实体表路径</summary>
        public const string ENTITY_TABLE_PATH = CONFIG_ROOT + "/BaseUnit/#BaseUnitTable.csv";
        
        /// <summary>角色表路径</summary>
        public const string ROLE_TABLE_PATH = CONFIG_ROOT + "/Role/#RoleBaseTable.csv";
        
        /// <summary>技能表路径</summary>
        public const string SKILL_TABLE_PATH = CONFIG_ROOT + "/Skill/#SkillTable.csv";
        
        /// <summary>技能动作表路径</summary>
        public const string SKILL_ACTION_TABLE_PATH = CONFIG_ROOT + "/Skill/#SkillActionTable.csv";
        
        /// <summary>动作表路径</summary>
        public const string ACTION_TABLE_PATH = CONFIG_ROOT + "/BaseUnit/#ActionTable.csv";
        
        /// <summary>技能效果表路径</summary>
        public const string SKILL_EFFECT_TABLE_PATH = CONFIG_ROOT + "/Skill/#SkillEffectTable.csv";
        
        // ===== 资源路径配置 =====
        
        public const string MODEL_ROOT = "Assets/ArtRes/Models/Characters";
        public const string ANIMATION_ROOT = "Assets/ArtRes/Animations";
        public const string EFFECT_ROOT = "Assets/ArtRes/Effects";
        public const string SOUND_ROOT = "Assets/ArtRes/Sounds";

        /// <summary>木桩模板 ScriptableObject 路径</summary>
        public const string HitDummyTemplateAssetPath = "Assets/Script/Editor/RoleEditor/Data/SkillHitDummyTemplateCollection.asset";
        
        // ===== 编辑器设置 =====
        
        public const int AUTO_SAVE_INTERVAL = 300; // 5分钟
        public const int DEFAULT_FRAME_RATE = 60;
        public const bool ENABLE_AUTO_BACKUP = true;
        public const int BACKUP_KEEP_COUNT = 5;
        
        // ===== ID生成规则 =====
        
        public const int ENTITY_ID_START = 1000;
        public const int ROLE_ID_START = 1000;
        public const int SKILL_ID_START = 2000;
        public const int ACTION_ID_START = 3000;
        public const int EFFECT_ID_START = 4000;
        
        // ===== 验证规则 =====
        
        public const int MAX_NAME_LENGTH = 50;
        public const int MAX_DESCRIPTION_LENGTH = 500;
        public const float MIN_ATTRIBUTE_VALUE = 0f;
        public const float MAX_ATTRIBUTE_VALUE = 9999f;
    }
}

