
using System.Collections.Generic;

using UnityEngine.UI;

using TMPro;
using SO.Map.UI;
using SO.Map.Economy;
using SO.Map.Province;

namespace SO.UI.Game.GUI.Object.MapArea.Provinces
{
    public class UIProvinceSummaryPanel : UIAProvinceSummaryPanel
    {
        public static UIProvinceSummaryPanel panelPrefab;
        static List<UIProvinceSummaryPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels)
        {
            //������� ������ � ������ ������������
            cachedPanels.Add(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel);

            //�������� ������ � �������� ������������ ������
            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.gameObject.SetActive(false);
            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.transform.SetParent(null);

            //������� ������ �� ������
            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel = null;
        }

        public static void InstantiatePanel(
            ref CProvinceCore pC, ref CProvinceEconomy pE, ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels,
            VerticalLayoutGroup parentLayout)
        {
            //������ ������ ���������� ��� ������
            UIProvinceSummaryPanel provinceSummaryPanel;

            //���� ������ ������������ ������� �� ����
            if(cachedPanels.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                provinceSummaryPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                provinceSummaryPanel = Instantiate(panelPrefab);
            }

            //��������� ������ ������
            provinceSummaryPanel.selfPE = pC.selfPE;

            //��������� ������ ������
            provinceSummaryPanel.RefreshPanel(ref pC, ref pE);

            //���������� ����� ������ � ������������ � � ���������� ��������
            provinceSummaryPanel.gameObject.SetActive(true);
            provinceSummaryPanel.transform.SetParent(parentLayout.transform);

            provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel = provinceSummaryPanel;
        }

        public void RefreshPanel(
            ref CProvinceCore pC, ref CProvinceEconomy pE)
        {
            //���������� �������� ���������
            selfName.text = pC.selfPE.Id.ToString();
        }
    }
}