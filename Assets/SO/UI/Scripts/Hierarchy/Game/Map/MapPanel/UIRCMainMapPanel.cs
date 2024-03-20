using System.Collections.Generic;

using UnityEngine;

using TMPro;
using SO.Map.UI;
using SO.Map.Region;

namespace SO.UI.Game.Map
{
    public class UIRCMainMapPanel : UIAMapPanel
    {
        public static UIRCMainMapPanel panelPrefab;
        static List<UIRCMainMapPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels)
        {
            //Заносим панель в список кэшированных
            cachedPanels.Add(regionDisplayedMapPanels.mainMapPanel);

            //Скрываем панель и обнуляем родительский объект
            regionDisplayedMapPanels.mainMapPanel.gameObject.SetActive(false);
            regionDisplayedMapPanels.mainMapPanel.transform.SetParent(null);

            //Удаляем ссылку на панель
            regionDisplayedMapPanels.mainMapPanel = null;
        }

        public static void InstantiatePanel(
            ref CRegionCore rC, ref CRegionDisplayedMapPanels regionDisplayedMapPanels)
        {
            //Создаём пустую переменную для панели
            UIRCMainMapPanel regionMainMapPanel;

            //Если список кэшированных панелей не пуст, то берём кэшированную
            if(cachedPanels.Count > 0)
            {
                //Берём последнюю панель в списке и удаляем её из списка
                regionMainMapPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую панель
                regionMainMapPanel = Instantiate(panelPrefab);
            }

            //Заполняем данные панели
            regionMainMapPanel.selfPE = rC.selfPE;

            //Обновляем данные панели
            regionMainMapPanel.RefreshPanel(ref rC);

            //Отображаем новую панель и присоединяем её к указанному родителю
            regionMainMapPanel.gameObject.SetActive(true);
            regionMainMapPanel.transform.SetParent(regionDisplayedMapPanels.mapPanelGroup.transform);
            regionMainMapPanel.transform.localPosition = Vector3.zero;

            regionDisplayedMapPanels.mainMapPanel = regionMainMapPanel;
        }

        public void RefreshPanel(
            ref CRegionCore rC)
        {
            //Отображаем название региона
            selfName.text = rC.Index.ToString();
        }
    }
}