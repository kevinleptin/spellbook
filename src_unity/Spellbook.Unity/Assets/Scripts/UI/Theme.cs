using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Spellbook.UI
{
    /// <summary>
    /// 视觉令牌:配色/字号/字体,全局唯一来源。
    /// 字体在启动时从 Resources/Fonts 的 TTF 动态创建 TMP 字体
    /// (Cinzel 做标题,Noto Serif SC 兜底中文,运行时动态图集免预烘焙)。
    /// </summary>
    public static class Theme
    {
        // ―― 配色:暗色桌面 + 羊皮纸书页 + 鎏金 + 奥术蓝 ――
        public static readonly Color Backdrop = Hex("#141017");
        public static readonly Color Parchment = Hex("#E8D5A9");
        public static readonly Color ParchmentDark = Hex("#D9C08C");
        public static readonly Color Ink = Hex("#3B2E1E");
        public static readonly Color InkFaded = Hex("#3B2E1E99");
        public static readonly Color Gold = Hex("#C8A951");
        public static readonly Color GoldBright = Hex("#F2D57A");
        public static readonly Color Ember = Hex("#FF7A2F");
        public static readonly Color Arcane = Hex("#7FD5FF");
        public static readonly Color Danger = Hex("#C0392B");
        public static readonly Color PanelDark = Hex("#241B14F2");

        public const int FontTitle = 34;
        public const int FontGroupTitle = 26;
        public const int FontBody = 20;
        public const int FontSmall = 16;
        public const int FontTile = 15;

        public static TMP_FontAsset TitleFont { get; private set; }
        public static TMP_FontAsset BodyFont { get; private set; }

        public static void Init()
        {
            var noto = Resources.Load<Font>("Fonts/NotoSerifSC");
            var cinzel = Resources.Load<Font>("Fonts/Cinzel");

            BodyFont = TMP_FontAsset.CreateFontAsset(noto);
            TitleFont = cinzel != null ? TMP_FontAsset.CreateFontAsset(cinzel) : BodyFont;
            // Cinzel 无中文字形,中文回退到 Noto(动态字体的回退表初始可能为 null)
            if (TitleFont != BodyFont)
            {
                TitleFont.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                TitleFont.fallbackFontAssetTable.Add(BodyFont);
            }
        }

        public static Sprite Sprite(string name) =>
            Resources.Load<Sprite>("Art/" + name);

        public static Sprite Icon(string key) =>
            Resources.Load<Sprite>("Icons/" + (string.IsNullOrEmpty(key) ? "book" : key))
            ?? Resources.Load<Sprite>("Icons/book");

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
