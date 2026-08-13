using System;
using System.Collections.Generic;
using Spellbook.UITween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 分组书签栏:书页右缘垂直排列的书签,选中的抽出并发光。
    /// "未分组"显示为固定书签(存在未分组条目时)。
    /// </summary>
    public class GroupTabs : MonoBehaviour
    {
        public Action<string> OnSelect;

        private readonly List<(string key, RectTransform rt, Image bg, TextMeshProUGUI label)> _tabs
            = new List<(string, RectTransform, Image, TextMeshProUGUI)>();
        private string _selected;

        public static GroupTabs Create(Transform parent)
        {
            var rt = UIFactory.Rect("GroupTabs", parent);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(150f, -120f);
            rt.anchoredPosition = new Vector2(-8f, 0f);
            return rt.gameObject.AddComponent<GroupTabs>();
        }

        /// <summary>重建书签列表(分组增删后调用)。</summary>
        public void Rebuild(List<string> groupKeys, string selected)
        {
            _selected = selected;
            foreach (var tab in _tabs) Destroy(tab.rt.gameObject);
            _tabs.Clear();

            const float tabH = 52f;
            for (var i = 0; i < groupKeys.Count; i++)
            {
                var key = groupKeys[i];
                var display = key.Length == 0 ? "未分组" : key;

                var bg = UIFactory.Panel("Tab_" + display, transform,
                    Theme.Sprite("panel_dark"), TabColor(key, key == selected));
                var rt = bg.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(146f, tabH - 6f);
                rt.anchoredPosition = new Vector2(key == selected ? -26f : 0f, -8f - i * tabH);
                bg.raycastTarget = true;

                var label = UIFactory.Text("T", rt, display, Theme.FontBody,
                    key == selected ? Theme.GoldBright : Theme.Parchment,
                    null, TextAlignmentOptions.Left);
                UIFactory.Fill(label.rectTransform, 10f);
                label.raycastTarget = false;

                var captured = key;
                var btn = bg.gameObject.AddComponent<HoverButton>();
                btn.HoverScale = 1.04f;
                btn.HoverSound = "hover";
                btn.ClickSound = null;   // 翻页音由 BookScreen 播,避免双音
                btn.OnClick = () => { if (captured != _selected) OnSelect?.Invoke(captured); };

                _tabs.Add((key, rt, bg, label));
            }
        }

        /// <summary>切换选中态:书签抽出/收回动画。</summary>
        public void SetSelected(string key)
        {
            _selected = key;
            foreach (var (k, rt, bg, label) in _tabs)
            {
                var isSel = k == key;
                rt.TweenAnchoredPos(new Vector2(isSel ? -26f : 0f, rt.anchoredPosition.y),
                    0.25f, Ease.OutBack);
                bg.TweenColor(TabColor(k, isSel), 0.25f);
                label.color = isSel ? Theme.GoldBright : Theme.Parchment;
            }
        }

        private static Color TabColor(string key, bool selected) => selected
            ? new Color(0.32f, 0.22f, 0.1f, 0.98f)
            : new Color(0.16f, 0.11f, 0.07f, 0.92f);
    }
}
