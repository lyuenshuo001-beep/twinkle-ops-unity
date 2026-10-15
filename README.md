# 闪烁行动 / TWINKLE OPS

Unity 6000.6.5f1 + URP 17.6 的单人搜打撤游戏，1.2.0-alpha。

[网页试玩](https://lyuenshuo001-beep.github.io/twinkle-ops-web/) · [Windows 下载](https://github.com/lyuenshuo001-beep/twinkle-ops-unity/releases) · [实际验证记录](Docs/STATUS.md)

V1.2 在现有工程上连接医疗接待区、诊疗楼、跨区运输廊、车库第二出口及屋顶路线，使用合法厂房模块、按尺寸铺设的建筑材质和明亮日间云天空。34 个六类搜索容器，其中 2 个保险箱。

新增逐件搜索、未知物品旋转揭示、大尺寸图标与网格、拖拽/旋转、2×2 安全箱、物资出售、药品补给、战前装包与战后价值结算。默认右键/触控点击切换 ADS，大厅或菜单可选 Hold。普通撤离保留，车库要求背包不超过 8 kg，仓储出口开局 90 秒信号就绪。

保留 M4A1、AKM、M9、M870 和已验证的感染者动画、骨骼 Hitbox、AK 单发音效、近战、药品、视线外对象池刷新。23 项编辑测试、6 项实际场景测试通过；完整范围与浏览器验证见 [V1.2 QA](Docs/V1.2-QA.md)。设计调研与差距见 [研究记录](Docs/V1.2-RESEARCH.md)。

这是早期 Alpha。枪械和角色美术、第一人称动作、地图细节仍需打磨。完整枪械商店、改枪、任务、多人、特殊感染者和 Boss 尚未实现。

## 打开与运行

1. 安装 Git LFS，执行 `git lfs install`，克隆仓库后执行 `git lfs pull`。
2. 用 Unity **6000.6.5f1** 打开工程，等待包恢复与素材导入。
3. 打开 `Assets/TwinkleOps/Scenes/Lobby.unity`，进入 Play Mode，点击“进入感染区”。
4. WASD 移动，Shift 冲刺，空格跳跃，鼠标左键开火，右键点击切换 ADS，R 换弹，1/2/3/4 切武器，V 匕首，Q 循环切换，E 搜索/拾取，Tab 背包，Esc 菜单。F3 显示弹道和受击区。菜单和背包不会暂停感染者。
5. 搜刮后进入绿色撤离圈并保持至倒计时结束。仓库存于当前浏览器或本机，不跨设备同步。手机横屏使用触控按钮；实机兼容性仍需测试。

## 构建与验证

本机主工程：`D:\GameProjects\TwinkleOps`。编辑器：`D:\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`。

```powershell
powershell -File Tools/RunV12Checks.ps1
powershell -File Tools/Unity.ps1 Web
powershell -File Tools/Prepare-Web.ps1
node Tools/ServeWeb.cjs
node Tools/GameWebSmokeV12.cjs http://127.0.0.1:8080/
node Tools/MobileWebSmokeV12.cjs http://127.0.0.1:8080/
powershell -File Tools/Unity.ps1 Windows
```

Windows 使用 Mono，Web 使用 Web Build Support / IL2CPP。输出位于 `Builds/Game/Windows` 和 `Builds/Game/Web`。Web 包须通过 HTTP 服务打开。

源码使用 Git LFS 保存允许再分发的二进制美术资产，缓存、原始下载归档和构建输出不入源码仓库。素材来源与修改见 [CREDITS](Docs/CREDITS.md)，验收及限制见 [V1.2 QA](Docs/V1.2-QA.md)，静态托管迁移见 [DEPLOYMENT](Docs/DEPLOYMENT.md)。

所有美术和声音资源均有独立授权，不含其他商业游戏提取素材。仓库代码未自动赋予第三方素材之外的开放许可。
