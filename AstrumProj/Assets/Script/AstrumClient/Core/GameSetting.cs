using UnityEngine;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 游戏设置配置类 - 用于在 Inspector 中配置游戏参数
    /// </summary>
    public class GameSetting : MonoBehaviour
    {
        [Header("应用程序设置")]
        [SerializeField] private int targetFrameRate = 60;
        
        [Header("日志设置")]
        [SerializeField] private bool enableDebugLog = false;
        
        [Header("逻辑线程设置")]
        [Tooltip("启用逻辑专用线程（提升性能，但增加调试难度）")]
        [SerializeField] private bool enableLogicThread = false;
        
        [Header("核心GameObject引用")]
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private GameObject stageRoot;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private UnityEngine.SceneManagement.SceneManager sceneManagerUnity;
        [SerializeField] private Astrum.Client.Behaviour.CoroutineRunner coroutineRunner;
        
        [Header("场景配置")]
        [Tooltip("单人模式进入的场景ID（场景索引号）")]
        [SerializeField] private int singlePlayerSceneId = 1;
        
        [Tooltip("联机模式进入的场景ID（场景索引号）")]
        [SerializeField] private int multiplayerSceneId = 2;
        
        // 公共属性访问器
        public int TargetFrameRate => targetFrameRate;
        public bool EnableDebugLog => enableDebugLog;
        public bool EnableLogicThread => enableLogicThread;
        
        /// <summary>
        /// 逻辑线程帧率（固定为 20 帧）
        /// </summary>
        public int LogicThreadTickRate => 20;
        
        // 核心GameObject访问器
        public GameObject UIRoot => uiRoot;
        public Canvas HUDCanvas => hudCanvas;
        public GameObject StageRoot => stageRoot;
        public Camera MainCamera => mainCamera;
        public Camera UICamera => uiCamera;
        public UnityEngine.SceneManagement.SceneManager SceneManagerUnity => sceneManagerUnity;
        public Astrum.Client.Behaviour.CoroutineRunner CoroutineRunner => coroutineRunner;
        
        // 场景ID访问器
        public int SinglePlayerSceneId => singlePlayerSceneId;
        public int MultiplayerSceneId => multiplayerSceneId;
        
        /// <summary>
        /// 根据游戏模式获取对应的场景ID
        /// </summary>
        /// <param name="isSinglePlayer">是否为单人模式</param>
        /// <returns>对应的场景ID</returns>
        public int GetSceneIdByMode(bool isSinglePlayer)
        {
            return isSinglePlayer ? singlePlayerSceneId : multiplayerSceneId;
        }
    }
}

