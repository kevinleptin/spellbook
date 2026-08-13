using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UITween
{
    /// <summary>缓动函数集(t ∈ [0,1])。</summary>
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InQuad(float t) => t * t;
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        /// <summary>回弹超出后收回,适合弹窗展开。</summary>
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>弹性震荡,适合强调。</summary>
        public static float OutElastic(float t)
        {
            const float c4 = 2f * Mathf.PI / 3f;
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }

    /// <summary>一条进行中的补间。到期后自动移除;target 销毁时静默终止。</summary>
    public class TweenHandle
    {
        internal UnityEngine.Object Target;      // 生命周期锚(销毁即停)
        internal float Delay, Duration, Elapsed;
        internal Func<float, float> EaseFn = Ease.OutCubic;
        internal Action<float> Apply;            // 收到缓动后的进度 0..1
        internal Action OnComplete;
        internal bool Done;

        /// <summary>完成回调(链式)。</summary>
        public TweenHandle Then(Action action) { OnComplete += action; return this; }

        public void Kill() => Done = true;
    }

    /// <summary>
    /// 轻量补间系统:场景常驻单例逐帧驱动。
    /// 用法:transform.TweenScale(Vector3.one, 0.3f, Ease.OutBack)。
    /// </summary>
    public class TweenRunner : MonoBehaviour
    {
        private static TweenRunner _instance;
        private readonly List<TweenHandle> _tweens = new List<TweenHandle>();

        public static TweenRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[TweenRunner]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<TweenRunner>();
                }
                return _instance;
            }
        }

        public static TweenHandle Run(TweenHandle tween)
        {
            Instance._tweens.Add(tween);
            return tween;
        }

        /// <summary>终止某对象上的全部补间(不触发完成回调)。</summary>
        public static void KillAll(UnityEngine.Object target)
        {
            if (_instance == null) return;
            foreach (var t in _instance._tweens)
            {
                if (t.Target == target) t.Done = true;
            }
        }

        private void Update()
        {
            for (var i = _tweens.Count - 1; i >= 0; i--)
            {
                var t = _tweens[i];
                // 目标已销毁:静默移除
                if (t.Done || t.Target == null)
                {
                    _tweens.RemoveAt(i);
                    continue;
                }

                t.Elapsed += Time.unscaledDeltaTime;
                var time = t.Elapsed - t.Delay;
                if (time < 0f) continue;

                var p = t.Duration <= 0f ? 1f : Mathf.Clamp01(time / t.Duration);
                t.Apply(t.EaseFn(p));
                if (p >= 1f)
                {
                    t.Done = true;
                    _tweens.RemoveAt(i);
                    t.OnComplete?.Invoke();
                }
            }
        }
    }

    /// <summary>常用补间扩展。全部使用 unscaled time,不受暂停影响。</summary>
    public static class TweenExt
    {
        public static TweenHandle TweenValue(
            UnityEngine.Object target, float from, float to, float duration,
            Action<float> onUpdate, Func<float, float> ease = null, float delay = 0f)
        {
            return TweenRunner.Run(new TweenHandle
            {
                Target = target,
                Duration = duration,
                Delay = delay,
                EaseFn = ease ?? Ease.OutCubic,
                Apply = p => onUpdate(Mathf.LerpUnclamped(from, to, p)),
            });
        }

        public static TweenHandle TweenScale(
            this Transform t, Vector3 to, float duration,
            Func<float, float> ease = null, float delay = 0f)
        {
            var from = t.localScale;
            return TweenRunner.Run(new TweenHandle
            {
                Target = t,
                Duration = duration,
                Delay = delay,
                EaseFn = ease ?? Ease.OutCubic,
                Apply = p => t.localScale = Vector3.LerpUnclamped(from, to, p),
            });
        }

        public static TweenHandle TweenAnchoredPos(
            this RectTransform t, Vector2 to, float duration,
            Func<float, float> ease = null, float delay = 0f)
        {
            var from = t.anchoredPosition;
            return TweenRunner.Run(new TweenHandle
            {
                Target = t,
                Duration = duration,
                Delay = delay,
                EaseFn = ease ?? Ease.OutCubic,
                Apply = p => t.anchoredPosition = Vector2.LerpUnclamped(from, to, p),
            });
        }

        public static TweenHandle TweenRotationZ(
            this Transform t, float toDegrees, float duration,
            Func<float, float> ease = null, float delay = 0f)
        {
            var from = t.localEulerAngles.z;
            if (from > 180f) from -= 360f;
            return TweenRunner.Run(new TweenHandle
            {
                Target = t,
                Duration = duration,
                Delay = delay,
                EaseFn = ease ?? Ease.OutCubic,
                Apply = p => t.localEulerAngles =
                    new Vector3(0f, 0f, Mathf.LerpUnclamped(from, toDegrees, p)),
            });
        }

        public static TweenHandle TweenAlpha(
            this CanvasGroup g, float to, float duration,
            Func<float, float> ease = null, float delay = 0f)
        {
            var from = g.alpha;
            return TweenRunner.Run(new TweenHandle
            {
                Target = g,
                Duration = duration,
                Delay = delay,
                EaseFn = ease ?? Ease.OutCubic,
                Apply = p => g.alpha = Mathf.LerpUnclamped(from, to, p),
            });
        }

        public static TweenHandle TweenColor(
            this Graphic g, Color to, float duration,
            Func<float, float> ease = null, float delay = 0f)
        {
            var from = g.color;
            return TweenRunner.Run(new TweenHandle
            {
                Target = g,
                Duration = duration,
                Delay = delay,
                EaseFn = ease ?? Ease.OutCubic,
                Apply = p => g.color = Color.LerpUnclamped(from, to, p),
            });
        }
    }
}
