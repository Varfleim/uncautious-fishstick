
using Leopotam.EcsLite;

namespace SO.UI.Game.Map.Events
{
    public enum ProvinceHighlightRequestType : byte
    {
        None,
        Hover
    }

    public readonly struct RGameShowProvinceHighlight
    {
        public RGameShowProvinceHighlight(
            EcsPackedEntity provincePE, 
            ProvinceHighlightRequestType requestType)
        {
            this.provincePE = provincePE;
            
            this.requestType = requestType;
        }

        public readonly EcsPackedEntity provincePE;

        public readonly ProvinceHighlightRequestType requestType;
    }
}