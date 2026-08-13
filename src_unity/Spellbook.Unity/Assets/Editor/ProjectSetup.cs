using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spellbook.EditorTools
{
    /// <summary>
    /// 工程自配置:首次打开(或每次域重载)时确保场景、构建设置、
    /// 播放器设置与粒子材质就绪。全部幂等,新克隆的仓库开箱即用。
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string MaterialDir = "Assets/Resources/Materials";

        static ProjectSetup()
        {
            // 延迟到首帧,避开资源库尚未就绪的时序
            EditorApplication.delayCall += EnsureAll;
        }

        [MenuItem("Spellbook/重新执行工程自配置")]
        public static void EnsureAll()
        {
            EnsureTmpEssentials();
            EnsureResourcesImported();
            EnsureScene();
            EnsurePlayerSettings();
            EnsureParticleMaterials();
            EnsureAlwaysIncludedShaders();
        }

        /// <summary>
        /// 首次打开工程时资源在编辑器脚本编译前导入,AssetImportPost 的规则
        /// (Sprite 类型、九宫格 border)未生效。检测到标志性贴图缺 border 时
        /// 强制重导整个 Resources 目录,幂等。
        /// </summary>
        private static void EnsureResourcesImported()
        {
            var probe = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art/tile_frame.png");
            if (probe != null && probe.border.sqrMagnitude > 0f) return;

            AssetDatabase.ImportAsset("Assets/Resources",
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// headless 生成的 GraphicsSettings 常驻着色器列表为空,UI/Default 等会被
        /// 构建裁剪导致整个界面品红。把 UI 必需着色器补进 Always Included。
        /// </summary>
        private static void EnsureAlwaysIncludedShaders()
        {
            var required = new[]
            {
                "UI/Default", "Sprites/Default", "Legacy Shaders/Particles/Additive",
            };
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;

            var so = new SerializedObject(gs);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            var existing = new System.Collections.Generic.HashSet<Object>();
            for (var i = 0; i < list.arraySize; i++)
            {
                existing.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            var changed = false;
            foreach (var name in required)
            {
                var shader = Shader.Find(name);
                if (shader == null || existing.Contains(shader)) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// TMP 必需资源(TMP Settings/默认字体/着色器):运行时 CreateFontAsset
        /// 依赖 TMP Settings 存在,否则空引用。从 ugui 包内置资源包自动导入。
        /// </summary>
        private static void EnsureTmpEssentials()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;
            AssetDatabase.ImportPackage(
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                interactive: false);
            AssetDatabase.Refresh();
        }

        private static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
                var scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            if (EditorBuildSettings.scenes.Length == 0
                || EditorBuildSettings.scenes[0].path != ScenePath)
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenePath, true),
                };
            }
        }

        private static void EnsurePlayerSettings()
        {
            PlayerSettings.productName = "Spellbook Arcane";
            PlayerSettings.companyName = "fourdvoid";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;   // 切窗口时音乐与动画不中断
            // Unity 6 起 Personal 可关闭 splash;浅色 splash 淡出会把开场动画罩成一片泛白
            PlayerSettings.SplashScreen.show = false;
        }

        /// <summary>
        /// 粒子加法材质存为 Resources 资产:保证 shader 进构建不被裁剪,
        /// 运行时 Resources.Load 即取。
        /// </summary>
        private static void EnsureParticleMaterials()
        {
            Directory.CreateDirectory(MaterialDir);
            CreateAdditive("EmberAdditive", "Assets/Resources/Art/particle_glow.png");
            CreateAdditive("SparkAdditive", "Assets/Resources/Art/particle_star.png");
            AssetDatabase.SaveAssets();
        }

        private static void CreateAdditive(string name, string texturePath)
        {
            var path = $"{MaterialDir}/{name}.mat";
            if (File.Exists(path)) return;

            var shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                Debug.LogWarning("找不到加法粒子 shader,粒子材质未创建");
                return;
            }
            var mat = new Material(shader);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null) mat.SetTexture("_MainTex", tex);
            AssetDatabase.CreateAsset(mat, path);
        }
    }
}
