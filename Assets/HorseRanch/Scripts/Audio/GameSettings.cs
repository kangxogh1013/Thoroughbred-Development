using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 사용자 설정 (PlayerPrefs 저장): 배경음·효과음 볼륨, 창 크기, 전체화면/창모드.
    /// 게임 진행 세이브(JSON)와 분리되어 새 게임을 해도 유지된다.
    /// </summary>
    public static class GameSettings
    {
        const string KBgm = "hr.bgm", KSfx = "hr.sfx", KRes = "hr.res", KFull = "hr.full";

        /// <summary>창 크기 선택지 (16:9)</summary>
        public static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440),
        };

        public static float BgmVolume
        {
            get { return PlayerPrefs.GetFloat(KBgm, 0.6f); }
            set { PlayerPrefs.SetFloat(KBgm, Mathf.Clamp01(value)); }
        }

        public static float SfxVolume
        {
            get { return PlayerPrefs.GetFloat(KSfx, 0.8f); }
            set { PlayerPrefs.SetFloat(KSfx, Mathf.Clamp01(value)); }
        }

        public static int ResolutionIndex
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(KRes, 2), 0, Resolutions.Length - 1); }
            set { PlayerPrefs.SetInt(KRes, Mathf.Clamp(value, 0, Resolutions.Length - 1)); }
        }

        public static bool Fullscreen
        {
            get { return PlayerPrefs.GetInt(KFull, 0) == 1; }
            set { PlayerPrefs.SetInt(KFull, value ? 1 : 0); }
        }

        public static string ResolutionLabel(int i) { return Resolutions[i].x + " × " + Resolutions[i].y; }

        /// <summary>창 크기·화면 모드 적용 + 저장. 에디터 Game 뷰에서는 해상도가 바뀌지 않는다 (빌드에서 확인)</summary>
        public static void ApplyScreen()
        {
            var r = Resolutions[ResolutionIndex];
            Screen.SetResolution(r.x, r.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            PlayerPrefs.Save();
        }

        public static void Save() { PlayerPrefs.Save(); }
    }
}
