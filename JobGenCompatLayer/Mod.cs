using System;
using UnityModManagerNet;

namespace JobGenCompatLayer
{
    public static class Mod
    {
        public static UnityModManager.ModEntry Entry { get; private set; }
        public static Settings Settings { get; private set; }

        static void OnLoad(UnityModManager.ModEntry loadedEntry)
        {
            Entry = loadedEntry;

            if (!Entry.Enabled) { return; }

            try { Settings = Settings.Load<Settings>(Entry); }
            catch (Exception ex)
            {
                Debug.LogWarning(() => $"Using default mod settings. Saved settings failed to load:\n{ex}");
                Settings = new Settings();
            }
        }
    }
}
