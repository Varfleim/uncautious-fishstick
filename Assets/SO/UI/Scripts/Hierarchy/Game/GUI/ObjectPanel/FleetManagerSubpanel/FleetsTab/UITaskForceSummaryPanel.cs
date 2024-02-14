
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
            //������� ������ � ������ ������������
            cachedPanels.Add(tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel);

            //�������� ������ � �������� ������������ ������
            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.gameObject.SetActive(false);
            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.transform.SetParent(null);

            //������� ������ �� ������
            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel = null;
        }

        public static void InstantiatePanel(
            ref CTaskForce tF, ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels,
            VerticalLayoutGroup parentLayout)
        {
            //������ ������ ���������� ��� ������
            UITaskForceSummaryPanel tFSummaryPanel;

            //���� ������ ������������ ������� �� ����
            if(cachedPanels.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                tFSummaryPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                tFSummaryPanel = Instantiate(panelPrefab);
            }

            //��������� ������ ������
            tFSummaryPanel.selfPE = tF.selfPE;

            //��������� ������ ������
            tFSummaryPanel.RefreshPanel(ref tF);

            //���������� ����� ������ � ������������ � � ���������� ��������
            tFSummaryPanel.gameObject.SetActive(true);
            tFSummaryPanel.transform.SetParent(parentLayout.transform);

            tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel = tFSummaryPanel;
        }

        public void RefreshPanel(
            ref CTaskForce tF)
        {
            //���������� �������� ����������� ������
            selfName.text = tF.selfPE.Id.ToString();
        }
    }
}