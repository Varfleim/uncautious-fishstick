using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(Image))]
public class TabGroupButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public TabGroup tabGroup;

    public Image backgroundImage;

    public UnityEvent onTabSelected;
    public UnityEvent onTabDeselected;

    void Start()
    {
        backgroundImage = GetComponent<Image>();

        //tabGroup.Subscribe(this);

    }

    public void Select()
    {
        if(onTabSelected != null)
        {
            onTabSelected.Invoke();
        }
    }

    public void Deselect()
    {
        if (onTabDeselected != null)
        {
            onTabDeselected.Invoke();
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        tabGroup.OnTabSelected(this);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        tabGroup.OnTabEnter(this);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        tabGroup.OnTabExit(this);
    }
}
