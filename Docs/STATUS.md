# V1.2 已验证版本

2026-10-15，1.2.0-alpha。完整工程：`D:\GameProjects\TwinkleOps`。

- 网页试玩：https://lyuenshuo001-beep.github.io/twinkle-ops-web/
- Windows 完整包：https://github.com/lyuenshuo001-beep/twinkle-ops-unity/releases/tag/v1.2.0-alpha
- 源码：https://github.com/lyuenshuo001-beep/twinkle-ops-unity

23/23 EditMode、6/6 带图形设备的 PlayMode 通过。十条实际玩家碰撞路线走通；34 个容器导航可达。旧存档迁移、安全箱死亡/中断保留、搜索暂停重开、经济保存失败回滚、射击/近战/骨骼接触回归已验证。详细测试范围、截图和限制见 V1.2-QA.md。

Web 完整构建 **112,541,452 字节**；data 文件 **103,424,750 字节**，低于 GitHub 单文件 100 MiB 上限，发布脚本已添加大小检查。资源哈希 `09c4e6bc065e`。Pages 发布提交 `64f9d49`，GitHub 构建状态 built；公网 version.json 匹配 V1.2 与此哈希。正式 HTTPS 地址通过 Edge PC 实际进入战区、右键点击开镜/退出、买药/装包、安全箱、四武器操作和背包；脚本异常、运行异常及失败资源请求均为 0。

同一正式 HTTPS 地址通过 Pixel 7 横屏触控模拟：进入战区、Toggle ADS 开/关、切枪、开火、匕首和背包；异常与失败请求均为 0。**没有 Android/iPhone 真机测试**。本地浏览器还按截图实际瞄准、走近、E 键打开容器，验证未知物品转圈、逐件揭示及点击拾取入包。

Windows 完整构建 **389,168,329 字节**；实际启动 Direct3D 11 / RTX 5060 Laptop，日志出现 TWINKLE_OPS_BOOT_OK。完整 ZIP **206,801,388 字节**，SHA-256 `b83b97057774e6410b56d62699bb3dfee4fcec262209e993bbacee39c8da60a5`，与 GitHub 上传资源 digest 一致。Release `413617627` 已发布（公开 Alpha 预发行），不是草稿。尚未进行 Windows 长时间人工游玩。

源码功能提交 `7dc97e2`，发布标签 `v1.2.0-alpha` 指向 `dc3a74f`；恢复点 `v1.2-before-upgrade` / `d5f7305`。旧 V1.1 Release 保留。

新增连通接待/诊疗区、运输廊、车库北坡道、屋顶步梯/桥梁、明亮 CC0 云天空及建筑材质。逐件搜索、大尺寸物品格、拖拽/旋转、安全箱、出售、药品补给/装包、结算和条件撤离已接入。M4A1/AKM/M9/M870 保留；本轮未虚构新增 MP5 等枪械。完整枪械商店、改枪、护甲弹药层级、任务、多人尚未完成，角色和地图细节仍需继续打磨。

首次网页资源约 113 MB；游戏资源随包部署，使用 Brotli 回退和浏览器缓存。仓库只保存到当前浏览器/本机，不跨设备同步。中国大陆访问质量未做地区实测；备用静态部署方式见 DEPLOYMENT.md，未上线第二托管站点。

证据：Logs/v12-release-EditMode.xml、v12-release-PlayMode.xml、v12-routes.txt、v12-pages-web.json、v12-web-mobile-emulated.json、v12-windows-launch.log、build-web.json、build-windows.json。运行日志中的继承性平台/池初始化警告见 V1.2-QA.md；零异常不等于零警告。
