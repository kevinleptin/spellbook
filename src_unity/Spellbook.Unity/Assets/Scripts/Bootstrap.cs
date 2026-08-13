using System;
using System.IO;
using Spellbook.Core;
using Spellbook.FX;
using Spellbook.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spellbook
{
    /// <summary>
    /// 应用入口:场景加载后以代码装配全部系统(相机/画布/氛围/界面)。
    /// 场景文件本身为空,所有内容 code-first 构建。
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Main()
        {
            Application.targetFrameRate = 120;
            Theme.Init();

            // ―― 相机:正交,深色底 ――
            var camGo = new GameObject("MainCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Backdrop;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // ―― 画布:Screen Space - Camera(粒子可与 UI 混排) ――
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 8f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystem);

            // ―― 氛围:背景余烬 + 环境音乐 ――
            var worldHeight = cam.orthographicSize * 2f;
            var worldWidth = worldHeight * ((float)Screen.width / Screen.height);
            Fx.Embers(null, new Vector2(worldWidth, worldHeight));
            AudioManager.Instance.PlayMusic("music");

            // ―― 数据与界面 ――
            var model = new SpellbookModel(new ItemStore());

            // 常驻顶层组件
            Toast.Attach(canvas.transform);
            Tooltip.Attach(canvas.transform);

            IntroScreen.Play(canvas.transform, () =>
            {
                BookScreen.Create(canvas.transform, model);
                // 让 Toast/Tooltip 保持在最顶层
                canvas.transform.Find("Toast").SetAsLastSibling();
                canvas.transform.Find("Tooltip").SetAsLastSibling();
            });

            // 自动化截图:--shots <目录>,从后备缓冲区导出(锁屏时窗口抓屏无效也能出图)
            var args = Environment.GetCommandLineArgs();
            var idx = Array.IndexOf(args, "--shots");
            if (idx >= 0 && idx + 1 < args.Length)
            {
                var runner = new GameObject("[Shots]").AddComponent<ShotRunner>();
                runner.OutDir = args[idx + 1];
            }
        }

        /// <summary>定时连拍:覆盖开场到主界面的关键时刻,拍完退出。</summary>
        private class ShotRunner : MonoBehaviour
        {
            private static readonly float[] Times = { 0.6f, 1.2f, 2.0f, 2.8f, 4f, 7f };
            public string OutDir;
            private float _t;
            private int _next;

            private void Update()
            {
                _t += Time.unscaledDeltaTime;
                if (_next < Times.Length && _t >= Times[_next])
                {
                    Directory.CreateDirectory(OutDir);
                    ScreenCapture.CaptureScreenshot(
                        Path.Combine(OutDir, $"shot-{Times[_next]:0.0}s.png"));
                    _next++;
                }
                if (_t >= 9f) Application.Quit();
            }
        }
    }
}
