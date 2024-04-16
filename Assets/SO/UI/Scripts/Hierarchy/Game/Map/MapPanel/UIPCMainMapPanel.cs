using System.Collections.Generic;

using UnityEngine;

using TMPro;
using SO.Map.UI;
using SO.Map.Province;

namespace SO.UI.Game.Map
{
    public class UIPCMainMapPanel : UIAMapPanel
    {
        public static UIPCMainMapPanel panelPrefab;
        static List<UIPCMainMapPanel> cachedPanels = new();

        public TextMeshProUGUI selfName;

        public static void CachePanel(
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels)
        {
            //������� ������ � ������ ������������
            cachedPanels.Add(provinceDisplayedMapPanels.mainMapPanel);

            //�������� ������ � �������� ������������ ������
            provinceDisplayedMapPanels.mainMapPanel.gameObject.SetActive(false);
            provinceDisplayedMapPanels.mainMapPanel.transform.SetParent(null);

            //������� ������ �� ������
            provinceDisplayedMapPanels.mainMapPanel = null;
        }

        public static void InstantiatePanel(
            ref CProvinceCore pC, ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels)
        {
            //������ ������ ���������� ��� ������
            UIPCMainMapPanel provinceMainMapPanel;

            //���� ������ ������������ ������� �� ����, �� ���� ������������
            if(cachedPanels.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                provinceMainMapPanel = cachedPanels[cachedPanels.Count - 1];
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                provinceMainMapPanel = Instantiate(panelPrefab);
            }

            //��������� ������ ������
            provinceMainMapPanel.selfPE = pC.selfPE;

            //��������� ������ ������
            provinceMainMapPanel.RefreshPanel(ref pC);

            //���������� ����� ������ � ������������ � � ���������� ��������
            provinceMainMapPanel.gameObject.SetActive(true);
            provinceMainMapPanel.transform.SetParent(provinceDisplayedMapPanels.mapPanelGroup.transform);
            provinceMainMapPanel.transform.localPosition = Vector3.zero;

            provinceDisplayedMapPanels.mainMapPanel = provinceMainMapPanel;
        }

        public void RefreshPanel(
            ref CProvinceCore pC)
        {
            //���������� �������� ���������
            selfName.text = pC.Index.ToString();
        }
    }
}