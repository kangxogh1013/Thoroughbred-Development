using System;
using System.IO;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 씬에 1개. RanchGame(순수 로직)을 생성하고 저장/불러오기를 담당한다.
    /// 싱글톤 대신 각 UI가 인스펙터 참조로 이 컴포넌트를 받는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RanchGameRunner : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private GameDatabaseSO database;

        [Header("저장")]
        [Tooltip("persistentDataPath 아래 저장 파일 이름")]
        [SerializeField] private string saveFileName = "horse_ranch_save.json";
        [SerializeField] private bool autoSave = true;

        private RanchGame game;

        public GameDatabaseSO Database { get { return database; } }
        public string SavePath { get { return Path.Combine(Application.persistentDataPath, saveFileName); } }

        /// <summary>UI의 OnEnable이 Awake보다 먼저 불려도 안전하도록 지연 생성</summary>
        public RanchGame Game
        {
            get
            {
                if (game == null) Init();
                return game;
            }
        }

        private void Awake()
        {
            if (game == null) Init();
        }

        private void Init()
        {
            if (database == null)
            {
                Debug.LogError("[HorseRanch] GameDatabase가 할당되지 않았습니다. Tools > HorseRanch > 전체 자동 생성을 다시 실행하세요.", this);
                return;
            }
            game = new RanchGame(database.BuildData(), Environment.TickCount);
            game.Changed += HandleChanged;

            GameState loaded = TryLoad();
            if (loaded != null) game.LoadState(loaded);
            else game.NewGame();
            Debug.Log("[HorseRanch] 게임 시작 · " + game.Summary() + " · 저장 위치: " + SavePath);
        }

        private void HandleChanged()
        {
            if (autoSave) Save();
        }

        public void Save()
        {
            if (game == null || game.State == null) return;
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(game.State)); }
            catch (Exception e) { Debug.LogWarning("[HorseRanch] 저장 실패: " + e.Message); }
        }

        private GameState TryLoad()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var s = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));
                return s != null && s.version == 2 ? s : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[HorseRanch] 세이브를 읽지 못해 새 게임을 시작합니다: " + e.Message);
                return null;
            }
        }

        public void RestartGame()
        {
            Game.NewGame();
        }

        // ===== 에디터 테스트용 (컴포넌트 우클릭 메뉴) =====
        [ContextMenu("세이브 삭제")]
        private void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            Debug.Log("[HorseRanch] 세이브 삭제: " + SavePath);
        }

        [ContextMenu("치트: 골드 +100,000")]
        private void CheatGold() { if (Application.isPlaying) Game.DebugAddGold(100000); }

        [ContextMenu("치트: 7일 건너뛰기")]
        private void CheatSkip() { if (Application.isPlaying) Game.DebugSkipDays(7); }

        [ContextMenu("새 게임")]
        private void CheatNewGame() { if (Application.isPlaying) RestartGame(); }
    }
}
