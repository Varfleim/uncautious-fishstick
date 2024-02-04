
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using SO.UI.Game.Map;
using SO.Map.Hexasphere;

namespace SO.Map
{
    public struct CRegionDisplayedMapPanels
    {
        public static VerticalLayoutGroup mapPanelGroupPrefab;
        static List<VerticalLayoutGroup> cachedMapPanelGroups = new();

        public VerticalLayoutGroup mapPanelGroup;

        public UIRCMainMapPanel mainMapPanel;

        public static void CacheMapPanelGroup(
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels)
        {
            //Заносим группу в список кэшированных
            cachedMapPanelGroups.Add(regionDisplayedMapPanels.mapPanelGroup);

            //Скрываем группу и обнуляем родительский объект
            regionDisplayedMapPanels.mapPanelGroup.gameObject.SetActive(false);
            regionDisplayedMapPanels.mapPanelGroup.transform.SetParent(null);

            //Удаляем ссылку на панель
            regionDisplayedMapPanels.mapPanelGroup = null;
        }

        public static void InstantiateMapPanelGroup(
            ref CRegionHexasphere rHS, ref CRegionDisplayedMapPanels regionDisplayedMapPanels,
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
                mapPanelGroup = MonoBehaviour.Instantiate(mapPanelGroupPrefab);
            }

            //Отображаем новую группу и присоединяем её к указанному родителю
            mapPanelGroup.gameObject.SetActive(true);
            mapPanelGroup.transform.SetParent(rHS.selfObject.transform);
            regionDisplayedMapPanels.mapPanelGroup = mapPanelGroup;

            //Задаём положение группы
            Vector3 regionCenter = rHS.GetRegionCenter() * hexasphereScale;
            Vector3 direction = regionCenter.normalized * mapPanelAltitude;
            regionDisplayedMapPanels.mapPanelGroup.transform.position = regionCenter + direction;
        }
    }
}