using System;
using System.Linq;
using Spellbook.UITween;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 图标选择器:仿魔兽"新建宏命令"的图标网格。
    /// 全部图标交错淡入,悬停放大,当前选中项鎏金描边。
    /// </summary>
    public static class IconPicker
    {
        private const int Columns = 8;
        private const float Cell = 64f;

        public static void Show(Transform canvasRoot, string currentKey, Action<string> onPick)
        {
            var panel = Modal.Open(canvasRoot, new Vector2(620f, 560f), out var close);

            var title = UIFactory.Text("Title", panel, "选择图标", Theme.FontGroupTitle, Theme.Ink);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(20f, -60f);
            titleRt.offsetMax = new Vector2(-20f, -14f);

            var viewportHolder = UIFactory.Rect("GridHolder", panel);
            viewportHolder.anchorMin = new Vector2(0f, 0f);
            viewportHolder.anchorMax = new Vector2(1f, 1f);
            viewportHolder.offsetMin = new Vector2(28f, 24f);
            viewportHolder.offsetMax = new Vector2(-28f, -66f);

            var content = UIFactory.ScrollView("Scroll", viewportHolder, out _);
            UIFactory.Fill((RectTransform)content.parent);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(Cell, Cell);
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 图标按名称排序,保证与 WPF 版选择器顺序一致
            var sprites = Resources.LoadAll<Sprite>("Icons").OrderBy(s => s.name).ToArray();
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var key = sprite.name;

                var cellImg = UIFactory.Panel("Cell_" + key, content, null,
                    new Color(0.08f, 0.05f, 0.03f, 0.9f));
                cellImg.raycastTarget = true;

                var icon = UIFactory.Panel("Icon", cellImg.transform, sprite, Color.white);
                UIFactory.Fill(icon.rectTransform, 3f);
                icon.raycastTarget = false;

                if (key == currentKey)
                {
                    var sel = UIFactory.Panel("Selected", cellImg.transform,
                        Theme.Sprite("tile_frame"), Theme.GoldBright);
                    UIFactory.Fill(sel.rectTransform);
                    sel.raycastTarget = false;
                }

                var btn = cellImg.gameObject.AddComponent<HoverButton>();
                btn.HoverScale = 1.15f;
                btn.OnClick = () =>
                {
                    onPick(key);
                    close();
                };

                // 交错淡入:每个格子按索引延迟,营造"图册翻开"感
                var cg = cellImg.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.TweenAlpha(1f, 0.2f, Ease.OutQuad, 0.008f * i);
            }
        }
    }
}
