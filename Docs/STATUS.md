# V1.1 已验证版本

2026-10-14，1.1.0-alpha。网页：https://lyuenshuo001-beep.github.io/twinkle-ops-web/ 。Windows 发布页：https://github.com/lyuenshuo001-beep/twinkle-ops-unity/releases/tag/v1.1.0-alpha 。本机完整工程：`D:\GameProjects\TwinkleOps`。

16 项 EditMode、5 项带图形设备的 PlayMode 测试通过；覆盖骨骼命中、攻击接触、AK 单发事件、匕首击杀、药品、物资、存档、死亡损失、物理路线、视线外刷新及对象池复用。详细边界见 V1.1-QA.md。

Web 完整构建成功：112,185,747 字节，foundation=false。GitHub Pages 发布提交 `64d693b`，资源哈希 `525535f6ed0a`。真实 HTTPS 页面通过 Edge WebGL 启动、部署到战区、四种武器、开火、换弹、移动、背包操作；运行异常与失败资源请求均为 0。桌面 Pixel 7 横屏触控模拟完成部署、切枪、开火、独立匕首按钮与挥刀，未发现运行异常。不等于 Android/iPhone 实机兼容性验收。

Windows 完整构建成功：348,619,833 字节；EXE 实际启动出现 TWINKLE_OPS_BOOT_OK。尚未做 Windows 长时间人工游玩。完整 ZIP：186,675,543 字节，SHA-256 `d1dfa726b1b505b53f9ae82e6d028b14f37f716e6d28fd1537653b14f3f5f246`。

新增医疗站、仓储区、下沉车库、27 个六类容器、两个保险箱、药品使用、M9 和 M870。明亮日间光照与低污渍建筑。当前仍为早期 Alpha；感染者变体来自同一模型、枪械动作以程序动画为主，地图与角色美术需继续提升。

日志：Logs/v11-release-EditMode.xml、v11-release-PlayMode.xml、v11-routes.txt、v11-pages-web.json、web-mobile-emulated.json、v11-windows-launch.log、build-web.json、build-windows.json。恢复点：v1.1-before-upgrade / eb9d227。

静态资源均随构建部署；首次网页加载约 112 MB。仓库本地保存，不跨设备同步。GitHub Pages 在中国大陆访问质量未实地测量；可迁移同一静态包的方案见 DEPLOYMENT.md，尚未上线第二托管站点。
