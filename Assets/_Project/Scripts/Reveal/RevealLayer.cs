namespace MysticCircus
{
    // The three looks of every unit. Dream is the default; Warm is before the abduction, Cold after it.
    // The white "reality" look only appears in pair films, so it is not a layer the player can switch to.
    public enum RevealLayer { Dream = 0, Warm = 1, Cold = 2 }

    public static class RevealLogic
    {
        public const int LayerCount = 3;

        // Each shutter press steps Dream → Warm → Cold → Dream.
        public static RevealLayer Next(RevealLayer layer) => (RevealLayer)(((int)layer + 1) % LayerCount);
    }
}
