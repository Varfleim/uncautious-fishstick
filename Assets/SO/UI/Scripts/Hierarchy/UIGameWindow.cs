
using UnityEngine;
using SO.UI.Game.GUI;

namespace SO.UI
{
    public enum MainPanelType : byte
    {
        None,

        Object
    }

    public class UIGameWindow : UIAMainWindow
    {
        public MainPanelType activeMainPanelType;
        public GameObject activeMainPanel;

        public UIObjectPanel objectPanel;

        public void HideActivePanel()
        {
            //�������� �������� ������� ������
            activeMainPanel.SetActive(false);

            //���������, ��� ��� �������� ������� ������
            activeMainPanelType = MainPanelType.None;
            activeMainPanel = null;
        }
    }
}