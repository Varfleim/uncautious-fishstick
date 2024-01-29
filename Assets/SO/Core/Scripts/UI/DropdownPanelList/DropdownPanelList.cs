
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
            //Если был активен какой-либо список
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
            //Если список неактивен
            if (scrollView.gameObject.activeSelf == false)
            {
                //Если был активен какой-либо список
                if (openPanelList != null)
                {
                    //Переключаем его
                    openPanelList.ToggleList();
                }

                //Отображаем текущий список
                ShowList();

                //Сохраняем ссылку на текущий список
                openPanelList = this;
            }
            //Иначе
            else
            {
                //Скрываем текущий список
                HideList();

                //Удаляем ссылку на открытый список
                openPanelList = null;
            }
        }

        void ShowList()
        {
            //Отображаем список
            scrollView.gameObject.SetActive(true);
        }

        void HideList()
        {
            //Скрываем список
            scrollView.gameObject.SetActive(false);
        }

        public void CachePanel(
            MonoBehaviour panel)
        {
            //Заносим панель в список кэшированных
            cachedPanels.Add(panel);

            //Удаляем панель из списка активных
            activePanels.Remove(panel);

            //Скрываем панель и обнуляем родительский объект
            panel.gameObject.SetActive(false);
            panel.transform.SetParent(null);

            //Обновляем размер списка
            RefreshListSize();
        }

        public TPanel InstantiatePanel<TPanel>(
            TPanel panelPrefab)
            where TPanel : MonoBehaviour
        {
            //Создаём пустую переменную для панели
            TPanel panel;

            //Если список кэшированных панелей не пуст, то берём кэшированную
            if (cachedPanels.Count < 0)
            {
                //Берём последнюю панель в списке и удаляем её из списка
                panel = cachedPanels[cachedPanels.Count - 1] as TPanel;
                cachedPanels.RemoveAt(cachedPanels.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новую панель
                panel = Instantiate(panelPrefab);
            }

            //Отображаем панель и присоединяем к списку
            panel.gameObject.SetActive(true);
            panel.transform.SetParent(panelsLayoutGroup.transform);
            activePanels.Add(panel);

            //Обновляем размер списка
            RefreshListSize();

            return panel;
        }

        void RefreshListSize()
        {
            //Если количество панелей в списке меньше пяти
            if (activePanels.Count < 5)
            {
                //Устанавливаем размер списка соответственное количеству панелей
                scrollView.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    activePanels.Count * 40);
            }
            //Иначе, если количество больше или равно пяти
            else if (activePanels.Count >= 5)
            {
                //Устанавливаем размер списка как при пяти элементах
                scrollView.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    5 * 40);
            }
            //Иначе
            else
            {
                //Устанавливаем размер списка как при одном элементе
                scrollView.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    1 * 40);
            }
        }
    }
}