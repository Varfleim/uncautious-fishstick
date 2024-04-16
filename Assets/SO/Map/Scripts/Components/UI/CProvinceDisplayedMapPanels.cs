
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using SO.UI.Game.Map;
using SO.Map.Hexasphere;

namespace SO.Map.UI
{
    public struct CProvinceDisplayedMapPanels
    {
        public CProvinceDisplayedMapPanels(int a)
        {
            mapPanelGroup = null;

            mainMapPanel = null;

            tFMainMapPanels = new();
        }

        public static VerticalLayoutGroup mapPanelGroupPrefab;
        static List<VerticalLayoutGroup> cachedMapPanelGroups = new();

        public VerticalLayoutGroup mapPanelGroup;

        public bool IsEmpty
        {
            get
            {
                if (mainMapPanel == null
                    && tFMainMapPanels.Count == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public UIPCMainMapPanel mainMapPanel;

        public List<UITFMainMapPanel> tFMainMapPanels;


        public static void CacheMapPanelGroup(
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels)
        {
            //Заносим группу в список кэшированных
            cachedMapPanelGroups.Add(provinceDisplayedMapPanels.mapPanelGroup);

            //Скрываем группу и обнуляем родительский объект
            provinceDisplayedMapPanels.mapPanelGroup.gameObject.SetActive(false);
            provinceDisplayedMapPanels.mapPanelGroup.transform.SetParent(null);

            //Удаляем ссылку на панель
            provinceDisplayedMapPanels.mapPanelGroup = null;
        }

        public static void InstantiateMapPanelGroup(
            ref CProvinceHexasphere pHS, ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels,
            float hexasphereScale,
            float mapPanelAltitude)
        {
            //Создаём пустую переменную для группы
            VerticalLayoutGroup mapPanelGroup;

            //Если список кэшированных групп не пуст, то берём кэшированную
            if (cachedMapPanelGroups.Count > 0)
            {
                //Берём последнюю группу в списке и удаляем её из списка
                mapPanelGroup = cachedMapPanelGroups[cachedMapPanelGroups.Count - 1];
                cachedMapPanelGroups.RemoveAt(cachedMapPanelGroups.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую группу
                mapPanelGroup = Object.Instantiate(mapPanelGroupPrefab);
            }

            //Отображаем новую группу и присоединяем её к указанному родителю
            mapPanelGroup.gameObject.SetActive(true);
            mapPanelGroup.transform.SetParent(pHS.selfObject.transform);
            provinceDisplayedMapPanels.mapPanelGroup = mapPanelGroup;

            //Задаём положение группы
            Vector3 provinceCenter = pHS.GetProvinceCenter() * hexasphereScale;
            Vector3 direction = provinceCenter.normalized * mapPanelAltitude;
            provinceDisplayedMapPanels.mapPanelGroup.transform.position = provinceCenter + direction;
        }

        public void SetParentTaskForceMainMapPanel(
            UITFMainMapPanel tFMainMapPanel)
        {
            //Присоединяем переданную панель карты оперативной группы к группе панелей карты
            tFMainMapPanel.transform.SetParent(mapPanelGroup.transform);
            tFMainMapPanel.transform.localPosition = Vector3.zero;
            tFMainMapPanel.transform.localRotation = Quaternion.Euler(Vector3.zero);

            tFMainMapPanel.transform.localScale = Vector3.one;

            //Заносим панель карты в список соответствующих панелей
            tFMainMapPanels.Add(tFMainMapPanel);
        }

        public void CancelParentTaskForceMainMapPanel(
            UITFMainMapPanel tFMainMapPanel)
        {
            //Обнуляем родительский объект панели карты оперативной группы
            tFMainMapPanel.transform.SetParent(null);

            //Удаляем панель из списка соответствующих панелей
            tFMainMapPanels.Remove(tFMainMapPanel);
        }
    }
}