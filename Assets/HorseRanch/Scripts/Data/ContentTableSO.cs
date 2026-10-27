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

        public string[] NamePrefixes { get { return namePrefixes; } }
        public string[] NameSuffixes { get { return nameSuffixes; } }
        public CoatDefinition[] Coats { get { return coats; } }
        public TrainingEventDefinition[] TrainingEvents { get { return trainingEvents; } }
    }
}
