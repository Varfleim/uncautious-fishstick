
using System.Collections.Generic;

using UnityEngine.UI;

using TMPro;
using SO.Map.UI;
using SO.Map.Economy;
using SO.Map.Province;

namespace SO.UI.Game.GUI.Object.MapArea.Provinces
{
    public class UIProvinceSummaryPanel : UIAProvinceSummaryPanel
    {
        public static UIProvinceSummaryPanel panelPrefab;
        static List<UIProvinceSummaryPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels)
        {
            //Заносим панель в список кэшированных
            cachedPanels.Add(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel);

            //Скрываем панель и обнуляем родительский объект
            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.gameObject.SetActive(false);
            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.transform.SetParent(null);

            //Удаляем ссылку на панель
            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel = null;
        }

        public static void InstantiatePanel(
            ref CProvinceCore pC, ref CProvinceEconomy pE, ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels,
            VerticalLayoutGroup parentLayout)
        {
            //Создаём пустую переменную для панели
            UIProvinceSummaryPanel provinceSummaryPanel;

            //Если список кэшированных панелей не пуст
            if(cachedPanels.Count > 0)
            {
                //Берём последнюю панель в списке и удаляем её из списка
                provinceSummaryPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую панель
                provinceSummaryPanel = Instantiate(panelPrefab);
            }

            //Заполняем данные панели
            provinceSummaryPanel.selfPE = pC.selfPE;

            //Обновляем данные панели
            provinceSummaryPanel.RefreshPanel(ref pC, ref pE);

            //Отображаем новую панель и присоединяем её к указанному родителю
            provinceSummaryPanel.gameObject.SetActive(true);
            provinceSummaryPanel.transform.SetParent(parentLayout.transform);

            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel = provinceSummaryPanel;
        }

        public void RefreshPanel(
            ref CProvinceCore pC, ref CProvinceEconomy pE)
        {
            //Отображаем название провинции
            selfName.text = pC.selfPE.Id.ToString();
        }
    }
}