
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace SO.UI
{
    public class DropdownPanelList : MonoBehaviour
    {
        public static bool IsOpen
        {
            get
            {
                return openPanelList;
            }
        }

        static DropdownPanelList openPanelList;

        public static void HideOpenList()
        {
            //���� ��� ������� �����-���� ������
            if (openPanelList != null)
            {
                openPanelList.ToggleList();
            }
        }

        public DropdownPanelList()
        {
            activePanels = new();
            cachedPanels = new();
        }

        public RectTransform scrollView;

        public List<MonoBehaviour> activePanels;
        List<MonoBehaviour> cachedPanels;

        public VerticalLayoutGroup panelsLayoutGroup;

        public void ToggleList()
        {
            //���� ������ ���������
            if (scrollView.gameObject.activeSelf == false)
            {
                //���� ��� ������� �����-���� ������
                if (openPanelList != null)
                {
                    //����������� ���
                    openPanelList.ToggleList();
                }

                //���������� ������� ������
                ShowList();

                //��������� ������ �� ������� ������
                openPanelList = this;
            }
            //�����
            else
            {
                //�������� ������� ������
                HideList();

                //������� ������ �� �������� ������
                openPanelList = null;
            }
        }

        void ShowList()
        {
            //���������� ������
            scrollView.gameObject.SetActive(true);
        }

        void HideList()
        {
            //�������� ������
            scrollView.gameObject.SetActive(false);
        }

        public void CachePanel(
            MonoBehaviour panel)
        {
            //������� ������ � ������ ������������
            cachedPanels.Add(panel);

            //������� ������ �� ������ ��������
            activePanels.Remove(panel);

            //�������� ������ � �������� ������������ ������
            panel.gameObject.SetActive(false);
            panel.transform.SetParent(null);

            //��������� ������ ������
            RefreshListSize();
        }

        public TPanel InstantiatePanel<TPanel>(
            TPanel panelPrefab)
            where TPanel : MonoBehaviour
        {
            //������ ������ ���������� ��� ������
            TPanel panel;

            //���� ������ ������������ ������� �� ����, �� ���� ������������
            if (cachedPanels.Count < 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                panel = cachedPanels[cachedPanels.Count - 1] as TPanel;
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ������
                panel = Instantiate(panelPrefab);
            }

            //���������� ������ � ������������ � ������
            panel.gameObject.SetActive(true);
            panel.transform.SetParent(panelsLayoutGroup.transform);
            activePanels.Add(panel);

            //��������� ������ ������
            RefreshListSize();

            return panel;
        }

        void RefreshListSize()
        {
            //���� ���������� ������� � ������ ������ ����
            if (activePanels.Count < 5)
            {
                //������������� ������ ������ ��������������� ���������� �������
                scrollView.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    activePanels.Count * 40);
            }
            //�����, ���� ���������� ������ ��� ����� ����
            else if (activePanels.Count >= 5)
            {
                //������������� ������ ������ ��� ��� ���� ���������
                scrollView.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    5 * 40);
            }
            //�����
            else
            {
                //������������� ������ ������ ��� ��� ����� ��������
                scrollView.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    1 * 40);
            }
        }
    }
}