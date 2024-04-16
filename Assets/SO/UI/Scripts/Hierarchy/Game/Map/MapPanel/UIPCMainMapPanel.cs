using System.Collections.Generic;

using UnityEngine;

using TMPro;
using SO.Map.UI;
using SO.Map.Province;

namespace SO.UI.Game.Map
{
    public class UIPCMainMapPanel : UIAMapPanel
    {
        public static UIPCMainMapPanel panelPrefab;
        static List<UIPCMainMapPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels)
        {
            //Заносим панель в список кэшированных
            cachedPanels.Add(provinceDisplayedMapPanels.mainMapPanel);

            //Скрываем панель и обнуляем родительский объект
            provinceDisplayedMapPanels.mainMapPanel.gameObject.SetActive(false);
            provinceDisplayedMapPanels.mainMapPanel.transform.SetParent(null);

            //Удаляем ссылку на панель
            provinceDisplayedMapPanels.mainMapPanel = null;
        }

        public static void InstantiatePanel(
            ref CProvinceCore pC, ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels)
        {
            //Создаём пустую переменную для панели
            UIPCMainMapPanel provinceMainMapPanel;

            //Если список кэшированных панелей не пуст, то берём кэшированную
            if(cachedPanels.Count > 0)
            {
                //Берём последнюю панель в списке и удаляем её из списка
                provinceMainMapPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую панель
                provinceMainMapPanel = Instantiate(panelPrefab);
            }

            //Заполняем данные панели
            provinceMainMapPanel.selfPE = pC.selfPE;

            //Обновляем данные панели
            provinceMainMapPanel.RefreshPanel(ref pC);

            //Отображаем новую панель и присоединяем её к указанному родителю
            provinceMainMapPanel.gameObject.SetActive(true);
            provinceMainMapPanel.transform.SetParent(provinceDisplayedMapPanels.mapPanelGroup.transform);
            provinceMainMapPanel.transform.localPosition = Vector3.zero;

            provinceDisplayedMapPanels.mainMapPanel = provinceMainMapPanel;
        }

        public void RefreshPanel(
            ref CProvinceCore pC)
        {
            //Отображаем название провинции
            selfName.text = pC.Index.ToString();
        }
    }
}