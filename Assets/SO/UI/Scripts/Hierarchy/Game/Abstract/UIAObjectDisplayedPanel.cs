
using UnityEngine;

using Leopotam.EcsLite;

namespace SO.UI.Game
{
    public abstract class UIAObjectDisplayedPanel : MonoBehaviour
    {
        public RectTransform selfRect;

        public EcsPackedEntity selfPE;
    }
}