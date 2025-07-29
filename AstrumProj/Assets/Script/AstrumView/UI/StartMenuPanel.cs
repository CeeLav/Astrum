using UnityEngine;
using UnityEngine.UI;
using Astrum.View.Stages;
using Astrum.CommonBase;

namespace Astrum.View.UI
{
    /// <summary>
    /// Start场景主菜单面板
    /// </summary>
    public class StartMenuPanel : UIPanel
    {
        private Button _startGameButton;
        private Button _settingsButton;
        private Button _exitButton;
        private Text _titleText;
        
        private const string GAME_SCENE_NAME = "Game";
        
        public StartMenuPanel() : base("StartMenuPanel")
        {
        }
        
        protected override void CreatePanelObject(Transform parent)
        {
            // 创建主面板对象
            _panelObject = new GameObject(_panelName);
            _panelObject.transform.SetParent(parent);
            
            // 添加RectTransform组件
            _rectTransform = _panelObject.AddComponent<RectTransform>();
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            
            // 添加背景图片
            Image backgroundImage = _panelObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }
        
        protected override void SetupUI()
        {
            CreateTitle();
            CreateButtons();
        }
        
        /// <summary>
        /// 创建标题
        /// </summary>
        private void CreateTitle()
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_panelObject.transform);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(400, 100);
            
            _titleText = titleObj.AddComponent<Text>();
            _titleText.text = "Astrum Game";
            _titleText.fontSize = 48;
            _titleText.color = Color.white;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        /// <summary>
        /// 创建按钮
        /// </summary>
        private void CreateButtons()
        {
            // 开始游戏按钮
            _startGameButton = CreateButton("StartGameButton", "开始游戏", new Vector2(0.5f, 0.5f), new Vector2(200, 50));
            
            // 设置按钮
            _settingsButton = CreateButton("SettingsButton", "设置", new Vector2(0.5f, 0.4f), new Vector2(200, 50));
            
            // 退出按钮
            _exitButton = CreateButton("ExitButton", "退出游戏", new Vector2(0.5f, 0.3f), new Vector2(200, 50));
        }
        
        /// <summary>
        /// 创建按钮
        /// </summary>
        private Button CreateButton(string name, string text, Vector2 anchorPosition, Vector2 size)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(_panelObject.transform);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorPosition;
            buttonRect.anchorMax = anchorPosition;
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = size;
            
            // 添加按钮背景
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.3f, 0.5f, 1f);
            
            // 添加按钮组件
            Button button = buttonObj.AddComponent<Button>();
            
            // 创建按钮文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.fontSize = 20;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            return button;
        }
        
        protected override void BindEvents()
        {
            if (_startGameButton != null)
            {
                _startGameButton.onClick.AddListener(OnStartGameClick);
            }
            
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClick);
            }
            
            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitClick);
            }
        }
        
        protected override void UnbindEvents()
        {
            if (_startGameButton != null)
            {
                _startGameButton.onClick.RemoveListener(OnStartGameClick);
            }
            
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClick);
            }
            
            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClick);
            }
        }
        
        /// <summary>
        /// 开始游戏按钮点击事件
        /// </summary>
        private void OnStartGameClick()
        {
            ASLogger.Instance.Info("StartMenuPanel: 开始游戏按钮被点击");
            
            // TODO: 实现 GameApplication 和 GameLauncher
            /*
            // 检查GameApplication是否存在
            if (GameApplication.Instance == null)
            {
                ASLogger.Instance.Error("StartMenuPanel: GameApplication实例不存在");
                return;
            }
            */
            
            // 开始单机游戏
            StartSinglePlayerGame();
        }
        
        /// <summary>
        /// 开始单机游戏
        /// </summary>
        private void StartSinglePlayerGame()
        {
            ASLogger.Instance.Info("StartMenuPanel: 启动单机游戏模式");
            
            try
            {
                // TODO: 实现 GameLauncher
                /*
                // 创建游戏启动器并启动单机模式
                GameLauncher launcher = new GameLauncher();
                launcher.StartSinglePlayerGame(GAME_SCENE_NAME);
                */
                
                ASLogger.Instance.Info("StartMenuPanel: 游戏启动器未实现，跳过启动");
                
                // 隐藏当前面板
                Hide();
                
                // 触发面板事件
                TriggerEvent("GameStarted", "SinglePlayer");
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"StartMenuPanel: 启动单机游戏失败 - {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置按钮点击事件
        /// </summary>
        private void OnSettingsClick()
        {
            ASLogger.Instance.Info("StartMenuPanel: 设置按钮被点击");
            TriggerEvent("ShowSettings", null);
            // TODO: 实现设置界面
        }
        
        /// <summary>
        /// 退出按钮点击事件
        /// </summary>
        private void OnExitClick()
        {
            ASLogger.Instance.Info("StartMenuPanel: 退出按钮被点击");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        protected override void OnInitialize()
        {
            ASLogger.Instance.Info($"StartMenuPanel: 面板初始化完成");
        }
        
        protected override void OnShow()
        {
            ASLogger.Instance.Info($"StartMenuPanel: 面板显示");
        }
        
        protected override void OnHide()
        {
            ASLogger.Instance.Info($"StartMenuPanel: 面板隐藏");
        }
        
        protected override void OnUpdate()
        {
            // 处理输入或其他更新逻辑
        }
        
        protected override void OnDestroy()
        {
            ASLogger.Instance.Info($"StartMenuPanel: 面板销毁");
        }
    }
}
