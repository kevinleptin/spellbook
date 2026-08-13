using System;
using Spellbook.FX;
using Spellbook.UITween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 开场:暗幕中央一本封面法术书,符文辉光呼吸;点击或短暂停留后
    /// 封面翻开(X 向压缩模拟翻转)+ 光爆 + 翻页音,过渡进入主界面。
    /// </summary>
    public class IntroScreen : MonoBehaviour, IPointerClickHandler
    {
        private Action _onDone;
        private RectTransform _cover;
        private bool _opened;

        public static void Play(Transform canvasRoot, Action onDone)
        {
            var rt = UIFactory.Rect("Intro", canvasRoot);
            UIFactory.Fill(rt);
            var intro = rt.gameObject.AddComponent<IntroScreen>();
            intro._onDone = onDone;
            intro.Build(rt);
        }

        private void Build(RectTransform root)
        {
            // 全屏暗幕(可点击跳过)
            var dim = UIFactory.Panel("Dim", root, null, Theme.Backdrop);
            UIFactory.Fill(dim.rectTransform);
            dim.raycastTarget = true;

            // 封面:深色面板 + 鎏金边 + 中央发光书形图标 + 标题
            var cover = UIFactory.Panel("Cover", root, Theme.Sprite("panel_dark"),
                new Color(0.14f, 0.09f, 0.06f, 1f));
            _cover = cover.rectTransform;
            _cover.sizeDelta = new Vector2(420f, 560f);

            var frame = UIFactory.Panel("Frame", _cover, Theme.Sprite("tile_frame"), Theme.Gold);
            UIFactory.Fill(frame.rectTransform, -6f);
            frame.raycastTarget = false;

            var emblemHolder = UIFactory.Rect("Emblem", _cover);
            emblemHolder.sizeDelta = new Vector2(180f, 180f);
            emblemHolder.anchoredPosition = new Vector2(0f, 60f);
            Fx.GlowPulse(emblemHolder, new Color(1f, 0.72f, 0.25f, 0.6f), 1.7f);
            var emblem = UIFactory.Panel("Icon", emblemHolder, Theme.Icon("book"), Color.white);
            UIFactory.Fill(emblem.rectTransform);
            emblem.raycastTarget = false;

            var title = UIFactory.Text("Title", _cover, "Spellbook", 44, Theme.GoldBright,
                Theme.TitleFont);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(1f, 0f);
            titleRt.pivot = new Vector2(0.5f, 0f);
            titleRt.sizeDelta = new Vector2(0f, 60f);
            titleRt.anchoredPosition = new Vector2(0f, 120f);

            var hintText = UIFactory.Text("Hint", _cover, "点击翻开", Theme.FontSmall,
                new Color(0.8f, 0.72f, 0.5f, 0f));
            var hintRt = hintText.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(0f, 30f);
            hintRt.anchoredPosition = new Vector2(0f, 60f);
            hintText.TweenColor(new Color(0.8f, 0.72f, 0.5f, 0.9f), 0.8f, Ease.OutQuad, 0.6f);

            // 封面入场:自远处落下
            _cover.localScale = Vector3.one * 1.25f;
            var cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.TweenAlpha(1f, 0.45f, Ease.OutQuad);
            _cover.TweenScale(Vector3.one, 0.6f, Ease.OutCubic);

            // 1.6 秒后自动翻开(点击可提前)
            TweenExt.TweenValue(this, 0f, 1f, 1.6f, _ => { }).Then(Open);
        }

        public void OnPointerClick(PointerEventData e) => Open();

        private void Open()
        {
            if (_opened || this == null) return;
            _opened = true;

            AudioManager.Instance.PlayVariant("page", 3, 1f);
            Fx.ScreenFlash(transform.parent, new Color(1f, 0.9f, 0.6f));

            // 封面横向压缩到 0(翻转离场),整体淡出后进入主界面
            _cover.TweenScale(new Vector3(0.02f, 1.06f, 1f), 0.4f, Ease.InQuad);
            var cg = GetComponent<CanvasGroup>();
            cg.TweenAlpha(0f, 0.5f, Ease.OutQuad, 0.3f).Then(() =>
            {
                var done = _onDone;
                Destroy(gameObject);
                done?.Invoke();
            });
        }
    }
}
