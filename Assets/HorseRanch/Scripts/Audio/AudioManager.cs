using UnityEngine;

namespace HorseRanch
{
    public enum BgmId { None = -1, Title = 0, Town = 1, Race = 2, Hall = 3, Stadium = 4, GameOver = 5, Ending = 6 }
    public enum SfxId { Click = 0, Coin = 1, Door = 2, Search = 3, Fanfare = 4, Gate = 5, Trash = 6, Cheer = 7, Neigh = 8, Snort = 9, Shout = 10, Boo = 11, Warning = 12 }
    /// <summary>배경음 위에 겹치는 환경음 (반복)</summary>
    public enum AmbienceId { None = -1, HallCrowd = 0, StadiumCrowd = 1 }

    /// <summary>
    /// 배경음(1채널, 반복) + 환경음(1채널, 반복) + 효과음(PlayOneShot).
    /// 싱글톤 없이 GameUI · TitleView가 인스펙터 참조로 사용한다.
    /// 볼륨은 GameSettings(PlayerPrefs)에서 읽고, 설정 창에서 바꾸면 ApplyVolume으로 즉시 반영. 환경음은 효과음 볼륨을 따른다.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("오디오 소스")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;

        [Header("배경음 (BgmId 순서: 타이틀, 마을, 경주, 도박장, 경기장, 게임 오버, 엔딩)")]
        [SerializeField] private AudioClip[] bgmClips;
        [Header("효과음 (SfxId 순서: 클릭, 동전, 문, 뒤지기, 팡파레, 출발 벨, 쓰레기, 함성, 말 울음, 콧김, 고함, 야유, 경고)")]
        [SerializeField] private AudioClip[] sfxClips;
        [Header("환경음 (AmbienceId 순서: 도박장 웅성임, 경기장 관중)")]
        [SerializeField] private AudioClip[] ambienceClips;

        [Tooltip("배경음 전환 시 페이드 시간 (초)")]
        [SerializeField] [Range(0f, 2f)] private float fadeTime = 0.4f;

        private BgmId current = BgmId.None;
        private BgmId pending = BgmId.None;
        private float fade = 1f;        // 0~1, 현재 곡 페이드 배율
        private bool fadingOut;
        private AmbienceId ambience = AmbienceId.None;
        private float ambienceLevel = 1f;   // 환경음 크기 배율 (경주 중엔 크게)
        private float ambienceNow;

        public BgmId Current { get { return current; } }
        public AmbienceId CurrentAmbience { get { return ambience; } }

        private void Awake()
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ApplyVolume();
        }

        /// <summary>설정값(PlayerPrefs)을 볼륨에 반영</summary>
        public void ApplyVolume()
        {
            bgmSource.volume = GameSettings.BgmVolume * fade;
            sfxSource.volume = GameSettings.SfxVolume;
            ambienceSource.volume = GameSettings.SfxVolume * ambienceNow;
        }

        public void PlayBgm(BgmId id)
        {
            if (id == current && !fadingOut) return;
            pending = id;
            if (current == BgmId.None || fadeTime <= 0f) { SwitchNow(); return; }
            fadingOut = true;
        }

        /// <summary>환경음 바꾸기. level = 크기 배율 (0~1.5)</summary>
        public void PlayAmbience(AmbienceId id, float level)
        {
            ambienceLevel = level;
            if (id == ambience) return;
            ambience = id;
            int i = (int)id;
            if (id == AmbienceId.None || ambienceClips == null || i >= ambienceClips.Length || ambienceClips[i] == null) { ambienceSource.Stop(); return; }
            ambienceSource.clip = ambienceClips[i];
            ambienceSource.Play();
            ambienceNow = 0f;
        }

        public void PlaySfx(SfxId id) { PlaySfx(id, 1f); }

        public void PlaySfx(SfxId id, float volumeScale)
        {
            int i = (int)id;
            if (sfxClips == null || i >= sfxClips.Length || sfxClips[i] == null) return;
            sfxSource.PlayOneShot(sfxClips[i], volumeScale);
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

            // 환경음은 부드럽게 목표 크기로
            float target = ambience == AmbienceId.None ? 0f : ambienceLevel;
            ambienceNow = Mathf.MoveTowards(ambienceNow, target, Time.unscaledDeltaTime * 1.5f);
            ambienceSource.volume = GameSettings.SfxVolume * ambienceNow;
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
