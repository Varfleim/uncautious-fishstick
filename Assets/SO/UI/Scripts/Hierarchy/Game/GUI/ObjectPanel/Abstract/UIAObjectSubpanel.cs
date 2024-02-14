
using UnityEngine;

namespace SO.UI.Game.GUI.Object
{
    public class UIAObjectSubpanel : MonoBehaviour
    {
        public RectTransform parentRect;

        public TabGroup tabGroup;

        public UIASubpanelTab activeTab;

        public void HideActiveTab()
        {
            //�������� �������� �������
            activeTab.gameObject.SetActive(false);

            //������� PE ��������� �������
            activeTab.objectPE = new();

            //���������, ��� ��� �������� �������
            activeTab = null;
        }
    }
}