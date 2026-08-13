using Spellbook.UITween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 状态横幅:底部滑入的鎏金卷轴条,替代传统状态栏。
    /// 错误消息红色描边并停留更久。
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private static Toast _instance;
        private RectTransform _banner;
        private Image _bg;
        private TextMeshProUGUI _text;
        private TweenHandle _hideTween;

        public static void Attach(Transform canvasRoot)
        {
            var rt = UIFactory.Rect("Toast", canvasRoot);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(720f, 46f);
            rt.anchoredPosition = new Vector2(0f, -60f);   // 初始藏在屏幕下方
            _instance = rt.gameObject.AddComponent<Toast>();
            _instance.Build(rt);
        }

        private void Build(RectTransform rt)
        {
            _banner = rt;
            _bg = UIFactory.Panel("Bg", rt, Theme.Sprite("panel_dark"), Theme.PanelDark);
            UIFactory.Fill(_bg.rectTransform);
            _bg.raycastTarget = false;

            _text = UIFactory.Text("Text", rt, "", Theme.FontBody, Theme.Parchment);
            UIFactory.Fill(_text.rectTransform, 8f);
        }

        public static void Show(string message, bool isError = false)
        {
            if (_instance == null) return;
            var self = _instance;
            self._text.text = message;
            self._text.color = isError ? new Color(1f, 0.45f, 0.35f) : Theme.Parchment;
            self._bg.color = isError ? new Color(0.25f, 0.08f, 0.06f, 0.95f) : Theme.PanelDark;

            self._hideTween?.Kill();
            TweenRunner.KillAll(self._banner);
            self._banner.TweenAnchoredPos(new Vector2(0f, 18f), 0.3f, Ease.OutBack);
            self._hideTween = self._banner
                .TweenAnchoredPos(new Vector2(0f, -60f), 0.25f, Ease.InQuad, isError ? 4.5f : 2.5f);
        }
    }
}
