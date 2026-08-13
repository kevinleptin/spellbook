using Spellbook.UITween;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.FX
{
    /// <summary>
    /// 粒子与光效工厂。粒子系统全部代码配置;材质来自 Resources/Materials
    /// (由 Editor/ProjectSetup 预生成,保证加法粒子 shader 不被构建裁剪)。
    /// </summary>
    public static class Fx
    {
        private static Material Mat(string name) =>
            Resources.Load<Material>("Materials/" + name);

        /// <summary>
        /// 全屏漂浮余烬:缓慢上升、闪烁的暖色粒子,置于书本后方,营造氛围。
        /// worldRect 为可视区域世界坐标尺寸。
        /// </summary>
        public static ParticleSystem Embers(Transform parent, Vector2 worldSize)
        {
            var go = new GameObject("Embers");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.55f, 0.2f, 0.8f), new Color(1f, 0.8f, 0.35f, 0.5f));
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(worldSize.x, worldSize.y, 0.1f);

            // 上升 + 轻微横向游移(x/y/z 必须同为双常量模式,否则每帧告警)
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // 尾段淡出
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f),
                });
            col.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = Mat("EmberAdditive");
            renderer.sortingOrder = 1;   // 书本(order 0 的 Canvas)之上、内容之下由层级控制
            return ps;
        }

        /// <summary>施法爆发:从某点向四周迸发的金色/奥术色火花,一次性。</summary>
        public static void CastBurst(Vector3 worldPos, Color color, int count = 36)
        {
            var go = new GameObject("CastBurst");
            go.transform.position = worldPos;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = 0.6f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.4f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = Mat("SparkAdditive");
            renderer.sortingOrder = 30;  // 盖在 UI 之上
            ps.Play();
        }

        /// <summary>
        /// UI 辉光脉冲:在目标 RectTransform 后插入软光晕图,循环缩放呼吸。
        /// 返回光晕对象以便悬停结束时销毁。
        /// </summary>
        public static GameObject GlowPulse(RectTransform target, Color color, float scale = 1.45f)
        {
            var go = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(target, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("Art/glow_soft");
            img.color = color;
            img.raycastTarget = false;

            rt.localScale = Vector3.one * 1.1f;
            PulseLoop(rt, scale);
            return go;
        }

        private static void PulseLoop(RectTransform rt, float scale)
        {
            if (rt == null) return;
            rt.TweenScale(Vector3.one * scale, 0.7f, Ease.InOutCubic)
              .Then(() =>
              {
                  if (rt == null) return;
                  rt.TweenScale(Vector3.one * 1.1f, 0.7f, Ease.InOutCubic)
                    .Then(() => PulseLoop(rt, scale));
              });
        }

        /// <summary>全屏白光爆闪后淡出(开场翻书、施法成功用)。</summary>
        public static void ScreenFlash(Transform canvasRoot, Color color, float alpha = 0.55f)
        {
            var go = new GameObject("Flash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvasRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = alpha;
            cg.TweenAlpha(0f, 0.5f, Ease.OutQuad).Then(() => Object.Destroy(go));
        }
    }
}
