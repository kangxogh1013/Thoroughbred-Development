using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HorseRanch.EditorTools
{
    /// <summary>
    /// 상단 메뉴 Tools > HorseRanch.
    /// [전체 자동 생성] 한 번이면 데이터 에셋 → 스프라이트 → 한글 폰트 → 씬/UI 순서로 모두 만든다.
    /// </summary>
    public static class HorseRanchMenu
    {
        private const string Menu = "Tools/HorseRanch/";

        [MenuItem(Menu + "전체 자동 생성 (Build All)", false, 0)]
        public static void BuildAll()
        {
            HorseRanchPaths.EnsureFolders();
            if (!HorseRanchSceneBuilder.EnsureTmpEssentials()) return; // TMP 리소스 가져온 뒤 다시 실행

            var db = HorseRanchDataBuilder.Build(false);
            var art = HorseRanchArtBuilder.BuildSprites();
            var font = HorseRanchArtBuilder.EnsureKoreanFont(true);
            HorseRanchSceneBuilder.Build(db, art, font);

            EditorUtility.DisplayDialog("질주마 목장",
                "자동 생성이 끝났습니다.\n\n" +
                "· 씬: " + HorseRanchPaths.ScenePath + "\n" +
                "· 데이터: " + HorseRanchPaths.Data + "\n" +
                (font == null ? "\n※ 한글 폰트가 없어 글자가 □로 보일 수 있습니다.\n  Tools > HorseRanch > 3. 한글 폰트 설정 후 다시 생성하세요.\n" : "") +
                "\n상단 ▶ Play 버튼을 누르면 바로 플레이할 수 있습니다.", "확인");
        }

        [MenuItem(Menu + "1. 데이터 에셋 생성 (ScriptableObject)", false, 20)]
        public static void BuildData()
        {
            HorseRanchPaths.EnsureFolders();
            var db = HorseRanchDataBuilder.Build(false);
            Selection.activeObject = db;
            EditorGUIUtility.PingObject(db);
        }

        [MenuItem(Menu + "2. 스프라이트 생성", false, 21)]
        public static void BuildSprites()
        {
            HorseRanchPaths.EnsureFolders();
            HorseRanchArtBuilder.BuildSprites();
        }

        [MenuItem(Menu + "3. 한글 폰트 설정", false, 22)]
        public static void BuildFont()
        {
            HorseRanchPaths.EnsureFolders();
            if (!HorseRanchSceneBuilder.EnsureTmpEssentials()) return;
            var f = HorseRanchArtBuilder.EnsureKoreanFont(true);
            if (f != null) { Selection.activeObject = f; EditorGUIUtility.PingObject(f); }
        }

        [MenuItem(Menu + "4. 씬 다시 생성 (UI 포함)", false, 23)]
        public static void BuildScene()
        {
            HorseRanchPaths.EnsureFolders();
            if (!HorseRanchSceneBuilder.EnsureTmpEssentials()) return;
            var db = HorseRanchDataBuilder.Build(false);
            var art = HorseRanchArtBuilder.BuildSprites();
            var font = HorseRanchArtBuilder.EnsureKoreanFont(false);
            HorseRanchSceneBuilder.Build(db, art, font);
        }

        [MenuItem(Menu + "씬 열고 플레이", false, 40)]
        public static void OpenAndPlay()
        {
            if (!File.Exists(HorseRanchPaths.ScenePath))
            {
                EditorUtility.DisplayDialog("질주마 목장", "씬이 없습니다. 먼저 [전체 자동 생성]을 실행하세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(HorseRanchPaths.ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem(Menu + "데이터 값 초기화 (기본 밸런스로 덮어쓰기)", false, 60)]
        public static void ResetData()
        {
            if (!EditorUtility.DisplayDialog("데이터 초기화", "인스펙터에서 조정한 밸런스·이름·보상 값이 기본값으로 돌아갑니다.", "초기화", "취소")) return;
            HorseRanchPaths.EnsureFolders();
            Selection.activeObject = HorseRanchDataBuilder.Build(true);
        }

        [MenuItem(Menu + "세이브 데이터 삭제", false, 61)]
        public static void DeleteSave()
        {
            string path = Path.Combine(Application.persistentDataPath, "horse_ranch_save.json");
            if (File.Exists(path)) File.Delete(path);
            Debug.Log("[HorseRanch] 세이브 삭제: " + path);
        }
    }

    public static class HorseRanchPaths
    {
        public const string Root = "Assets/HorseRanch";
        public const string Data = Root + "/Data";
        public const string Art = Root + "/Art";
        public const string Fonts = Root + "/Fonts";
        public const string Scenes = Root + "/Scenes";
        public const string ScenePath = Scenes + "/HorseRanch.unity";
        public const string FontAssetPath = Fonts + "/HorseRanchKR SDF.asset";

        public static void EnsureFolders()
        {
            Ensure("Assets", "HorseRanch");
            Ensure(Root, "Data");
            Ensure(Data, "Races");
            Ensure(Data, "Items");
            Ensure(Data, "Achievements");
            Ensure(Root, "Art");
            Ensure(Root, "Fonts");
            Ensure(Root, "Scenes");
        }

        private static void Ensure(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
