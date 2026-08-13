using System;
using System.Collections.Generic;
using Spellbook.UITween;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>右键菜单:深色卷轴条目列表,点击空白处关闭。</summary>
    public static class ContextMenu
    {
        public static void Show(
            Transform canvasRoot, Vector2 screenPos, List<(string label, Action action)> entries)
        {
            var layer = UIFactory.Rect("ContextLayer", canvasRoot);
            UIFactory.Fill(layer);

            // 透明遮罩:点击任意处关闭
            var dim = UIFactory.Panel("Dim", layer, null, Color.clear);
            UIFactory.Fill(dim.rectTransform);
            dim.raycastTarget = true;
            void CloseMenu() { if (layer != null) UnityEngine.Object.Destroy(layer.gameObject); }
            dim.gameObject.AddComponent<ClickCatcher>().OnAnyClick = CloseMenu;

            const float itemH = 38f;
            const float width = 190f;
            var menu = UIFactory.Panel("Menu", layer, Theme.Sprite("panel_dark"), Theme.PanelDark);
            var menuRt = menu.rectTransform;
            menuRt.pivot = new Vector2(0f, 1f);
            menuRt.sizeDelta = new Vector2(width, entries.Count * itemH + 12f);

            var canvas = canvasRoot.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvasRoot, screenPos, canvas.worldCamera, out var localPos);
            // 越界翻转
            var rect = ((RectTransform)canvasRoot).rect;
            if (localPos.x + width > rect.xMax) localPos.x -= width;
            if (localPos.y - menuRt.sizeDelta.y < rect.yMin) localPos.y += menuRt.sizeDelta.y;
            menuRt.anchoredPosition = localPos;

            menuRt.localScale = new Vector3(1f, 0.6f, 1f);
            menuRt.TweenScale(Vector3.one, 0.15f, Ease.OutQuad);

            for (var i = 0; i < entries.Count; i++)
            {
                var (label, action) = entries[i];
                var item = UIFactory.Button("Item_" + label, menu.transform, null, Color.clear, () =>
                {
                    CloseMenu();
                    action();
                });
                var rt = item.GetComponent<Image>().rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, itemH);
                rt.anchoredPosition = new Vector2(0f, -6f - i * itemH);
                item.HoverScale = 1.02f;

                var text = UIFactory.Text("T", rt, label, Theme.FontBody, Theme.Parchment,
                    null, TMPro.TextAlignmentOptions.Left);
                UIFactory.Fill(text.rectTransform, 12f);
                text.raycastTarget = false;
            }
        }

        /// <summary>任意鼠标键点击都触发(含右键,避免右键再开一个菜单时旧菜单残留)。</summary>
        private class ClickCatcher : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler
        {
            public Action OnAnyClick;
            public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
                => OnAnyClick?.Invoke();
        }
    }
}
