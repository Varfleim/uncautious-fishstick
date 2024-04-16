
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
            //������� ������ � ������ ������������
            cachedMapPanelGroups.Add(provinceDisplayedMapPanels.mapPanelGroup);

            //�������� ������ � �������� ������������ ������
            provinceDisplayedMapPanels.mapPanelGroup.gameObject.SetActive(false);
            provinceDisplayedMapPanels.mapPanelGroup.transform.SetParent(null);

            //������� ������ �� ������
            provinceDisplayedMapPanels.mapPanelGroup = null;
        }

        public static void InstantiateMapPanelGroup(
            ref CProvinceHexasphere pHS, ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels,
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
                mapPanelGroup = Object.Instantiate(mapPanelGroupPrefab);
            }

            //���������� ����� ������ � ������������ � � ���������� ��������
            mapPanelGroup.gameObject.SetActive(true);
            mapPanelGroup.transform.SetParent(pHS.selfObject.transform);
            provinceDisplayedMapPanels.mapPanelGroup = mapPanelGroup;

            //����� ��������� ������
            Vector3 provinceCenter = pHS.GetProvinceCenter() * hexasphereScale;
            Vector3 direction = provinceCenter.normalized * mapPanelAltitude;
            provinceDisplayedMapPanels.mapPanelGroup.transform.position = provinceCenter + direction;
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