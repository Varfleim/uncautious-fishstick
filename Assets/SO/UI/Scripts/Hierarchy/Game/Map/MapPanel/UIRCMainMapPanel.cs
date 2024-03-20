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
            //������� ������ � ������ ������������
            cachedPanels.Add(regionDisplayedMapPanels.mainMapPanel);

            //�������� ������ � �������� ������������ ������
            regionDisplayedMapPanels.mainMapPanel.gameObject.SetActive(false);
            regionDisplayedMapPanels.mainMapPanel.transform.SetParent(null);

            //������� ������ �� ������
            regionDisplayedMapPanels.mainMapPanel = null;
        }

        public static void InstantiatePanel(
            ref CRegionCore rC, ref CRegionDisplayedMapPanels regionDisplayedMapPanels)
        {
            //������ ������ ���������� ��� ������
            UIRCMainMapPanel regionMainMapPanel;

            //���� ������ ������������ ������� �� ����, �� ���� ������������
            if(cachedPanels.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                regionMainMapPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                regionMainMapPanel = Instantiate(panelPrefab);
            }

            //��������� ������ ������
            regionMainMapPanel.selfPE = rC.selfPE;

            //��������� ������ ������
            regionMainMapPanel.RefreshPanel(ref rC);

            //���������� ����� ������ � ������������ � � ���������� ��������
            regionMainMapPanel.gameObject.SetActive(true);
            regionMainMapPanel.transform.SetParent(regionDisplayedMapPanels.mapPanelGroup.transform);
            regionMainMapPanel.transform.localPosition = Vector3.zero;

            regionDisplayedMapPanels.mainMapPanel = regionMainMapPanel;
        }

        public void RefreshPanel(
            ref CRegionCore rC)
        {
            //���������� �������� �������
            selfName.text = rC.Index.ToString();
        }
    }
}