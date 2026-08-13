# Spellbook Unity 版设计文档(src_unity)

日期:2026-08-13
状态:已批准(用户全权委托,决策按推荐执行)

## 定位

市场化水准的"游戏感"版 Spellbook:同样是脚本/程序启动器,但交互体验对标游戏——
绚丽丝滑的过场动画、粒子特效、全程音效反馈、魔兽风格的法术书视觉。
WPF 版(src/)继续作为轻量版共存;两版共享同一份数据文件,可随时互换使用。

## 技术选型

| 项 | 选择 | 理由 |
|---|---|---|
| 引擎 | Unity 6000.0.81f1 LTS | 当前最成熟 LTS |
| 渲染管线 | 内置管线 + 加法粒子/软辉光贴图 | 实施中降险调整:URP+Bloom 的收益不抵盲配管线资产的失败风险,辉光用 Kenney 软光贴图 + 缩放脉动实现,视觉效果同样到位 |
| UI | uGUI + TextMeshPro,Screen Space - Camera | 成熟、粒子可与 UI 混排、吃 Bloom |
| 场景构建 | Code-first:单一空场景 + Bootstrap 全代码构建 UI | 可测试、可 diff、避免手写 YAML 场景 |
| 动画 | 自研轻量 Tween 库(约 200 行) | 零外部依赖,Asset Store 需登录不可自动化 |
| JSON | com.unity.nuget.newtonsoft-json (UPM) | 容错好,与 WPF 版 System.Text.Json 输出互通 |
| 脚本后端 | Mono + .NET Standard 2.1 | 支持 System.Diagnostics.Process,构建快 |
| 测试 | Unity Test Framework EditMode(纯逻辑层) | 与 WPF 版同等的核心逻辑覆盖 |

## 数据互通

读写 `%APPDATA%\Spellbook\items.json`,字段与 WPF 版 `SpellItem` 完全一致
(Name/ScriptPath/Arguments/Notes/GroupName/SortOrder/IconKey)。
两版可同时安装、交替使用,互相看到对方的改动(启动时读、改动即写,不做文件监听)。

启动分发逻辑与 WPF 版一致:http(s) 网址 > 已存在目录 > .ps1 > 其他程序/文档,
全部 UseShellExecute 语义(网址/程序/文件夹交给系统,ps1 开新控制台等退出码)。

## 体验设计(核心卖点)

1. **开场**:暗色余烬背景中央一本封面法术书,符文呼吸辉光;点击或 1.2s 后自动
   翻开——封面旋开 + 光爆 + 翻页音,过渡到主界面。可点击跳过。
2. **主界面**:羊皮纸书页铺开;分组是右侧的书签标签(选中的书签抽出发光);
   磁贴是魔兽技能按钮样式(手绘图标 + 鎏金描边)。
3. **磁贴交互**:悬停 → 放大 1.08 + 金色辉光脉动 + tick 音;按下 → 缩小 0.95;
   点击 → 法阵粒子爆发 + 施法音,成功后磁贴短暂过曝闪光;失败 → 红色抖动 + 低沉音。
4. **分组切换**:翻书页过场——当前页向左卷出(缩放+旋转+透明度),新页从右滑入,
   带纸张翻动音;方向与分组顺序一致。
5. **搜索**:Ctrl+K 呼出,搜索条从顶部展开发光;不匹配磁贴逐个(交错 20ms)缩小
   淡出,匹配项保持明亮。
6. **编辑对话框**:模态羊皮卷从中心"展开"(Y 向缩放带回弹);图标选择器为发光
   网格,选中项法阵环绕。
7. **氛围层**:全屏漂浮余烬粒子、边缘暗角呼吸、循环环境音乐(可静音,状态存
   PlayerPrefs)。
8. **全程音效**:悬停 tick、点击、翻页、施法、错误、对话框开合,各自独立音效。

## 功能范围(与 WPF 版对齐)

新建/编辑/删除条目、分组管理(移动/排序)、组内拖拽排序、搜索过滤、
按类型分发启动、路径失效标记(磁贴变灰 + 裂纹叠加)、状态横幅(替代状态栏)。
不做:全局快捷键、多语言、自动更新(后续迭代)。

## 架构

```
src_unity/Spellbook.Unity/
  Assets/
    Scenes/Main.unity            空场景:相机 + Bootstrap
    Scripts/
      Core/                      纯 C# 无 Unity 依赖:SpellItem、ItemStore、LaunchKind、Launcher
      Tween/                     Tween.cs:位置/缩放/透明度/颜色缓动,链式 API
      UI/                        Theme(颜色/字号/间距令牌)、UIFactory(控件工厂)、
                                 IntroScreen、BookScreen、TileView、GroupTabs、
                                 SearchBar、EditDialog、IconPicker、Toast
      FX/                        AudioManager(音效/音乐)、ParticleFactory(法阵/余烬)
      Bootstrap.cs               入口:装配所有系统
    Art/    UI 边框、粒子贴图(Kenney CC0)
    Audio/  界面音效(Kenney CC0)、施法音效与音乐(OpenGameArt,逐项记录授权)
    Icons/  163 个手绘图标(复用 src/ 的 CC-BY 素材)
    Fonts/  Cinzel(标题,OFL)+ Noto Serif SC(中文正文,OFL)
    Editor/BuildScript.cs        -batchmode 一键构建 Windows 版
  Packages/manifest.json         URP、TMP、Newtonsoft JSON、Test Framework
  ProjectSettings/               公司名/产品名/分辨率 1280x800/窗口化
  CREDITS.md                     全部素材来源与授权清单
```

Core 层与 UI 完全隔离(不引用 UnityEngine),EditMode 测试覆盖:
JSON 读写与 WPF 版互通性、LaunchKind 分类、排序/分组逻辑。

## 风险与对策

- **Unity 许可证需账号登录激活**:自动化安装 Hub 与 Editor;若批处理构建因许可证
  失败,交付"打开 Hub 登录一次即可构建"的工程 + 文档,所有代码保证编译通过的
  置信度靠 EditMode 测试与严格移植(逻辑与 WPF 版一一对应)。
- **素材下载源失效**:全部使用 Kenney/OpenGameArt/Google Fonts 直链,失败则换
  等价 CC0 资源;CREDITS.md 记录最终实际来源。
- **中文渲染**:TMP 动态字体图集 + Noto Serif SC,启动时无需预烘焙。
