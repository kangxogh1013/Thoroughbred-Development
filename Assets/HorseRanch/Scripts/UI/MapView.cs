using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>지도 (M): 현재 구역의 축소 지도 + 내 위치 + 장소 이름</summary>
    public class MapView : RanchViewBase
    {
        [SerializeField] private WorldMap map;
        [SerializeField] private PlayerController player;
        [SerializeField] private Image mapImage;
        [SerializeField] private RectTransform marker;
        [SerializeField] private TMP_Text areaText;
        [Tooltip("이름표 풀 (구역의 장소 수만큼 사용)")]
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private Vector2 frameSize = new Vector2(1500f, 760f);

        private void Update()
        {
            // 열려 있는 동안 위치 갱신 (NPC 이동 등과 무관하게 내 위치만)
            UpdateMarker();
        }

        public override void Refresh()
        {
            var area = map.AreaAt(player.Mover.Cell);
            if (area == null) return;
            areaText.text = area.AreaName + "  <size=70%><color=#8B927F>● 현재 위치</color></size>";
            mapImage.sprite = area.MapSprite;
            float s = Mathf.Min(frameSize.x / area.Width, frameSize.y / area.Height);
            mapImage.rectTransform.sizeDelta = new Vector2(area.Width * s, area.Height * s);
            var names = area.MapLabels;
            var cells = area.MapLabelCells;
            for (int i = 0; i < labels.Length; i++)
            {
                bool on = names != null && i < names.Length;
                labels[i].gameObject.SetActive(on);
                if (!on) continue;
                labels[i].text = names[i];
                labels[i].rectTransform.anchoredPosition = ToMap(area, cells[i], s);
            }
            UpdateMarker();
        }

        private void UpdateMarker()
        {
            var area = map.AreaAt(player.Mover.Cell);
            if (area == null) return;
            float s = Mathf.Min(frameSize.x / area.Width, frameSize.y / area.Height);
            marker.anchoredPosition = ToMap(area, new Vector2(player.Mover.Cell.x - area.Origin.x + 0.5f, player.Mover.Cell.y - area.Origin.y + 0.5f), s);
        }

        private static Vector2 ToMap(WorldArea a, Vector2 local, float s)
        {
            return new Vector2((local.x - a.Width * 0.5f) * s, (local.y - a.Height * 0.5f) * s);
        }
    }
}
