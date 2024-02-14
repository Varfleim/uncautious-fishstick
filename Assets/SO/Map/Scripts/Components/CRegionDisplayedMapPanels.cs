
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using SO.UI.Game.Map;
using SO.Map.Hexasphere;

namespace SO.Map
{
    public struct CRegionDisplayedMapPanels
    {
        public CRegionDisplayedMapPanels(int a)
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
                if(mainMapPanel == null
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
        public UIRCMainMapPanel mainMapPanel;

        public List<UITFMainMapPanel> tFMainMapPanels;


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

        public void SetParentTaskForceMainMapPanel(
            UITFMainMapPanel tFMainMapPanel)
        {
            //������������ ���������� ������ ����� ����������� ������ � ������ ������� �����
            tFMainMapPanel.transform.SetParent(mapPanelGroup.transform);
            tFMainMapPanel.transform.localPosition = Vector3.zero;
            tFMainMapPanel.transform.localRotation = Quaternion.Euler(Vector3.zero);

            tFMainMapPanel.transform.localScale = Vector3.one;

            //������� ������ ����� � ������ ��������������� �������
            tFMainMapPanels.Add(tFMainMapPanel);
        }

        public void CancelParentTaskForceMainMapPanel(
            UITFMainMapPanel tFMainMapPanel)
        {
            //�������� ������������ ������ ������ ����� ����������� ������
            tFMainMapPanel.transform.SetParent(null);

            //������� ������ �� ������ ��������������� �������
            tFMainMapPanels.Remove(tFMainMapPanel);
        }
    }
}