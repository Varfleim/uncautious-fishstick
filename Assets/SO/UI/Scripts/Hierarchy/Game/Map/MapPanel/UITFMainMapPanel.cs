using System.Collections.Generic;

using UnityEngine;

using TMPro;

using SO.Map;
using SO.Warfare.Fleet;

namespace SO.UI.Game.Map
{
    public class UITFMainMapPanel : UIAMapPanel
    {
        public static UITFMainMapPanel panelPrefab;
        static List<UITFMainMapPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //Заносим панель в список кэшированных
            cachedPanels.Add(tFDisplayedMapPanels.mainMapPanel);

            //Скрываем панель и обнуляем родительский объект
            tFDisplayedMapPanels.mainMapPanel.gameObject.SetActive(false);
            tFDisplayedMapPanels.mainMapPanel.transform.SetParent(null);

            //Удаляем ссылку на панель
            tFDisplayedMapPanels.mainMapPanel = null;
        }

        public static void InstantiatePanel(
            ref CTaskForce tF, ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //Создаём пустую переменную для панели
            UITFMainMapPanel tFMainMapPanel;

            //Если список кэшированных панелей не пуст, то берём кэшированную
            if(cachedPanels.Count > 0)
            {
                //Берём последнюю панель в списке и удаляем её из списка
                tFMainMapPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую панель
                tFMainMapPanel = Instantiate(panelPrefab);
            }

            //Заполняем данные панели
            tFMainMapPanel.selfPE = tF.selfPE;

            //Обновляем данные панели
            tFMainMapPanel.RefreshPanel(ref tF);

            //Отображаем новую панель и присоединяем её к указанному родителю
            tFMainMapPanel.gameObject.SetActive(true);

            tFDisplayedMapPanels.mainMapPanel = tFMainMapPanel;
        }

        public void RefreshPanel(
            ref CTaskForce tF)
        {
            //Отображаем название оперативной группы
            selfName.text = tF.selfPE.Id.ToString();
        }
    }
}