# 已发布的第一阶段试玩

2026-10-13，版本 0.2.0-alpha。

- 网页试玩：https://lyuenshuo001-beep.github.io/twinkle-ops-web/
- Windows 下载：https://github.com/lyuenshuo001-beep/twinkle-ops-unity/releases/tag/v0.2.0-alpha
- 完整源码：https://github.com/lyuenshuo001-beep/twinkle-ops-unity
- 本机工程：`D:\GameProjects\TwinkleOps`
- 网页构建检出：`D:\GameProjects\TwinkleOpsWeb`

已接入真实模型和贴图、M4A1 / AKM、动画感染者、10 个搜索容器、六档随机物资、网格背包、丢弃拾回、8 秒撤离、持久仓库、死亡损失。当前关卡为隔离工地，包含室内与楼梯。

## 实际验证

- Unity 6000.6.5f1 正常调用；Windows Mono 和 Web 构建模块可用。
- 15 项 EditMode 测试、1 项真实场景整局 PlayMode 测试通过。后者额外检查 AKM 模型方向和枪口位置。
- 出生点到 10 个容器和撤离点的导航路径全部可达。
- Windows 完整构建成功（222,713,541 字节），EXE 启动出现 TWINKLE_OPS_BOOT_OK。尚未做 Windows 长时间人工游玩验收。
- Windows ZIP 106,133,697 字节，上传后的 GitHub SHA-256 与本地一致：`49386a3429d60719e512c31c2ba95392052b6d3fa0ee2573f38fec45ed4a05d5`。
- Web 完整构建成功（85,600,004 字节，构建报告 foundation=false）。
- GitHub Pages 实际 HTTPS 地址已通过 Edge 浏览器验证：引擎启动、大厅进入战区、切枪/开火/换弹/移动/背包截图；JavaScript 运行错误和失败资源请求为 0。
- 桌面 Chromium 的 Pixel 7 横屏触控模拟：进入战区、切枪、开火成功。真实 Android / iPhone 尚未验收。
- 源码和 102 个 LFS 资源已上传。公开网页产物使用普通 Git，避免静态网站收到 LFS 指针。

## 仍待完善

当前是可玩的早期 Alpha，尚未达到最终写实美术标准。手指与握持贴合、换弹动作、感染者动作细节、地图布置、移动端 HUD 和性能仍需打磨。暂无地下区域、商店、完整改枪、多种感染者外观、特殊感染者、Boss 或多人联机。

所有资源本地打包；首次 Web 加载约 86 MB。仓库保存在当前浏览器，清除网站数据会清除存档，不跨设备同步。GitHub Pages 在中国大陆的网络质量未经实地验证；静态托管迁移方案见 DEPLOYMENT.md，尚未部署备用站点。

可复查日志：Logs/playmode-results.xml、editmode-results.xml、raid-routes.json、build-web.json、build-windows.json、pages-web.json、web-mobile-emulated.json。旧 Builds/Validation 仅验证大厅工具链，不是本版交付。
