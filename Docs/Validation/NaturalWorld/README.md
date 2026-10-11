# 场景、角色与小地图优化验收（2026-10-11）

本轮改动已同步 iOS / Windows，保留两端原有控制和平台代码。未修改图标资源，未提交或推送。

## 已接入

- 角色与怪物：圆润躯干、面部、头盔、肩甲、手指、靴子；布料/钢铁/皮革/皮肤材质图集；原有关节上的双骨骼连续袖管和裤腿；肘部摆动、踝关节屈伸、呼吸。攻击阶段保留原有攻击姿态和武器接触时序。史莱姆使用胶质身体，精灵使用水滴核心。
- 场景：自然石材、木纹、土壤与苔藓材质；双尺度采样、细微凹凸、石材风化；道路草土渐变边缘；水面动态微波纹；软阴影及更贴近物体的阴影偏移。原生分辨率、4 倍抗锯齿、纹理 mipmaps、各向异性过滤。
- 地形：四处平滑坡地，最高约 2 米；CPU 移动/怪物/技能落点与 GPU 地形共享相同山丘参数，营地、桥面、河道和传送门保持平稳。
- 植被：三种共享树木网格，锥形树干、分枝、根部与独立曲面树叶；林地蕨叶、落叶与碎石合并为三个渲染对象。城镇/副本只增加一组碎石；不新增碰撞体或寻路障碍。
- 小地图：暗绿地形底图、岸线、坡地阴影/等高线与圆点；192 平方像素 Color32、每帧约 1.5ms 首次烘焙、六项 LRU 缓存；重复返回营地复用纹理。地面细分缓存 CPU 几何，每个世界仍独立拥有并释放 GPU 网格。

## 卡顿原因与验证

隔离项目的转换计时定位到返回营地时的同步刷怪：约 12 只怪物，每只安全出生检查包含多次寻路，刷怪约 260ms，整体返回约 245–292ms。现改为每帧生成一只远处怪物，暂停、UI 阻塞、后台和试招时等待；切换副本取消旧任务。编辑器观察返回约 12–36ms。此数据是编辑器同步转换计时，不代表真机加载时间或帧率。

Minimap/After/deferred-population-report.txt 保存最初分帧测试；本目录 minimap-with-scene-detail-report.txt 保存加入环境细节后的计时（11.58–17.89ms）与同一营地纹理复用检查。

## 验证范围

- iOS / Windows 独立 Runtime 与 Editor 源码编译均通过；FinalCompile/report.json 无验证期间源码变化。
- 两端各 2408 项生产小地图几何/缓存身份检查；两端各 617 项地形、移动、怪物和技能贴地检查通过。
- ActorAnatomy/pose-review.txt：实际 Metal 渲染四职业与四类怪物的待机、行走、攻击姿态；连续蒙皮双骨骼、权重归一化、每段不超过 500 顶点、变形顶点有限。人物近景包含专用补光，不能作为默认游戏照明截图。
- 本目录 review.txt：实际 Metal 渲染营地、林地、溪流、副本、赤岩驿站、星望城；新 shader 支持，无 shader 错误；单个树冠少于 3000 三角形，合并细节网格少于 25000 顶点；坡地人物贴地；试招期间只有练习目标、结束后恢复 12 只环境怪物。
- iOS Unity 导出、Xcode 模拟器构建成功；Mach-O platform 为 IOSSIMULATOR。独立包名 com.h644782259.emberfall.worldreview 已安装并在 iPhone 16 Pro / iOS 26.5 模拟器运行。截图为实际游戏画面，视频包含移动与场景动态；不是性能基准。
- Windows 本轮验证到源码编译，未运行 Windows 玩家程序；未测物理设备帧率、长时间热性能或所有装备组合。

## 图片与材质来源

- camp.png / woodland-hill.png / brook.png / dungeon.png / quarry.png / observatory.png：编辑器实际 Play Mode 场景相机。
- simulator-camp.png / simulator-hill.png：iOS 模拟器实际运行截图。
- simulator-actions.mp4：实际模拟器录像的短预览。
- Assets/Resources/WorldArt/ActorMaterialAtlas.png 与 NaturalSurfaceAtlas.png：内置 imagegen 生成，已保存到两端工程。完整提示词与来源在 ArtSource/ActorAnatomy/material-generation.txt 和 ArtSource/NaturalWorld/material-generation.txt。

动态检查补充：模拟器通过键盘捕获持续移动到坡地，再恢复键盘捕获为关闭；屏幕按钮闪避触发冷却。录像中人物位置、摄像机跟随和怪物行走发生实际变化，未见新模型蒙皮破裂或明显穿地。短预览取原始 72.85 秒录像前 40 秒，保留移动时段。动作按钮操作不等于逐项技能结算验收。
