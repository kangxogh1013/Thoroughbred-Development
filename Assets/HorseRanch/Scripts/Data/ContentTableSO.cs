using UnityEngine;

namespace HorseRanch
{
    /// <summary>이름 생성 테이블, 털색, 훈련 돌발 이벤트 문구</summary>
    [CreateAssetMenu(fileName = "ContentTable", menuName = "HorseRanch/Content Table")]
    public class ContentTableSO : ScriptableObject
    {
        [Header("말 이름 = 앞 + 뒤")]
        [SerializeField] private string[] namePrefixes = { "번개", "천둥", "바람", "은빛", "흑진주", "태양", "달빛", "폭풍", "청룡", "황금", "초원", "질풍", "강철", "새벽", "별빛", "한라", "무지개", "불꽃", "파도", "설원" };
        [SerializeField] private string[] nameSuffixes = { "질주", "왕", "호", "스타", "킹", "의꿈", "대장", "한별", "아리", "돌풍", "마루", "보라", "나래", "가람", "빛" };

        [Header("털색")]
        [SerializeField] private CoatDefinition[] coats =
        {
            new CoatDefinition("밤색", new Color(0.478f, 0.290f, 0.157f)),
            new CoatDefinition("흑색", new Color(0.165f, 0.129f, 0.110f)),
            new CoatDefinition("회색", new Color(0.561f, 0.549f, 0.525f)),
            new CoatDefinition("적갈색", new Color(0.604f, 0.290f, 0.165f)),
            new CoatDefinition("백색", new Color(0.867f, 0.847f, 0.800f)),
            new CoatDefinition("얼룩", new Color(0.420f, 0.337f, 0.267f)),
        };

        [Header("훈련 중 돌발 이벤트 (위험 50% / 안전 80%)")]
        [SerializeField] private TrainingEventDefinition[] trainingEvents =
        {
            new TrainingEventDefinition("말이 당근을 보고 흥분했습니다!", "당근을 준다", "진정시킨다"),
            new TrainingEventDefinition("훈련장에 낯선 새가 날아들어 말이 놀랐습니다.", "그대로 훈련한다", "잠시 쉬게 한다"),
            new TrainingEventDefinition("옆 마방의 말에게 강한 경쟁심을 보입니다.", "함께 병주시킨다", "떼어놓는다"),
            new TrainingEventDefinition("갑자기 소나기가 쏟아집니다.", "빗속 훈련 강행", "실내 마장으로 이동"),
        };

        [Header("시장 음식 (사람 음식은 오늘 하루 효과, 말 음식은 고른 말에게 바로 먹임)")]
        [SerializeField] private FoodDefinition[] foods =
        {
            new FoodDefinition("김밥", 300, false, FoodEffect.SearchBoost, 1.5f, 0, "든든하게 먹고 힘내기. 오늘 하루 쓰레기통 뒤지기 1.5배 빠르게"),
            new FoodDefinition("아이스 커피", 200, false, FoodEffect.Coffee, 1f, 0, "정신이 번쩍! 오늘 하루 Shift 없이도 달리는 속도로 걷기"),
            new FoodDefinition("당근 한 다발", 300, true, FoodEffect.Condition, 15f, 0, "말이 제일 좋아하는 간식. 컨디션 +15"),
            new FoodDefinition("각설탕", 500, true, FoodEffect.Fatigue, 25f, 0, "달콤한 휴식. 피로 -25"),
            new FoodDefinition("고급 사료", 4000, true, FoodEffect.PermanentStat, 1f, 2, "고른 스탯 하나를 영구히 +1 (상한까지)"),
            new FoodDefinition("황금 귀리", 12000, true, FoodEffect.PermanentAll, 0.5f, 1, "전설의 귀리. 6개 스탯 모두 영구히 +0.5"),
        };

        public FoodDefinition[] Foods { get { return foods; } }
        public string[] NamePrefixes { get { return namePrefixes; } }
        public string[] NameSuffixes { get { return nameSuffixes; } }
        public CoatDefinition[] Coats { get { return coats; } }
        public TrainingEventDefinition[] TrainingEvents { get { return trainingEvents; } }
    }
}
