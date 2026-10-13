# 闪烁行动 / TWINKLE OPS

Unity 6000.6.5f1 + URP 17.6 的单人末日搜打撤游戏，0.2.0 早期试玩。

[网页试玩](https://lyuenshuo001-beep.github.io/twinkle-ops-web/) · [Windows 下载](https://github.com/lyuenshuo001-beep/twinkle-ops-unity/releases/tag/v0.2.0-alpha) · [实际验证记录](Docs/STATUS.md)

当前战局包含真实贴图建筑、室内与楼梯、M4A1 / AKM、带追击及攻击动画的感染者、10 个可搜索容器、六档物资、网格背包、丢弃拾回、8 秒撤离和本地持久仓库。死亡或中途退出会损失本局战利品。

这是第一阶段工程。手部握持、换弹动作、地图细节及移动设备性能还需打磨；尚不包含商店、完整改枪、多人联机、地下关卡、特殊感染者或 Boss。

## 打开与运行

1. 安装 Git LFS，执行 `git lfs install`，克隆本仓库后执行 `git lfs pull`。
2. 用 Unity **6000.6.5f1** 打开工程，等待包恢复与素材导入。
3. 打开 `Assets/TwinkleOps/Scenes/Lobby.unity`，进入 Play Mode。
4. 点击“进入感染区”。WASD 移动，Shift 冲刺，空格跳跃，鼠标左键开火，右键 ADS，R 换弹，1/2/Q 切枪，E 搜索/拾取，Tab 背包，Esc 菜单。菜单和背包不会暂停感染者。
5. 搜刮后向北侧绿色撤离圈移动并停留 8 秒。仓库保存在当前浏览器或本机，不跨设备同步。

## 构建与验证

本机主工程：`D:\GameProjects\TwinkleOps`。编辑器：`D:\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`。

```powershell
powershell -File Tools/Unity.ps1 Test
powershell -File Tools/Unity.ps1 PlayTest
powershell -File Tools/Unity.ps1 Windows
powershell -File Tools/Unity.ps1 Web
powershell -File Tools/Prepare-Web.ps1
node Tools/ServeWeb.cjs
node Tools/GameWebSmoke.cjs http://127.0.0.1:8080/
```

Windows 构建使用 Mono，Web 使用已安装的 Web Build Support。输出位于 `Builds/Game/Windows` 和 `Builds/Game/Web`。不要双击 HTML 运行 Web 包，应通过 HTTP 服务打开。

源码仓库使用 Git LFS 保存允许再分发的二进制美术资产。缓存、原始下载归档、日志与构建输出不进入源码仓库。素材来源和修改记录见 [CREDITS](Docs/CREDITS.md)，测试边界见 [QA](Docs/QA.md)，部署方式见 [DEPLOYMENT](Docs/DEPLOYMENT.md)。

所有美术/声音资源都有独立授权，不含其他商业游戏提取素材。仓库代码未自动赋予第三方素材之外的开放许可。
