namespace SO.Character.Events
{
    public readonly struct RCharacterCreating
    {
        public RCharacterCreating(
            string characterName)
        {
            this.characterName = characterName;
        }

        public readonly string characterName;
    }
}