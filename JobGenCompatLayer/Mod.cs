using UnityModManagerNet;

namespace JobGenCompatLayer
{
    public static class Mod
    {
        public static UnityModManager.ModEntry Entry { get; private set; }

        static void OnLoad(UnityModManager.ModEntry loadedEntry)
        {
            Entry = loadedEntry;

            if (!Entry.Enabled) { return; }
        }
    }
}
