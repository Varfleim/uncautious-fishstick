
using System.Collections.Generic;

using UnityEngine.UI;

using TMPro;
using SO.Map.UI;
using SO.Map.Economy;
using SO.Map.Region;

namespace SO.UI.Game.GUI.Object.StrategicArea.Regions
{
    public class UIRegionSummaryPanel : UIARegionSummaryPanel
    {
        public static UIRegionSummaryPanel panelPrefab;
        static List<UIRegionSummaryPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels)
        {
            //������� ������ � ������ ������������
            cachedPanels.Add(regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel);

            //�������� ������ � �������� ������������ ������
            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.gameObject.SetActive(false);
            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.transform.SetParent(null);

            //������� ������ �� ������
            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel = null;
        }

        public static void InstantiatePanel(
            ref CRegionCore rC, ref CRegionEconomy rE, ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels,
            VerticalLayoutGroup parentLayout)
        {
            //������ ������ ���������� ��� ������
            UIRegionSummaryPanel regionSummaryPanel;

            //���� ������ ������������ ������� �� ����
            if(cachedPanels.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                regionSummaryPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                regionSummaryPanel = Instantiate(panelPrefab);
            }

            //��������� ������ ������
            regionSummaryPanel.selfPE = rC.selfPE;

            //��������� ������ ������
            regionSummaryPanel.RefreshPanel(ref rC, ref rE);

            //���������� ����� ������ � ������������ � � ���������� ��������
            regionSummaryPanel.gameObject.SetActive(true);
            regionSummaryPanel.transform.SetParent(parentLayout.transform);

            regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel = regionSummaryPanel;
        }

        public void RefreshPanel(
            ref CRegionCore rC, ref CRegionEconomy rE)
        {
            //���������� �������� �������
            selfName.text = rC.selfPE.Id.ToString();
        }
    }
}