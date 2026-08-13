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
            EnsureScene();
            EnsurePlayerSettings();
            EnsureParticleMaterials();
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
