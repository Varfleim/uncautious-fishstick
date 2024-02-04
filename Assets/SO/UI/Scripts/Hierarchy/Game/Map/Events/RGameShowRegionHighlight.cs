
using Leopotam.EcsLite;

namespace SO.UI.Game.Map.Events
{
    public enum RegionHighlightRequestType : byte
    {
        None,
        Hover
    }

    public readonly struct RGameShowRegionHighlight
    {
        public RGameShowRegionHighlight(
            EcsPackedEntity regionPE, 
            RegionHighlightRequestType requestType)
        {
            this.regionPE = regionPE;
            
            this.requestType = requestType;
        }

        public readonly EcsPackedEntity regionPE;

        public readonly RegionHighlightRequestType requestType;
    }
}