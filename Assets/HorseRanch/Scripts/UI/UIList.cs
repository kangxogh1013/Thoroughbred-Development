using System.Collections.Generic;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 가변 길이 목록. 비활성 템플릿을 필요한 만큼만 복제하고 이후엔 재사용 (잦은 Instantiate/Destroy 방지)
    /// </summary>
    public class UIList : MonoBehaviour
    {
        [SerializeField] private UIRow template;
        [SerializeField] private RectTransform container;

        private readonly List<UIRow> rows = new List<UIRow>();

        private void Awake()
        {
            if (template != null) template.gameObject.SetActive(false);
        }

        public UIRow Get(int i)
        {
            while (rows.Count <= i)
            {
                UIRow r = Instantiate(template, container);
                r.name = template.name + "_" + rows.Count;
                rows.Add(r);
            }
            return rows[i];
        }

        public void SetCount(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var r = Get(i);
                if (!r.gameObject.activeSelf) r.gameObject.SetActive(true);
            }
            for (int i = count; i < rows.Count; i++)
                if (rows[i].gameObject.activeSelf) rows[i].gameObject.SetActive(false);
        }
    }
}
