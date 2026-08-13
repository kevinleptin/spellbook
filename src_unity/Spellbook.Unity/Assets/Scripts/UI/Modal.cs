using System;
using Spellbook.FX;
using Spellbook.UITween;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 模态层基建:半透明遮罩 + 中央面板"展开"动画(Y 向缩放带回弹)。
    /// 点击遮罩 = 取消。所有对话框(编辑/确认/图标选择)基于此构建。
    /// </summary>
    public static class Modal
    {
        /// <summary>打开模态面板,返回内容容器;close 委托用于程序化关闭。</summary>
        public static RectTransform Open(
            Transform canvasRoot, Vector2 size, out Action close, Action onCancel = null)
        {
            var layer = UIFactory.Rect("ModalLayer", canvasRoot);
            UIFactory.Fill(layer);

            var dim = UIFactory.Panel("Dim", layer, null, new Color(0f, 0f, 0f, 0f));
            UIFactory.Fill(dim.rectTransform);
            dim.raycastTarget = true;
            dim.TweenColor(new Color(0f, 0f, 0f, 0.62f), 0.25f);

            var panel = UIFactory.Panel("Panel", layer, Theme.Sprite("panel_parchment"), Theme.Parchment);
            var panelRt = panel.rectTransform;
            panelRt.sizeDelta = size;
            panel.raycastTarget = true;

            // 卷轴展开:横向先到位,纵向回弹展开
            panelRt.localScale = new Vector3(1f, 0.05f, 1f);
            panelRt.TweenScale(Vector3.one, 0.35f, Ease.OutBack);
            AudioManager.Instance.Play("open", 0.7f);

            var closed = false;
            void Close()
            {
                if (closed || layer == null) return;
                closed = true;
                AudioManager.Instance.Play("close", 0.7f);
                dim.TweenColor(new Color(0f, 0f, 0f, 0f), 0.2f);
                panelRt.TweenScale(new Vector3(1f, 0.05f, 1f), 0.18f, Ease.InQuad)
                       .Then(() => { if (layer != null) UnityEngine.Object.Destroy(layer.gameObject); });
            }

            close = Close;
            var closeFn = close;
            dim.gameObject.AddComponent<DimClick>().OnClickAction = () =>
            {
                onCancel?.Invoke();
                closeFn();
            };
            return panelRt;
        }

        /// <summary>遮罩点击组件(避免 lambda 无法挂到 EventTrigger 的样板)。</summary>
        private class DimClick : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
        {
            public Action OnClickAction;
            public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
                => OnClickAction?.Invoke();
        }
    }

    /// <summary>通用确认框:标题 + 消息 + 确认/取消。</summary>
    public static class ConfirmDialog
    {
        public static void Show(Transform canvasRoot, string title, string message, Action onConfirm)
        {
            var panel = Modal.Open(canvasRoot, new Vector2(460f, 240f), out var close);

            var titleText = UIFactory.Text("Title", panel, title, Theme.FontGroupTitle, Theme.Ink,
                Theme.TitleFont);
            var titleRt = titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(20f, -66f);
            titleRt.offsetMax = new Vector2(-20f, -18f);

            var msg = UIFactory.Text("Msg", panel, message, Theme.FontBody, Theme.InkFaded);
            msg.textWrappingMode = TMPro.TextWrappingModes.Normal;
            var msgRt = msg.rectTransform;
            msgRt.anchorMin = new Vector2(0f, 0.5f);
            msgRt.anchorMax = new Vector2(1f, 1f);
            msgRt.offsetMin = new Vector2(24f, 0f);
            msgRt.offsetMax = new Vector2(-24f, -70f);

            MakeActionButton(panel, "确认", new Vector2(-90f, 46f), Theme.Danger, Color.white, () =>
            {
                close();
                onConfirm();
            });
            MakeActionButton(panel, "取消", new Vector2(90f, 46f), new Color(0.35f, 0.28f, 0.2f),
                Theme.Parchment, close);
        }

        /// <summary>底部动作按钮(确认框与编辑框共用样式)。</summary>
        public static HoverButton MakeActionButton(
            RectTransform panel, string label, Vector2 bottomCenterPos,
            Color bg, Color fg, Action onClick)
        {
            var btn = UIFactory.Button("Btn_" + label, panel, Theme.Sprite("panel_dark"), bg, onClick);
            var rt = ((Image)btn.GetComponent<Image>()).rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(150f, 44f);
            rt.anchoredPosition = bottomCenterPos;
            var text = UIFactory.Text("Label", rt, label, Theme.FontBody, fg);
            UIFactory.Fill(text.rectTransform);
            text.raycastTarget = false;
            return btn;
        }
    }
}
