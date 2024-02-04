namespace SO.Map.Hexasphere
{
    public struct CRegionHoverHighlightRenderer
    {
        public CRegionHoverHighlightRenderer(
            GORegionRenderer renderer)
        {
            this.renderer = renderer;
        }

        public GORegionRenderer renderer;
    }
}