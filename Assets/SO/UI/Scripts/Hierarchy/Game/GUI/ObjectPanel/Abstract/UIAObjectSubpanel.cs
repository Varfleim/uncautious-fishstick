
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
            //Скрываем активную вкладку
            activeTab.gameObject.SetActive(false);

            //Очищаем PE активного объекта
            activeTab.objectPE = new();

            //Указываем, что нет активной вкладки
            activeTab = null;
        }
    }
}