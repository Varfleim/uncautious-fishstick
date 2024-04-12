namespace SO.Country.Events
{
    public readonly struct RCountryCreating
    {
        public RCountryCreating(
            string countryName)
        {
            this.countryName = countryName;
        }

        public readonly string countryName;
    }
}