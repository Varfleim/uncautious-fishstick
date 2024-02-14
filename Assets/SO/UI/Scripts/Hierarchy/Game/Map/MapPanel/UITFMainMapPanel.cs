using System.Collections.Generic;

using UnityEngine;

using TMPro;

using SO.Map;
using SO.Warfare.Fleet;

namespace SO.UI.Game.Map
{
    public class UITFMainMapPanel : UIAMapPanel
    {
        public static UITFMainMapPanel panelPrefab;
        static List<UITFMainMapPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //������� ������ � ������ ������������
            cachedPanels.Add(tFDisplayedMapPanels.mainMapPanel);

            //�������� ������ � �������� ������������ ������
            tFDisplayedMapPanels.mainMapPanel.gameObject.SetActive(false);
            tFDisplayedMapPanels.mainMapPanel.transform.SetParent(null);

            //������� ������ �� ������
            tFDisplayedMapPanels.mainMapPanel = null;
        }

        public static void InstantiatePanel(
            ref CTaskForce tF, ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //������ ������ ���������� ��� ������
            UITFMainMapPanel tFMainMapPanel;

            //���� ������ ������������ ������� �� ����, �� ���� ������������
            if(cachedPanels.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                tFMainMapPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                tFMainMapPanel = Instantiate(panelPrefab);
            }

            //��������� ������ ������
            tFMainMapPanel.selfPE = tF.selfPE;

            //��������� ������ ������
            tFMainMapPanel.RefreshPanel(ref tF);

            //���������� ����� ������ � ������������ � � ���������� ��������
            tFMainMapPanel.gameObject.SetActive(true);

            tFDisplayedMapPanels.mainMapPanel = tFMainMapPanel;
        }

        public void RefreshPanel(
            ref CTaskForce tF)
        {
            //���������� �������� ����������� ������
            selfName.text = tF.selfPE.Id.ToString();
        }
    }
}