
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
            //������� ������ � ������ ������������
            cachedMapPanelGroups.Add(regionDisplayedMapPanels.mapPanelGroup);

            //�������� ������ � �������� ������������ ������
            regionDisplayedMapPanels.mapPanelGroup.gameObject.SetActive(false);
            regionDisplayedMapPanels.mapPanelGroup.transform.SetParent(null);

            //������� ������ �� ������
            regionDisplayedMapPanels.mapPanelGroup = null;
        }

        public static void InstantiateMapPanelGroup(
            ref CRegionHexasphere rHS, ref CRegionDisplayedMapPanels regionDisplayedMapPanels,
            float hexasphereScale,
            float mapPanelAltitude)
        {
            //������ ������ ���������� ��� ������
            VerticalLayoutGroup mapPanelGroup;

            //���� ������ ������������ ����� �� ����, �� ���� ������������
            if (cachedMapPanelGroups.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                mapPanelGroup = cachedMapPanelGroups[cachedMapPanelGroups.Count - 1];
                cachedMapPanelGroups.RemoveAt(cachedMapPanelGroups.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                mapPanelGroup = MonoBehaviour.Instantiate(mapPanelGroupPrefab);
            }

            //���������� ����� ������ � ������������ � � ���������� ��������
            mapPanelGroup.gameObject.SetActive(true);
            mapPanelGroup.transform.SetParent(rHS.selfObject.transform);
            regionDisplayedMapPanels.mapPanelGroup = mapPanelGroup;

            //����� ��������� ������
            Vector3 regionCenter = rHS.GetRegionCenter() * hexasphereScale;
            Vector3 direction = regionCenter.normalized * mapPanelAltitude;
            regionDisplayedMapPanels.mapPanelGroup.transform.position = regionCenter + direction;
        }
    }
}