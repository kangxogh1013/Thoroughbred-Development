using UnityEngine;

namespace HorseRanch
{
    /// <summary>UI 색상 팔레트 (경마장 잔디 + 전광판 컨셉)</summary>
    public static class UITheme
    {
        public static readonly Color Background = Hex(0xE7EBE2);
        public static readonly Color Panel = Hex(0xF6F8F2);
        public static readonly Color Panel2 = Hex(0xE3E9DC);
        public static readonly Color Ink = Hex(0x16201A);
        public static readonly Color Mute = Hex(0x566458);
        public static readonly Color Line = Hex(0xC6CFBF);
        public static readonly Color Turf = Hex(0x2A6A3D);
        public static readonly Color TurfLight = Hex(0x3D8443);
        public static readonly Color TurfStripe = Hex(0x45904B);
        public static readonly Color Accent = Hex(0xB8860B);
        public static readonly Color Board = Hex(0x151B14);
        public static readonly Color BoardRow = Hex(0x1E271C);
        public static readonly Color BoardMine = Hex(0x2C3D25);
        public static readonly Color BoardFg = Hex(0xF2BF37);
        public static readonly Color BoardText = Hex(0xE6E3D3);
        public static readonly Color BoardMute = Hex(0x8B927F);
        public static readonly Color Good = Hex(0x2A6A3D);
        public static readonly Color Warn = Hex(0xB06D12);
        public static readonly Color Bad = Hex(0xB3342A);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color White = Color.white;

        // 마번 색 (1백 2흑 3적 4청 5황 6녹 7등 8도 — 실제 경마 마번 색 규칙)
        public static readonly Color[] Silk =
        {
            Hex(0xF3F2EC), Hex(0x1C1C1C), Hex(0xD3302A), Hex(0x2A5EC2),
            Hex(0xF1CD1C), Hex(0x2E984A), Hex(0xEF871B), Hex(0xF19ABF)
        };
        public static readonly Color[] SilkInk =
        {
            Hex(0x111111), Color.white, Color.white, Color.white,
            Hex(0x111111), Color.white, Hex(0x111111), Hex(0x111111)
        };
        public static readonly string[] SilkHex = { "F3F2EC", "1C1C1C", "D3302A", "2A5EC2", "F1CD1C", "2E984A", "EF871B", "F19ABF" };

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        public static string Tone(NoticeTone t)
        {
            switch (t)
            {
                case NoticeTone.Good: return "#2A6A3D";
                case NoticeTone.Bad: return "#B3342A";
                default: return "#16201A";
            }
        }
    }
}
