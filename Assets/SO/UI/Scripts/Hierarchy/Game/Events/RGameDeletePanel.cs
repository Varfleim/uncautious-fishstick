
using Leopotam.EcsLite;

namespace SO.UI.Game.Events
{
    public readonly struct RGameDeletePanel
    {
        public RGameDeletePanel(
            EcsPackedEntity objectPE, 
            GamePanelType panelType)
        {
            this.objectPE = objectPE;

            this.panelType = panelType;
        }

        public readonly EcsPackedEntity objectPE;

        public readonly GamePanelType panelType;
    }
}