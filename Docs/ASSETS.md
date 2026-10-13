# 美术资源与接入

当前试玩所需素材已经从无需登录的合法来源取得，并转换为 Unity URP。无需继续下载 Asset Store、Sketchfab 或 Mixamo 素材才能运行本版。

来源、作者、许可和修改详见 [CREDITS.md](CREDITS.md)。M4A1、AKM、建筑、手臂、感染者动画及音效主要为 CC0；Pixelhouse 感染者模型为 CC BY 3.0，游戏网页、Windows 包及源码中均保留署名。字体遵循 SIL OFL 1.1。

`SourceAssets/OpenGameArt` 和 `SourceAssets/PolyHaven` 在 D 盘保存原始归档、来源网页与校验信息；该目录不提交 Git。可再分发的模型、PBR 贴图、动画和声音位于 `Assets/ThirdParty` 并由 LFS 管理。十二种物品图标由本工程绘制。

导入与场景工具位于 `Assets/TwinkleOps/Editor`：OpenAssets、CC0Props、RaidContent、ProjectSetup、RaidPolish、ReleasePrep。正常打开工程不必重新生成场景。重新 Assemble 会重建本项目的 InfectionZone 场景，须先提交当前修改并确认需要重建。

质量边界：使用实际模型与贴图，不以可见 Cube/Capsule 代替角色、枪械和建筑。碰撞与场景边界可使用不可见基础碰撞体。当前场景为隔离工地，尚无地下区域；手指贴合、换弹动作、感染者脚部贴地和重复建筑布置仍需进一步美术制作。
