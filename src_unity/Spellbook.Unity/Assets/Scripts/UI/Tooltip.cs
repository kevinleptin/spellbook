using Spellbook.Core;
using Spellbook.UITween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 磁贴悬浮说明:名称 + 完整路径与参数 + 备注,跟随鼠标,短延迟淡入。
    /// 单例,由 Bootstrap 创建在最顶层。
    /// </summary>
    public class Tooltip : MonoBehaviour
    {
        private static Tooltip _instance;

        private RectTransform _root;
        private CanvasGroup _group;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _path;
        private TextMeshProUGUI _notes;
        private float _showAt = -1f;

        public static void Attach(Transform canvasRoot)
        {
            var rt = UIFactory.Rect("Tooltip", canvasRoot);
            _instance = rt.gameObject.AddComponent<Tooltip>();
            _instance.Build(rt);
        }

        private void Build(RectTransform rt)
        {
            _root = rt;
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = new Vector2(380f, 110f);

            var panel = UIFactory.Panel("Bg", rt, Theme.Sprite("panel_dark"), Theme.PanelDark);
            UIFactory.Fill(panel.rectTransform);
            panel.raycastTarget = false;

            _name = UIFactory.Text("Name", rt, "", Theme.FontBody, Theme.GoldBright,
                Theme.TitleFont, TextAlignmentOptions.TopLeft);
            var nameRt = _name.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.offsetMin = new Vector2(14f, -40f);
            nameRt.offsetMax = new Vector2(-14f, -10f);

            _path = UIFactory.Text("Path", rt, "", Theme.FontSmall, new Color(0.85f, 0.8f, 0.65f),
                null, TextAlignmentOptions.TopLeft);
            var pathRt = _path.rectTransform;
            pathRt.anchorMin = new Vector2(0f, 1f);
            pathRt.anchorMax = new Vector2(1f, 1f);
            pathRt.pivot = new Vector2(0.5f, 1f);
            pathRt.offsetMin = new Vector2(14f, -66f);
            pathRt.offsetMax = new Vector2(-14f, -42f);

            _notes = UIFactory.Text("Notes", rt, "", Theme.FontSmall, new Color(0.7f, 0.78f, 0.85f),
                null, TextAlignmentOptions.TopLeft);
            _notes.textWrappingMode = TextWrappingModes.Normal;
            var notesRt = _notes.rectTransform;
            notesRt.anchorMin = new Vector2(0f, 0f);
            notesRt.anchorMax = new Vector2(1f, 1f);
            notesRt.offsetMin = new Vector2(14f, 10f);
            notesRt.offsetMax = new Vector2(-14f, -68f);

            _group = rt.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        public static void Show(SpellItem item)
        {
            if (_instance == null) return;
            _instance._name.text = item.Name;
            _instance._path.text = string.IsNullOrWhiteSpace(item.Arguments)
                ? item.ScriptPath
                : $"{item.ScriptPath} {item.Arguments}";
            var hasNotes = !string.IsNullOrWhiteSpace(item.Notes);
            _instance._notes.text = hasNotes ? item.Notes : "";
            _instance._root.sizeDelta = new Vector2(380f, hasNotes ? 130f : 78f);
            _instance._showAt = Time.unscaledTime + 0.35f;   // 延迟显示,快速扫过不打扰
        }

        public static void Hide()
        {
            if (_instance == null) return;
            _instance._showAt = -1f;
            TweenRunner.KillAll(_instance._group);
            _instance._group.TweenAlpha(0f, 0.1f);
        }

        private void Update()
        {
            if (_showAt > 0f && Time.unscaledTime >= _showAt)
            {
                _showAt = -1f;
                TweenRunner.KillAll(_group);
                _group.TweenAlpha(1f, 0.15f);
            }
            if (_group.alpha <= 0f && _showAt < 0f) return;

            // 跟随鼠标,越界翻转
            var canvas = GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform, Input.mousePosition, canvas.worldCamera, out var pos);
            var canvasRect = ((RectTransform)canvas.transform).rect;
            var offset = new Vector2(18f, -18f);
            if (pos.x + _root.sizeDelta.x + 30f > canvasRect.xMax) offset.x = -_root.sizeDelta.x - 12f;
            if (pos.y - _root.sizeDelta.y - 30f < canvasRect.yMin) offset.y = _root.sizeDelta.y + 12f;
            _root.anchoredPosition = pos + offset;
        }
    }
}
