# GameDirector 架构图

## 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                        Unity Application                        │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                    GameApplication                               │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ • Unity MonoBehaviour 入口点                               │ │
│  │ • 基础设置 (帧率、运行状态)                                │ │
│  │ • 委托给 GameDirector 进行核心管理                         │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                    GameDirector                                 │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ • 单例模式，游戏核心控制器                                 │ │
│  │ • 统一管理所有子系统                                       │ │
│  │ • 游戏状态切换和流程控制                                   │ │
│  │ • GameMode 生命周期管理                                    │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
┌───────▼──────┐ ┌───▼────┐ ┌─────▼─────┐
│ GameState    │ │ GameMode│ │ Subsystems│
│ Management   │ │ System  │ │ Management│
│              │ │         │ │           │
│ • State      │ │ • IGame │ │ • Resource│
│   Machine    │ │   Mode  │ │   Manager │
│ • State      │ │ • Single│ │ • Scene   │
│   Events     │ │   Player│ │   Manager │
│ • Transitions│ │ • Multi │ │ • Network │
│              │ │   Player│ │   Manager │
│              │ │ • Custom│ │ • UI      │
│              │ │   Modes │ │   Manager │
│              │ │         │ │ • Audio   │
│              │ │         │ │   Manager │
│              │ │         │ │ • Input   │
│              │ │         │ │   Manager │
│              │ │         │ │ • Camera  │
│              │ │         │ │   Manager │
└──────────────┘ └─────────┘ └───────────┘
```

## 详细组件关系图

```
GameApplication (Unity MonoBehaviour)
    │
    ├── GameDirector (Singleton)
    │   │
    │   ├── GameStateMachine
    │   │   ├── ApplicationStarting
    │   │   ├── ApplicationReady
    │   │   ├── GameMenu
    │   │   ├── GameLoading
    │   │   ├── GamePlaying
    │   │   ├── GamePaused
    │   │   ├── GameEnding
    │   │   └── SystemShutdown
    │   │
    │   ├── GameMode Management
    │   │   ├── IGameMode Interface
    │   │   │   ├── Initialize()
    │   │   │   ├── StartGame()
    │   │   │   ├── Update()
    │   │   │   ├── Shutdown()
    │   │   │   ├── OnStateEnter()
    │   │   │   ├── OnStateExit()
    │   │   │   └── OnGameEvent()
    │   │   │
    │   │   ├── SinglePlayerGameMode
    │   │   │   ├── Local Room Creation
    │   │   │   ├── Local Stage Management
    │   │   │   └── Local Player Creation
    │   │   │
    │   │   ├── MultiplayerGameMode
    │   │   │   ├── Network Room Management
    │   │   │   ├── Frame Sync Handling
    │   │   │   └── Network Player Management
    │   │   │
    │   │   └── CustomGameMode (Future)
    │   │       ├── Custom Logic
    │   │       └── Custom State Management
    │   │
    │   └── Subsystem Management
    │       ├── ResourceManager
    │       ├── SceneManager
    │       ├── NetworkManager
    │       ├── UIManager
    │       ├── AudioManager
    │       ├── InputManager
    │       ├── CameraManager
    │       └── GamePlayManager (Legacy)
    │
    └── Legacy Components (Gradual Migration)
        ├── GamePlayManager (Simplified)
        │   ├── User Management
        │   ├── Room System Management
        │   └── Network Message Handling
        │
        └── Direct Manager Access (Deprecated)
            ├── SceneManager.Instance
            ├── ResourceManager.Instance
            └── NetworkManager.Instance
```

## 状态流转图

```
ApplicationStarting
    │
    ▼
ApplicationReady
    │
    ▼
GameMenu ──────────────┐
    │                 │
    ▼                 │
GameLoading           │
    │                 │
    ▼                 │
GamePlaying ──────────┤
    │                 │
    ▼                 │
GamePaused ───────────┤
    │                 │
    ▼                 │
GameEnding            │
    │                 │
    ▼                 │
SystemShutdown ◄──────┘
```

## GameMode 状态流转图

```
Initializing
    │
    ▼
Loading
    │
    ▼
Ready
    │
    ▼
Playing ──────────────┐
    │                 │
    ▼                 │
Paused ───────────────┤
    │                 │
    ▼                 │
Ending                │
    │                 │
    ▼                 │
Finished ◄────────────┘
```

## 数据流图

```
User Input
    │
    ▼
GameApplication
    │
    ▼
GameDirector
    │
    ├── GameStateMachine ──► State Events
    │
    ├── Current GameMode ──► Game Logic
    │
    └── Subsystem Updates ──► Manager Updates
                                │
                                ▼
                            Game Rendering
```

## 类关系图

```
GameApplication
    │
    │ uses
    ▼
GameDirector
    │
    │ manages
    ├── GameStateMachine
    │
    │ controls
    ├── IGameMode
    │   │
    │   ├── SinglePlayerGameMode
    │   └── MultiplayerGameMode
    │
    │ coordinates
    └── IGameSubsystem
        │
        ├── ResourceManager
        ├── SceneManager
        ├── NetworkManager
        ├── UIManager
        ├── AudioManager
        ├── InputManager
        └── CameraManager
```

## 事件流图

```
GameEvent
    │
    ▼
GameDirector.OnGameEvent()
    │
    ├── GameStateMachine.ProcessEvent()
    │
    ├── CurrentGameMode.OnGameEvent()
    │
    └── SubsystemManager.BroadcastEvent()
```

## 初始化序列图

```
GameApplication.Awake()
    │
    ▼
GameDirector.Initialize()
    │
    ├── InitializeSubsystems()
    │   ├── ResourceManager.Initialize()
    │   ├── SceneManager.Initialize()
    │   ├── NetworkManager.Initialize()
    │   └── ...
    │
    ├── InitializeStateMachine()
    │
    └── SetInitialState(ApplicationStarting)
```

## 更新循环序列图

```
GameApplication.Update()
    │
    ▼
GameDirector.Update(deltaTime)
    │
    ├── GameStateMachine.Update(deltaTime)
    │
    ├── CurrentGameMode.Update(deltaTime)
    │
    └── SubsystemManager.Update(deltaTime)
        ├── ResourceManager.Update()
        ├── SceneManager.Update()
        ├── NetworkManager.Update()
        └── ...
```

## 状态切换序列图

```
ChangeGameState(newState)
    │
    ▼
GameStateMachine.ChangeState()
    │
    ├── ValidateTransition()
    │
    ├── OnStateExit(currentState)
    │
    ├── UpdateCurrentState(newState)
    │
    └── OnStateEnter(newState)
        │
        ├── GameDirector.OnStateEnter()
        │
        └── CurrentGameMode.OnStateEnter()
```

这个架构设计提供了清晰的职责分离、统一的状态管理和强大的扩展性，为 Astrum 游戏的核心模块提供了坚实的基础。
