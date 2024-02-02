
using Leopotam.EcsLite;

namespace SO.UI.Game.Events
{
    public enum GamePanelType : byte
    {
        None,
        RegionMainMapPanel
    }

    public readonly struct RGameCreatePanel
    {
        public RGameCreatePanel(
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