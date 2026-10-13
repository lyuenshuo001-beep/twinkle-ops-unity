# 构建与发布

源码仓库：https://github.com/lyuenshuo001-beep/twinkle-ops-unity
网页产物仓库：https://github.com/lyuenshuo001-beep/twinkle-ops-web

GitHub 设备授权已验证。授权由 Git Credential Manager 保管，脚本不保存或打印令牌。

Windows 使用 Mono，完整输出目录为 `D:\GameProjects\TwinkleOps\Builds\Game\Windows`；必须打包 EXE、Data、UnityPlayer.dll、MonoBleedingEdge 及许可文件，不能只分发 EXE。Web 输出目录为 `Builds/Game/Web`，运行 `Tools/Prepare-Web.ps1` 安装本地化网页外壳。

Web 采用 gzip + decompressionFallback，兼容无法设置 Content-Encoding 的静态站点。当前为单线程 WebGL 2，不依赖 SharedArrayBuffer。游戏资源随构建本地打包，不依赖素材平台 CDN。

网页仓库只放 Web 构建文件，不使用 Git LFS。GitHub Pages 采用 main 分支根目录并保留 `.nojekyll`。预期地址为 https://lyuenshuo001-beep.github.io/twinkle-ops-web/ ，是否已验证须查看 STATUS.md；不得仅根据预期地址宣称上线。

部署后运行 `node Tools/GameWebSmoke.cjs https://lyuenshuo001-beep.github.io/twinkle-ops-web/ pages-web`，检查真实 HTTPS 资源、Unity 启动、进入战区及操作截图。

## 国内备用托管

同一个 `Builds/Game/Web` 可完整上传到已有静态托管或对象存储，使用相对 URL，保留文件结构、许可及 `.nojekyll`。Unity 的压缩回退不要求特殊服务器压缩头。应启用 HTTPS，HTML 短缓存，构建资源保留版本查询参数。本项目尚未配置第二个托管账号或域名，不自动开通付费服务。GitHub Pages 在中国大陆的访问质量尚未实地验证。

## 本地试玩

执行 `node Tools/ServeWeb.cjs` 后访问 http://127.0.0.1:8080/ 。手机实机需要另行配置受信任 HTTPS 测试服务；桌面模拟不等同于真机验收。

旧 `Builds/Validation` 只含工具链大厅，不得发布为本版试玩。
