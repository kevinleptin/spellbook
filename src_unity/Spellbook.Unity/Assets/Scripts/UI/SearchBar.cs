using System;
using Spellbook.UITween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 搜索条:Ctrl+K 呼出/收起,从顶部滑入并发光,实时回调过滤文本。
    /// Esc 或再次 Ctrl+K 收起并清空。
    /// </summary>
    public class SearchBar : MonoBehaviour
    {
        public Action<string> OnChanged;

        private RectTransform _root;
        private TMP_InputField _input;
        private bool _visible;

        public static SearchBar Create(Transform canvasRoot)
        {
            var rt = UIFactory.Rect("SearchBar", canvasRoot);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(520f, 54f);
            rt.anchoredPosition = new Vector2(0f, 70f);   // 藏于顶端之上
            var bar = rt.gameObject.AddComponent<SearchBar>();
            bar.Build(rt);
            return bar;
        }

        private void Build(RectTransform rt)
        {
            _root = rt;
            var glow = UIFactory.Panel("Glow", rt, Theme.Sprite("glow_soft"),
                new Color(Theme.Arcane.r, Theme.Arcane.g, Theme.Arcane.b, 0.35f));
            UIFactory.Fill(glow.rectTransform, -26f);
            glow.raycastTarget = false;

            _input = UIFactory.Input("Input", rt, "输入名称过滤法术…  (Esc 关闭)");
            UIFactory.Fill(((Image)_input.targetGraphic).rectTransform);
            _input.onValueChanged.AddListener(v => OnChanged?.Invoke(v));
        }

        private void Update()
        {
            var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.K)) Toggle();
            if (_visible && Input.GetKeyDown(KeyCode.Escape)) Toggle();
        }

        private void Toggle()
        {
            _visible = !_visible;
            FX.AudioManager.Instance.Play(_visible ? "open" : "close", 0.5f);
            TweenRunner.KillAll(_root);
            if (_visible)
            {
                _root.TweenAnchoredPos(new Vector2(0f, -16f), 0.3f, Ease.OutBack);
                _input.text = "";
                _input.ActivateInputField();
            }
            else
            {
                _root.TweenAnchoredPos(new Vector2(0f, 70f), 0.25f, Ease.InQuad);
                _input.text = "";
                _input.DeactivateInputField();
                OnChanged?.Invoke("");
            }
        }
    }
}
