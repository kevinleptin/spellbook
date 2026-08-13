using System;
using Spellbook.UITween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>uGUI 控件工厂:所有界面元素由代码构建,这里是唯一的样板代码集中地。</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>铺满父级。</summary>
        public static RectTransform Fill(RectTransform rt, float margin = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        public static Image Panel(string name, Transform parent, Sprite sprite, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
            return img;
        }

        public static TextMeshProUGUI Text(
            string name, Transform parent, string content, int size, Color color,
            TMP_FontAsset font = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(name, parent);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font ?? Theme.BodyFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        /// <summary>
        /// 带悬停/按压反馈的按钮:悬停放大 + tick 音,按压缩小,点击回调。
        /// 具体动效在 HoverButton 组件里,保证全应用手感一致。
        /// </summary>
        public static HoverButton Button(
            string name, Transform parent, Sprite sprite, Color color, Action onClick)
        {
            var img = Panel(name, parent, sprite, color);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<HoverButton>();
            btn.OnClick = onClick;
            return btn;
        }

        /// <summary>文本输入框(TMP):深色底 + 金色描边聚焦效果。</summary>
        public static TMP_InputField Input(
            string name, Transform parent, string placeholder, bool multiLine = false)
        {
            var bg = Panel(name, parent, Theme.Sprite("panel_dark"), new Color(0.1f, 0.07f, 0.05f, 0.85f));
            bg.raycastTarget = true;

            var area = Rect("TextArea", bg.transform);
            Fill(area, 8f);
            area.gameObject.AddComponent<RectMask2D>();

            var textGo = Rect("Text", area);
            Fill(textGo);
            var text = textGo.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Theme.BodyFont;
            text.fontSize = Theme.FontBody;
            text.color = Theme.Parchment;
            text.alignment = multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left;

            var placeGo = Rect("Placeholder", area);
            Fill(placeGo);
            var place = placeGo.gameObject.AddComponent<TextMeshProUGUI>();
            place.font = Theme.BodyFont;
            place.fontSize = Theme.FontBody;
            place.fontStyle = FontStyles.Italic;
            place.color = new Color(0.9f, 0.85f, 0.7f, 0.35f);
            place.alignment = text.alignment;
            place.text = placeholder;

            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = place;
            input.caretColor = Theme.GoldBright;
            input.customCaretColor = true;
            input.selectionColor = new Color(0.78f, 0.66f, 0.32f, 0.45f);
            input.lineType = multiLine
                ? TMP_InputField.LineType.MultiLineNewline
                : TMP_InputField.LineType.SingleLine;
            return input;
        }

        /// <summary>垂直滚动区,返回内容容器(挂好 ScrollRect/Mask)。</summary>
        public static RectTransform ScrollView(string name, Transform parent, out ScrollRect scroll)
        {
            var viewport = Rect(name, parent);
            viewport.gameObject.AddComponent<RectMask2D>();
            var img = viewport.gameObject.AddComponent<Image>();
            img.color = Color.clear;   // 仅用于接收滚轮事件
            img.raycastTarget = true;

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            return content;
        }
    }

    /// <summary>统一按钮手感:悬停放大 1.06 + tick 音;按压 0.94;点击回调。</summary>
    public class HoverButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, IPointerClickHandler
    {
        public Action OnClick;
        public float HoverScale = 1.06f;
        public string HoverSound = "hover";
        public string ClickSound = "click";

        public void OnPointerEnter(PointerEventData e)
        {
            TweenRunner.KillAll(transform);
            transform.TweenScale(Vector3.one * HoverScale, 0.12f, Ease.OutQuad);
            if (!string.IsNullOrEmpty(HoverSound)) FX.AudioManager.Instance.Play(HoverSound, 0.4f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            TweenRunner.KillAll(transform);
            transform.TweenScale(Vector3.one, 0.15f, Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData e)
            => transform.TweenScale(Vector3.one * 0.94f, 0.06f, Ease.OutQuad);

        public void OnPointerUp(PointerEventData e)
            => transform.TweenScale(Vector3.one * HoverScale, 0.1f, Ease.OutQuad);

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (!string.IsNullOrEmpty(ClickSound)) FX.AudioManager.Instance.Play(ClickSound, 0.7f);
            OnClick?.Invoke();
        }
    }
}
