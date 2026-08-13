using System;
using System.Collections.Generic;
using System.IO;
using Spellbook.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellbook.UI
{
    /// <summary>
    /// 新建/编辑条目对话框:目标路径(选文件/选文件夹)、名称、参数、备注、
    /// 分组(可选已有或输入新建)、图标选择。逻辑对齐 WPF 版 EditItemViewModel:
    /// 名称未被手改时跟随所选文件名。
    /// </summary>
    public static class EditDialog
    {
        public static void Show(
            Transform canvasRoot, IReadOnlyList<string> existingGroups,
            SpellItem editing, Action<SpellItem> onConfirm)
        {
            var isNew = editing == null;
            var panel = Modal.Open(canvasRoot, new Vector2(640f, 620f), out var close);

            var title = UIFactory.Text("Title", panel, isNew ? "缮写新法术" : "修订法术",
                Theme.FontGroupTitle, Theme.Ink, Theme.TitleFont);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(20f, -62f);
            titleRt.offsetMax = new Vector2(-20f, -16f);

            var iconKey = isNew || string.IsNullOrWhiteSpace(editing.IconKey)
                ? "book" : editing.IconKey;
            var lastAutoName = "";

            // ―― 左上:大图标按钮(点开图标选择器) ――
            var iconBtnImg = UIFactory.Panel("IconBtn", panel, null, new Color(0.08f, 0.05f, 0.03f, 0.9f));
            var iconBtnRt = iconBtnImg.rectTransform;
            iconBtnRt.anchorMin = new Vector2(0f, 1f);
            iconBtnRt.anchorMax = new Vector2(0f, 1f);
            iconBtnRt.pivot = new Vector2(0f, 1f);
            iconBtnRt.sizeDelta = new Vector2(96f, 96f);
            iconBtnRt.anchoredPosition = new Vector2(36f, -84f);
            iconBtnImg.raycastTarget = true;

            var iconImg = UIFactory.Panel("Icon", iconBtnImg.transform, Theme.Icon(iconKey), Color.white);
            UIFactory.Fill(iconImg.rectTransform, 5f);
            iconImg.raycastTarget = false;
            var iconFrame = UIFactory.Panel("Frame", iconBtnImg.transform,
                Theme.Sprite("tile_frame"), Theme.Gold);
            UIFactory.Fill(iconFrame.rectTransform);
            iconFrame.raycastTarget = false;

            var iconHover = iconBtnImg.gameObject.AddComponent<HoverButton>();
            iconHover.OnClick = () => IconPicker.Show(canvasRoot, iconKey, picked =>
            {
                iconKey = picked;
                iconImg.sprite = Theme.Icon(picked);
            });

            var iconHint = UIFactory.Text("IconHint", panel, "点击换图标", Theme.FontSmall, Theme.InkFaded);
            var hintRt = iconHint.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 1f);
            hintRt.anchorMax = new Vector2(0f, 1f);
            hintRt.pivot = new Vector2(0f, 1f);
            hintRt.sizeDelta = new Vector2(96f, 22f);
            hintRt.anchoredPosition = new Vector2(36f, -184f);

            // ―― 右侧字段列 ――
            var fieldLeft = 156f;
            var fieldRight = -36f;
            var y = -84f;

            TMP_InputField MakeField(string label, string value, float height, bool multi = false)
            {
                var lab = UIFactory.Text("L_" + label, panel, label, Theme.FontSmall, Theme.InkFaded,
                    null, TextAlignmentOptions.Left);
                var labRt = lab.rectTransform;
                labRt.anchorMin = new Vector2(0f, 1f);
                labRt.anchorMax = new Vector2(1f, 1f);
                labRt.pivot = new Vector2(0.5f, 1f);
                labRt.offsetMin = new Vector2(fieldLeft, y - 22f);
                labRt.offsetMax = new Vector2(fieldRight, y);

                var input = UIFactory.Input("F_" + label, panel, "", multi);
                var inRt = ((Image)input.targetGraphic).rectTransform;
                inRt.anchorMin = new Vector2(0f, 1f);
                inRt.anchorMax = new Vector2(1f, 1f);
                inRt.pivot = new Vector2(0.5f, 1f);
                inRt.offsetMin = new Vector2(fieldLeft, y - 24f - height);
                inRt.offsetMax = new Vector2(fieldRight, y - 24f);
                input.text = value;
                y -= 30f + height;
                return input;
            }

            var pathInput = MakeField("目标路径(脚本 / 程序 / 文件夹 / 网址)",
                isNew ? "" : editing.ScriptPath, 40f);

            // 为"选文件/选文件夹"按钮行预留位置(按钮在 nameInput 创建后再挂,避免闭包引用未赋值变量)
            var browseY = y - 2f;
            y -= 36f;

            var nameInput = MakeField("名称", isNew ? "" : editing.Name, 40f);

            // 选中文件后:名称为空或仍等于上次自动名时自动跟随文件名(与 WPF 版一致)
            void ApplyPickedPath(string path)
            {
                pathInput.text = path;
                if (string.IsNullOrWhiteSpace(nameInput.text) || nameInput.text == lastAutoName)
                {
                    lastAutoName = Path.GetFileNameWithoutExtension(path);
                    nameInput.text = lastAutoName;
                }
            }

            void MakeBrowse(string label, float xOffset, Func<string> pick)
            {
                var btn = UIFactory.Button("B_" + label, panel, Theme.Sprite("panel_dark"),
                    new Color(0.35f, 0.28f, 0.2f), () =>
                    {
                        var result = pick();
                        if (!string.IsNullOrEmpty(result)) ApplyPickedPath(result);
                    });
                var rt = btn.GetComponent<Image>().rectTransform;
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(96f, 30f);
                rt.anchoredPosition = new Vector2(fieldRight - xOffset, browseY);
                var text = UIFactory.Text("T", rt, label, Theme.FontSmall, Theme.Parchment);
                UIFactory.Fill(text.rectTransform);
                text.raycastTarget = false;
            }

            MakeBrowse("选文件", 104f,
                () => NativeFileDialog.OpenFile("选择目标文件",
                    "所有文件|*.*|PowerShell 脚本|*.ps1|程序|*.exe"));
            MakeBrowse("选文件夹", 0f,
                () => NativeFileDialog.OpenFolder("选择文件夹"));

            var argsInput = MakeField("参数(原样拼接,可空)", isNew ? "" : editing.Arguments, 40f);
            var groupInput = MakeField("分组(可输入新分组名,空 = 未分组)",
                isNew ? "" : editing.GroupName, 40f);

            // 已有分组快捷片:点一下填入
            if (existingGroups.Count > 0)
            {
                var chipX = fieldLeft;
                foreach (var g in existingGroups)
                {
                    var chip = UIFactory.Button("Chip_" + g, panel, Theme.Sprite("panel_dark"),
                        new Color(0.3f, 0.24f, 0.16f), () => groupInput.text = g);
                    var rt = chip.GetComponent<Image>().rectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    var width = Mathf.Clamp(g.Length * 20f + 24f, 60f, 160f);
                    rt.sizeDelta = new Vector2(width, 28f);
                    rt.anchoredPosition = new Vector2(chipX, y - 2f);
                    var text = UIFactory.Text("T", rt, g, Theme.FontSmall, Theme.GoldBright);
                    UIFactory.Fill(text.rectTransform, 4f);
                    text.raycastTarget = false;
                    chipX += width + 8f;
                    if (chipX > 560f) break;   // 一行装不下就截断,分组名仍可手输
                }
                y -= 36f;
            }

            var notesInput = MakeField("备注(可空)", isNew ? "" : editing.Notes, 64f, multi: true);

            // ―― 底部按钮 ――
            ConfirmDialog.MakeActionButton(panel, isNew ? "缮写" : "保存", new Vector2(-90f, 44f),
                new Color(0.55f, 0.42f, 0.15f), Color.white, () =>
                {
                    var name = nameInput.text.Trim();
                    var path = pathInput.text.Trim();
                    if (name.Length == 0 || path.Length == 0)
                    {
                        Toast.Show("名称与目标路径不能为空", isError: true);
                        return;
                    }

                    var target = editing ?? new SpellItem();
                    target.Name = name;
                    target.ScriptPath = path;
                    target.Arguments = argsInput.text.Trim();
                    target.Notes = notesInput.text;
                    target.GroupName = groupInput.text.Trim();
                    target.IconKey = iconKey;
                    close();
                    onConfirm(target);
                });
            ConfirmDialog.MakeActionButton(panel, "取消", new Vector2(90f, 44f),
                new Color(0.35f, 0.28f, 0.2f), Theme.Parchment, close);
        }
    }
}
