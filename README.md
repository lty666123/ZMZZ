# 一个2D的小项目 

使用 Unity 开发的 2D 动作游戏，包含角色移动与战斗、敌人与龙 Boss、物品掉落、背包、装备、UI、账号登录、好友聊天和账号云存档相关代码。

本仓库的游戏客户端来自正在开发的 `E:\unity Prograss\ZMZZ` 工程。早期的 `ZMZZ-Unity` 方块战斗原型已从最新版本中移除。

## 目录

- `ZMZZ/Assets/`：游戏脚本、美术资源、动画、场景、预制体和 `.meta` 文件。
- `ZMZZ/Packages/`：Unity 包清单与锁定文件。
- `ZMZZ/ProjectSettings/`：Unity 工程设置。
- `Server/ZmzzServer/`：ASP.NET Core 服务端源码，包含账号、好友、聊天与 SQLite 云存档相关实现。

## 打开客户端

使用 Unity Hub 添加仓库内的 `ZMZZ` 文件夹。Unity 版本为 **2022.3.62f3c1**。首次打开时，Unity 会恢复依赖并重建缓存。

当前构建设置中的场景为：

1. `Assets/Scenes/SampleScene.unity`
2. `Assets/Scenes/GamePanelCeShi.unity`
3. `Assets/Scenes/GameScene1.unity`

主要脚本目录包括 `Assets/Scripts/Inventory`、`Assets/Scripts/NetWork`、`Assets/Scripts/UIPanel` 和 `Assets/Scripts/Object`。

## 启动服务端

安装 .NET 8 SDK 或支持该目标框架的较新 SDK，然后运行：

```powershell
dotnet run --project Server/ZmzzServer/ZmzzServer.csproj
```

客户端当前连接本机的 `http://127.0.0.1:5080`，因此联网功能需要先启动服务端。

Unity 缓存、IDE 缓存、构建输出、日志、本地备份和服务端 SQLite 数据库不纳入版本管理。该仓库保留当前开发状态，功能以实际源码实现为准。
