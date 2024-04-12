
using System.Collections.Generic;

using UnityEngine.UI;

using TMPro;
using SO.Map.UI;
using SO.Map.Economy;
using SO.Map.Region;

namespace SO.UI.Game.GUI.Object.StrategicArea.Regions
{
    public class UIRegionSummaryPanel : UIARegionSummaryPanel
    {
        public static UIRegionSummaryPanel panelPrefab;
        static List<UIRegionSummaryPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels)
        {
            //Заносим панель в список кэшированных
            cachedPanels.Add(regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel);

            //Скрываем панель и обнуляем родительский объект
            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.gameObject.SetActive(false);
            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.transform.SetParent(null);

            //Удаляем ссылку на панель
            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel = null;
        }

        public static void InstantiatePanel(
            ref CRegionCore rC, ref CRegionEconomy rE, ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels,
            VerticalLayoutGroup parentLayout)
        {
            //Создаём пустую переменную для панели
            UIRegionSummaryPanel regionSummaryPanel;

            //Если список кэшированных панелей не пуст
            if(cachedPanels.Count > 0)
            {
                //Берём последнюю панель в списке и удаляем её из списка
                regionSummaryPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую панель
                regionSummaryPanel = Instantiate(panelPrefab);
            }

            //Заполняем данные панели
            regionSummaryPanel.selfPE = rC.selfPE;

            //Обновляем данные панели
            regionSummaryPanel.RefreshPanel(ref rC, ref rE);

            //Отображаем новую панель и присоединяем её к указанному родителю
            regionSummaryPanel.gameObject.SetActive(true);
            regionSummaryPanel.transform.SetParent(parentLayout.transform);

            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel = regionSummaryPanel;
        }

        public void RefreshPanel(
            ref CRegionCore rC, ref CRegionEconomy rE)
        {
            //Отображаем название региона
            selfName.text = rC.selfPE.Id.ToString();
        }
    }
}