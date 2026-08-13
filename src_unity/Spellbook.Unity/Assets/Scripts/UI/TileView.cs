using System;
using Spellbook.Core;
using Spellbook.FX;
using Spellbook.UITween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 法术磁贴:魔兽技能按钮样式。悬停辉光脉动 + 放大,点击施法(粒子 + 音效),
    /// 右键上下文菜单,失效目标显示裂纹灰化,组内拖拽排序。
    /// </summary>
    public class TileView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float Size = 108f;

        public SpellItem Item { get; private set; }

        public Action<TileView> OnRun;
        public Action<TileView, Vector2> OnContextMenu;
        public Action<TileView, TileView> OnReorder;   // (source, target)

        private Image _frame;
        private Image _icon;
        private Image _missingOverlay;
        private TextMeshProUGUI _label;
        private GameObject _glow;
        private bool _missing;
        private RectTransform _dragGhost;

        /// <summary>构建磁贴视觉树并绑定条目。</summary>
        public static TileView Create(Transform parent, SpellItem item)
        {
            var rt = UIFactory.Rect("Tile_" + item.Name, parent);
            rt.sizeDelta = new Vector2(Size, Size + 26f);
            var view = rt.gameObject.AddComponent<TileView>();
            view.Build(item);
            return view;
        }

        private void Build(SpellItem item)
        {
            Item = item;

            // 图标底座(深色)+ 图标 + 鎏金边框
            var socket = UIFactory.Panel("Socket", transform, null, new Color(0.08f, 0.05f, 0.03f, 0.9f));
            var socketRt = socket.rectTransform;
            socketRt.anchorMin = new Vector2(0.5f, 1f);
            socketRt.anchorMax = new Vector2(0.5f, 1f);
            socketRt.pivot = new Vector2(0.5f, 1f);
            socketRt.sizeDelta = new Vector2(Size, Size);
            socketRt.anchoredPosition = Vector2.zero;
            socket.raycastTarget = true;

            _icon = UIFactory.Panel("Icon", socket.transform, Theme.Icon(item.IconKey), Color.white);
            UIFactory.Fill(_icon.rectTransform, 6f);
            _icon.raycastTarget = false;

            _frame = UIFactory.Panel("Frame", socket.transform, Theme.Sprite("tile_frame"), Theme.Gold);
            UIFactory.Fill(_frame.rectTransform);
            _frame.raycastTarget = false;

            _missingOverlay = UIFactory.Panel("Missing", socket.transform, null, new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Fill(_missingOverlay.rectTransform, 4f);
            _missingOverlay.raycastTarget = false;
            var warn = UIFactory.Text("Warn", _missingOverlay.transform, "⚠", 34, new Color(1f, 0.75f, 0.2f));
            UIFactory.Fill(warn.rectTransform);

            // 墨色文字:磁贴落在羊皮纸页面上,浅色会隐形
            _label = UIFactory.Text("Name", transform, item.Name, Theme.FontTile, Theme.Ink);
            var labelRt = _label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(0f, 22f);
            labelRt.anchoredPosition = Vector2.zero;

            RefreshMissing();
        }

        /// <summary>编辑后刷新图标/名称/失效状态。</summary>
        public void Refresh()
        {
            _icon.sprite = Theme.Icon(Item.IconKey);
            _label.text = Item.Name;
            RefreshMissing();
        }

        public void RefreshMissing()
        {
            _missing = Launcher.TargetMissing(Item.ScriptPath);
            _missingOverlay.gameObject.SetActive(_missing);
            _icon.color = _missing ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            _frame.color = _missing ? new Color(0.45f, 0.4f, 0.32f) : Theme.Gold;
        }

        /// <summary>失败反馈:红闪 + 横向抖动。</summary>
        public void ShakeError()
        {
            AudioManager.Instance.Play("error", 0.8f);
            var rt = (RectTransform)transform;
            var origin = rt.anchoredPosition;
            TweenExt.TweenValue(this, 0f, 1f, 0.4f, p =>
            {
                if (rt == null) return;
                rt.anchoredPosition = origin + new Vector2(
                    Mathf.Sin(p * Mathf.PI * 6f) * 7f * (1f - p), 0f);
            }, Ease.Linear).Then(() => { if (rt != null) rt.anchoredPosition = origin; });
            _frame.color = Theme.Danger;
            _frame.TweenColor(_missing ? new Color(0.45f, 0.4f, 0.32f) : Theme.Gold, 0.6f);
        }

        /// <summary>成功施法反馈:过曝闪光。</summary>
        public void FlashSuccess()
        {
            _icon.color = new Color(2f, 1.9f, 1.6f);
            _icon.TweenColor(Color.white, 0.45f, Ease.OutQuad);
        }

        // ―― 指针交互 ――

        public void OnPointerEnter(PointerEventData e)
        {
            TweenRunner.KillAll(transform);
            transform.TweenScale(Vector3.one * 1.08f, 0.12f, Ease.OutQuad);
            if (_glow == null)
            {
                var socket = transform.Find("Socket") as RectTransform;
                _glow = Fx.GlowPulse(socket, _missing
                    ? new Color(0.8f, 0.3f, 0.2f, 0.5f)
                    : new Color(Theme.GoldBright.r, Theme.GoldBright.g, Theme.GoldBright.b, 0.55f));
            }
            AudioManager.Instance.Play("hover", 0.35f);
            Tooltip.Show(Item);
        }

        public void OnPointerExit(PointerEventData e)
        {
            TweenRunner.KillAll(transform);
            transform.TweenScale(Vector3.one, 0.15f, Ease.OutQuad);
            if (_glow != null) { Destroy(_glow); _glow = null; }
            Tooltip.Hide();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left)
                transform.TweenScale(Vector3.one * 0.93f, 0.06f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left)
                transform.TweenScale(Vector3.one * 1.08f, 0.1f, Ease.OutQuad);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            if (e.button == PointerEventData.InputButton.Right)
            {
                OnContextMenu?.Invoke(this, e.position);
                return;
            }
            if (e.button != PointerEventData.InputButton.Left) return;

            OnRun?.Invoke(this);
        }

        // ―― 组内拖拽排序:拖出半透明幻影,落到其他磁贴上时插入其前 ――

        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            Tooltip.Hide();
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _dragGhost = UIFactory.Rect("Ghost", canvas.transform);
            _dragGhost.sizeDelta = new Vector2(Size, Size);
            var img = _dragGhost.gameObject.AddComponent<Image>();
            img.sprite = _icon.sprite;
            img.color = new Color(1f, 1f, 1f, 0.55f);
            img.raycastTarget = false;
            MoveGhost(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_dragGhost != null) MoveGhost(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_dragGhost == null) return;
            Destroy(_dragGhost.gameObject);
            _dragGhost = null;

            var target = e.pointerCurrentRaycast.gameObject != null
                ? e.pointerCurrentRaycast.gameObject.GetComponentInParent<TileView>()
                : null;
            if (target != null && target != this)
            {
                AudioManager.Instance.Play("drop", 0.7f);
                OnReorder?.Invoke(this, target);
            }
        }

        private void MoveGhost(PointerEventData e)
        {
            var canvas = _dragGhost.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform, e.position, canvas.worldCamera, out var pos);
            _dragGhost.anchoredPosition = pos;
        }

        private void OnDestroy()
        {
            if (_glow != null) Destroy(_glow);
        }
    }
}
