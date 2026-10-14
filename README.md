# 闪烁行动 / TWINKLE OPS

Unity 6000.6.5f1 + URP 17.6 的单人搜打撤游戏，1.1.0-alpha。

[网页试玩](https://lyuenshuo001-beep.github.io/twinkle-ops-web/) · [Windows 下载](https://github.com/lyuenshuo001-beep/twinkle-ops-unity/releases) · [实际验证记录](Docs/STATUS.md)

V1.1 在原工程上修复感染者追击、攻击接触、骨骼受击区与 AK 单发音效，并改为清晰明亮的日间光照。现有 M4A1、AKM，加上 M9 匕首与 M870；27 个容器包含军用箱、垃圾桶、垃圾袋、行李箱、工具箱和 2 个保险箱。背包内可使用绷带和急救药品。

医疗站、仓储区、下沉车库扩展探索路线，包含可达楼层和三个不同计时的撤离点。六档随机物资、网格背包、丢弃拾回、撤离仓库与死亡损失继续保留。感染者采用视线外刷新和对象池。

这是早期 Alpha。感染者外观仍是同一基础模型的变体，第一人称换弹主要为程序动画，M870 尚无完整逐发装填。地图细节和角色美术距离最终目标仍有差距。商店、完整改枪、多人联机、特殊感染者和 Boss 尚未实现。

## 打开与运行

1. 安装 Git LFS，执行 `git lfs install`，克隆仓库后执行 `git lfs pull`。
2. 用 Unity **6000.6.5f1** 打开工程，等待包恢复与素材导入。
3. 打开 `Assets/TwinkleOps/Scenes/Lobby.unity`，进入 Play Mode，点击“进入感染区”。
4. WASD 移动，Shift 冲刺，空格跳跃，鼠标左键开火，右键 ADS，R 换弹，1/2/3/4 切武器，V 匕首，Q 循环切换，E 搜索/拾取，Tab 背包，Esc 菜单。F3 显示弹道和受击区。菜单和背包不会暂停感染者。
5. 搜刮后进入绿色撤离圈并保持至倒计时结束。仓库存于当前浏览器或本机，不跨设备同步。手机横屏使用触控按钮；实机兼容性仍需测试。

## 构建与验证

本机主工程：`D:\GameProjects\TwinkleOps`。编辑器：`D:\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe`。

```powershell
powershell -File Tools/RunV11Checks.ps1
powershell -File Tools/Unity.ps1 Web
powershell -File Tools/Prepare-Web.ps1
node Tools/ServeWeb.cjs
node Tools/GameWebSmoke.cjs http://127.0.0.1:8080/
node Tools/MobileWebSmoke.cjs http://127.0.0.1:8080/
powershell -File Tools/Unity.ps1 Windows
```

Windows 使用 Mono，Web 使用 Web Build Support / IL2CPP。输出位于 `Builds/Game/Windows` 和 `Builds/Game/Web`。Web 包须通过 HTTP 服务打开。

源码使用 Git LFS 保存允许再分发的二进制美术资产，缓存、原始下载归档和构建输出不入源码仓库。素材来源与修改见 [CREDITS](Docs/CREDITS.md)，验收及限制见 [V1.1 QA](Docs/V1.1-QA.md)，静态托管迁移见 [DEPLOYMENT](Docs/DEPLOYMENT.md)。

所有美术和声音资源均有独立授权，不含其他商业游戏提取素材。仓库代码未自动赋予第三方素材之外的开放许可。
