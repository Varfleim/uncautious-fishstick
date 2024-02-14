
using System.Collections.Generic;

using UnityEngine.UI;

using TMPro;

using SO.Warfare.Fleet;

namespace SO.UI.Game.GUI.Object.FleetManager.Fleets
{
    public class UITaskForceSummaryPanel : UIATaskForcesSummaryPanel
    {
        public static UITaskForceSummaryPanel panelPrefab;
        static List<UITaskForceSummaryPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public Toggle toggle;

        public static void CachePanel(
            ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels)
        {
            //Заносим панель в список кэшированных
            cachedPanels.Add(tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel);

            //Скрываем панель и обнуляем родительский объект
            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.gameObject.SetActive(false);
            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.transform.SetParent(null);

            //Удаляем ссылку на панель
            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel = null;
        }

        public static void InstantiatePanel(
            ref CTaskForce tF, ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels,
            VerticalLayoutGroup parentLayout)
        {
            //Создаём пустую переменную для панели
            UITaskForceSummaryPanel tFSummaryPanel;

            //Если список кэшированных панелей не пуст
            if(cachedPanels.Count > 0)
            {
                //Берём последнюю панель в списке и удаляем её из списка
                tFSummaryPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую панель
                tFSummaryPanel = Instantiate(panelPrefab);
            }

            //Заполняем данные панели
            tFSummaryPanel.selfPE = tF.selfPE;

            //Обновляем данные панели
            tFSummaryPanel.RefreshPanel(ref tF);

            //Отображаем новую панель и присоединяем её к указанному родителю
            tFSummaryPanel.gameObject.SetActive(true);
            tFSummaryPanel.transform.SetParent(parentLayout.transform);

            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel = tFSummaryPanel;
        }

        public void RefreshPanel(
            ref CTaskForce tF)
        {
            //Отображаем название оперативной группы
            selfName.text = tF.selfPE.Id.ToString();
        }
    }
}