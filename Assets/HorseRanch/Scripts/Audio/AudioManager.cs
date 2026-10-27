using UnityEngine;

namespace HorseRanch
{
    public enum BgmId { None = -1, Title = 0, Town = 1, Race = 2 }
    public enum SfxId { Click = 0, Coin = 1, Door = 2, Search = 3, Fanfare = 4, Gate = 5, Trash = 6 }

    /// <summary>
    /// 배경음(1채널, 반복) + 효과음(PlayOneShot). 싱글톤 없이 GameUI·TitleView가 인스펙터 참조로 사용한다.
    /// 볼륨은 GameSettings(PlayerPrefs)에서 읽고, 설정 창에서 바꾸면 Apply로 즉시 반영.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("오디오 소스")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("배경음 (BgmId 순서: 타이틀, 마을, 경주)")]
        [SerializeField] private AudioClip[] bgmClips;
        [Header("효과음 (SfxId 순서: 클릭, 동전, 문, 뒤지기, 팡파레, 출발 벨, 쓰레기)")]
        [SerializeField] private AudioClip[] sfxClips;

        [Tooltip("배경음 전환 시 페이드 시간 (초)")]
        [SerializeField] [Range(0f, 2f)] private float fadeTime = 0.4f;

        private BgmId current = BgmId.None;
        private BgmId pending = BgmId.None;
        private float fade = 1f;        // 0~1, 현재 곡 페이드 배율
        private bool fadingOut;

        public BgmId Current { get { return current; } }

        private void Awake()
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            ApplyVolume();
        }

        /// <summary>설정값(PlayerPrefs)을 볼륨에 반영</summary>
        public void ApplyVolume()
        {
            bgmSource.volume = GameSettings.BgmVolume * fade;
            sfxSource.volume = GameSettings.SfxVolume;
        }

        public void PlayBgm(BgmId id)
        {
            if (id == current && !fadingOut) return;
            pending = id;
            if (current == BgmId.None || fadeTime <= 0f) { SwitchNow(); return; }
            fadingOut = true;
        }

        public void PlaySfx(SfxId id)
        {
            int i = (int)id;
            if (sfxClips == null || i >= sfxClips.Length || sfxClips[i] == null) return;
            sfxSource.PlayOneShot(sfxClips[i]);
        }

        private void Update()
        {
            if (fadingOut)
            {
                fade -= Time.unscaledDeltaTime / fadeTime;
                if (fade <= 0f) { fade = 0f; fadingOut = false; SwitchNow(); }
            }
            else if (fade < 1f) fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            bgmSource.volume = GameSettings.BgmVolume * fade;
        }

        private void SwitchNow()
        {
            current = pending;
            int i = (int)current;
            if (current == BgmId.None || bgmClips == null || i >= bgmClips.Length || bgmClips[i] == null) { bgmSource.Stop(); return; }
            bgmSource.clip = bgmClips[i];
            bgmSource.Play();
            fade = fadeTime > 0f ? 0f : 1f;
        }
    }
}
