
using System.Collections.Generic;

using UnityEngine;

public class TabGroup : MonoBehaviour
{
    public List<TabGroupButton> tabButtons;
    public TabGroupButton selectedTab;

    public List<GameObject> objectsToSwap;

    public Sprite tabIdle;
    public Sprite tabHover;
    public Sprite tabSelected;

    public void Subscribe(TabGroupButton button)
    {
        if(tabButtons == null)
        {
            tabButtons = new List<TabGroupButton>();
        }

        tabButtons.Add(button);
    }

    public void OnTabEnter(TabGroupButton button)
    {
        ResetTabs();

        if (selectedTab == null
            || selectedTab != button)
        {
            button.backgroundImage.sprite = tabHover;
        }
    }

    public void OnTabExit(TabGroupButton button)
    {
        ResetTabs();

        button.backgroundImage.sprite = tabIdle;
    }

    public void OnTabSelected(TabGroupButton button)
    {
        if(selectedTab != null)
        {
            selectedTab.Deselect();
        }

        selectedTab = button;

        selectedTab.Select();

        ResetTabs();

        button.backgroundImage.sprite = tabSelected;

        int index = button.transform.GetSiblingIndex();
        for(int a = 0; a < objectsToSwap.Count; a++)
        {
            if (a == index)
            {
                objectsToSwap[a].SetActive(true);
            }
            else
            {
                objectsToSwap[a].SetActive(false);
            }
        }
    }

    public void ResetTabs()
    {
        foreach(TabGroupButton tabButton in tabButtons)
        {
            if(selectedTab != null
                && selectedTab == tabButton)
            {
                continue;
            }

            tabButton.backgroundImage.sprite = tabIdle;
        }
    }
}
