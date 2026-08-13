using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Spellbook.Core;
using Spellbook.FX;
using Spellbook.UITween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 主界面:摊开的法术书。左页磁贴网格 + 右缘分组书签 + 顶栏 + 搜索。
    /// 分组切换 = 翻页过场;点击磁贴 = 施法启动;脚本退出码轮询回填横幅。
    /// </summary>
    public class BookScreen : MonoBehaviour
    {
        private SpellbookModel _model;
        private Transform _canvasRoot;

        private RectTransform _bookPanel;
        private RectTransform _pageHolder;
        private RectTransform _currentPage;
        private GroupTabs _tabs;
        private TextMeshProUGUI _groupTitle;
        private string _selectedGroup = "";
        private string _search = "";

        // 运行中的脚本:轮询退出码
        private readonly List<(Process process, string name)> _running
            = new List<(Process, string)>();

        public static BookScreen Create(Transform canvasRoot, SpellbookModel model)
        {
            var rt = UIFactory.Rect("BookScreen", canvasRoot);
            UIFactory.Fill(rt);
            var screen = rt.gameObject.AddComponent<BookScreen>();
            screen._model = model;
            screen._canvasRoot = canvasRoot;
            screen.Build(rt);
            return screen;
        }

        private void Build(RectTransform root)
        {
            // ―― 书本主体:羊皮纸大面板 ――
            var book = UIFactory.Panel("Book", root, Theme.Sprite("panel_parchment"), Theme.Parchment);
            _bookPanel = book.rectTransform;
            _bookPanel.anchorMin = new Vector2(0.5f, 0.5f);
            _bookPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _bookPanel.sizeDelta = new Vector2(1150f, 690f);
            book.raycastTarget = true;

            // 顶栏:标题 + 操作按钮
            var title = UIFactory.Text("Title", _bookPanel, "Spellbook", Theme.FontTitle,
                new Color(0.42f, 0.3f, 0.12f), Theme.TitleFont, TextAlignmentOptions.Left);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(0f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.sizeDelta = new Vector2(320f, 48f);
            titleRt.anchoredPosition = new Vector2(46f, -26f);

            _groupTitle = UIFactory.Text("GroupTitle", _bookPanel, "", Theme.FontGroupTitle,
                Theme.Ink, Theme.TitleFont, TextAlignmentOptions.Left);
            var gtRt = _groupTitle.rectTransform;
            gtRt.anchorMin = new Vector2(0f, 1f);
            gtRt.anchorMax = new Vector2(1f, 1f);
            gtRt.pivot = new Vector2(0.5f, 1f);
            gtRt.offsetMin = new Vector2(50f, -118f);
            gtRt.offsetMax = new Vector2(-170f, -82f);

            MakeTopButton("＋ 缮写", -46f, new Color(0.55f, 0.42f, 0.15f), Color.white,
                () => EditDialog.Show(_canvasRoot, NamedGroups(), null, OnCreateConfirmed));
            MakeTopButton("音效", -160f, new Color(0.3f, 0.24f, 0.16f),
                AudioManager.Instance.Muted ? new Color(0.6f, 0.55f, 0.45f) : Theme.GoldBright,
                ToggleMute);

            // 搜索提示
            var hint = UIFactory.Text("SearchHint", _bookPanel, "Ctrl+K 检索", Theme.FontSmall,
                Theme.InkFaded, null, TextAlignmentOptions.Right);
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(1f, 1f);
            hintRt.anchorMax = new Vector2(1f, 1f);
            hintRt.pivot = new Vector2(1f, 1f);
            hintRt.sizeDelta = new Vector2(160f, 24f);
            hintRt.anchoredPosition = new Vector2(-270f, -34f);

            // 页容器(翻页动画的舞台,裁剪超出部分)
            _pageHolder = UIFactory.Rect("PageHolder", _bookPanel);
            _pageHolder.anchorMin = Vector2.zero;
            _pageHolder.anchorMax = Vector2.one;
            _pageHolder.offsetMin = new Vector2(40f, 34f);
            _pageHolder.offsetMax = new Vector2(-166f, -126f);
            _pageHolder.gameObject.AddComponent<RectMask2D>();

            // 分组书签
            _tabs = GroupTabs.Create(_bookPanel);
            _tabs.OnSelect = SwitchGroup;

            // 搜索条
            var search = SearchBar.Create(_canvasRoot);
            search.OnChanged = ApplySearch;

            // 初始分组与页面
            var keys = _model.GroupKeys();
            _selectedGroup = keys.Count > 0 ? keys[0] : "";
            _tabs.Rebuild(keys, _selectedGroup);
            _currentPage = BuildPage(_selectedGroup);
            UpdateGroupTitle();

            if (_model.LoadFailed)
            {
                Toast.Show("items.json 已损坏,本次以空法术书启动(原文件未被覆盖)", isError: true);
            }
        }

        private void MakeTopButton(string label, float xFromRight, Color bg, Color fg, Action onClick)
        {
            var btn = UIFactory.Button("Top_" + label, _bookPanel, Theme.Sprite("panel_dark"), bg, onClick);
            var rt = btn.GetComponent<Image>().rectTransform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(104f, 38f);
            rt.anchoredPosition = new Vector2(xFromRight, -28f);
            var text = UIFactory.Text("T", rt, label, Theme.FontSmall, fg);
            UIFactory.Fill(text.rectTransform);
            text.raycastTarget = false;
        }

        private void ToggleMute()
        {
            var audio = AudioManager.Instance;
            audio.Muted = !audio.Muted;
            var btn = _bookPanel.Find("Top_音效/T").GetComponent<TextMeshProUGUI>();
            btn.color = audio.Muted ? new Color(0.6f, 0.55f, 0.45f) : Theme.GoldBright;
            Toast.Show(audio.Muted ? "音效已静音" : "音效已开启");
        }

        private List<string> NamedGroups() =>
            _model.GroupKeys().Where(k => k.Length > 0).ToList();

        // ―― 页面构建与翻页 ――

        /// <summary>构建某分组的磁贴页(应用当前搜索过滤,磁贴交错浮现)。</summary>
        private RectTransform BuildPage(string groupKey)
        {
            var page = UIFactory.Rect("Page_" + groupKey, _pageHolder);
            UIFactory.Fill(page);

            var content = UIFactory.ScrollView("Scroll", page, out _);
            UIFactory.Fill((RectTransform)content.parent);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(TileView.Size, TileView.Size + 26f);
            grid.spacing = new Vector2(18f, 18f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var items = _model.ItemsIn(groupKey)
                .Where(i => SpellbookModel.Matches(i, _search)).ToList();

            if (_model.AllItems.Count == 0)
            {
                var empty = UIFactory.Text("Empty", page,
                    "法术书空空如也\n点击右上「＋ 缮写」记下第一条法术", Theme.FontBody, Theme.InkFaded);
                empty.textWrappingMode = TextWrappingModes.Normal;
                UIFactory.Fill(empty.rectTransform);
            }

            for (var i = 0; i < items.Count; i++)
            {
                var tile = TileView.Create(content, items[i]);
                tile.OnRun = RunTile;
                tile.OnContextMenu = ShowTileMenu;
                tile.OnReorder = (source, target) =>
                {
                    _model.ReorderBefore(source.Item, target.Item);
                    RebuildCurrentPage();
                };

                // 交错浮现:缩放 + 淡入
                var cg = tile.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.TweenAlpha(1f, 0.25f, Ease.OutQuad, 0.03f * i);
                tile.transform.localScale = Vector3.one * 0.7f;
                tile.transform.TweenScale(Vector3.one, 0.3f, Ease.OutBack, 0.03f * i);
            }
            return page;
        }

        /// <summary>翻页切换分组:旧页卷出,新页滑入,方向随分组顺序。</summary>
        private void SwitchGroup(string groupKey)
        {
            var keys = _model.GroupKeys();
            var forward = keys.IndexOf(groupKey) > keys.IndexOf(_selectedGroup);
            _selectedGroup = groupKey;
            _tabs.SetSelected(groupKey);
            UpdateGroupTitle();
            AudioManager.Instance.PlayVariant("page", 3, 0.8f);

            var old = _currentPage;
            var width = _pageHolder.rect.width;

            if (old != null)
            {
                var oldCg = old.gameObject.GetComponent<CanvasGroup>()
                            ?? old.gameObject.AddComponent<CanvasGroup>();
                oldCg.blocksRaycasts = false;
                old.TweenAnchoredPos(new Vector2(forward ? -width * 0.55f : width * 0.55f, 0f),
                    0.32f, Ease.InQuad);
                old.TweenRotationZ(forward ? 5f : -5f, 0.32f, Ease.InQuad);
                oldCg.TweenAlpha(0f, 0.3f, Ease.InQuad)
                     .Then(() => { if (old != null) Destroy(old.gameObject); });
            }

            _currentPage = BuildPage(groupKey);
            _currentPage.anchoredPosition = new Vector2(forward ? width * 0.55f : -width * 0.55f, 0f);
            _currentPage.localEulerAngles = new Vector3(0f, 0f, forward ? -4f : 4f);
            var cg2 = _currentPage.gameObject.AddComponent<CanvasGroup>();
            cg2.alpha = 0f;
            _currentPage.TweenAnchoredPos(Vector2.zero, 0.38f, Ease.OutCubic, 0.08f);
            _currentPage.TweenRotationZ(0f, 0.38f, Ease.OutCubic, 0.08f);
            cg2.TweenAlpha(1f, 0.3f, Ease.OutQuad, 0.08f);
        }

        /// <summary>就地重建当前页(增删改/排序后),不播翻页动画。</summary>
        private void RebuildCurrentPage()
        {
            // 分组可能因编辑/删除而消失,回退到第一个分组
            var keys = _model.GroupKeys();
            if (!keys.Contains(_selectedGroup))
            {
                _selectedGroup = keys.Count > 0 ? keys[0] : "";
            }
            _tabs.Rebuild(keys, _selectedGroup);
            UpdateGroupTitle();

            if (_currentPage != null) Destroy(_currentPage.gameObject);
            _currentPage = BuildPage(_selectedGroup);
        }

        private void UpdateGroupTitle()
        {
            _groupTitle.text = _selectedGroup.Length == 0 ? "未分组" : _selectedGroup;
        }

        private void ApplySearch(string text)
        {
            _search = text;
            RebuildCurrentPage();
        }

        // ―― 条目操作 ――

        private void OnCreateConfirmed(SpellItem item)
        {
            _model.Add(item);
            _selectedGroup = item.GroupName;
            RebuildCurrentPage();
            Toast.Show($"「{item.Name}」已缮写入法术书");
        }

        private void ShowTileMenu(TileView tile, Vector2 screenPos)
        {
            var entries = new List<(string, Action)>
            {
                ("运行", () => RunTile(tile)),
                ("编辑", () =>
                {
                    var prevGroup = tile.Item.GroupName;
                    EditDialog.Show(_canvasRoot, NamedGroups(), tile.Item, edited =>
                    {
                        _model.ApplyEdit(edited, prevGroup);
                        _selectedGroup = edited.GroupName;
                        RebuildCurrentPage();
                    });
                }),
            };

            foreach (var g in _model.GroupKeys().Where(k => k != tile.Item.GroupName))
            {
                var display = g.Length == 0 ? "未分组" : g;
                var captured = g;
                entries.Add(($"移至「{display}」", () =>
                {
                    _model.MoveToGroup(tile.Item, captured);
                    RebuildCurrentPage();
                }));
            }

            entries.Add(("删除", () => ConfirmDialog.Show(_canvasRoot, "焚毁此页?",
                $"「{tile.Item.Name}」将从法术书中永久移除。", () =>
                {
                    _model.Delete(tile.Item);
                    RebuildCurrentPage();
                    Toast.Show($"「{tile.Item.Name}」已焚毁");
                })));

            ContextMenu.Show(_canvasRoot, screenPos, entries);
        }

        /// <summary>点击磁贴:失效则抖动报错;否则按类型分发,并施放粒子与音效。</summary>
        private void RunTile(TileView tile)
        {
            tile.RefreshMissing();
            var item = tile.Item;
            if (Launcher.TargetMissing(item.ScriptPath))
            {
                tile.ShakeError();
                Toast.Show($"目标不存在: {item.ScriptPath}", isError: true);
                return;
            }

            var kind = Launcher.GetLaunchKind(item.ScriptPath);
            try
            {
                Fx.CastBurst(tile.transform.position,
                    kind == LaunchKind.Script ? Theme.Ember : Theme.Arcane);
                AudioManager.Instance.PlayVariant("cast", 5, 0.8f);

                switch (kind)
                {
                    case LaunchKind.Script:
                        var process = Launcher.StartScript(item.ScriptPath, item.Arguments);
                        _running.Add((process, item.Name));
                        Toast.Show($"「{item.Name}」施放中…");
                        break;
                    case LaunchKind.Folder:
                        Launcher.Launch(item.ScriptPath, "");
                        Toast.Show($"「{item.Name}」已打开文件夹");
                        break;
                    case LaunchKind.Url:
                        Launcher.Launch(item.ScriptPath, item.Arguments);
                        Toast.Show($"「{item.Name}」已在浏览器打开");
                        break;
                    default:
                        Launcher.Launch(item.ScriptPath, item.Arguments);
                        Toast.Show($"「{item.Name}」已启动");
                        break;
                }
                tile.FlashSuccess();
            }
            catch (Exception ex)
            {
                tile.ShakeError();
                Toast.Show($"「{item.Name}」启动失败: {ex.Message}", isError: true);
            }
        }

        /// <summary>轮询运行中的脚本进程,退出后回填退出码。</summary>
        private void Update()
        {
            for (var i = _running.Count - 1; i >= 0; i--)
            {
                var (process, name) = _running[i];
                bool exited;
                int code = 0;
                try
                {
                    exited = process.HasExited;
                    if (exited) code = process.ExitCode;
                }
                catch (Exception)
                {
                    exited = true;   // 进程句柄失效视为已结束,不再轮询
                }

                if (!exited) continue;
                _running.RemoveAt(i);
                process.Dispose();
                if (code == 0)
                {
                    AudioManager.Instance.Play("confirm", 0.6f);
                    Toast.Show($"「{name}」施放完成,退出码 0");
                }
                else
                {
                    Toast.Show($"「{name}」退出码 {code}", isError: true);
                }
            }
        }
    }
}
