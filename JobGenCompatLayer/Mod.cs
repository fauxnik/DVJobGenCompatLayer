using HarmonyLib;
using System;
using System.Reflection;
using UnityModManagerNet;

namespace JobGenCompatLayer
{
    public static class Mod
    {
        public static UnityModManager.ModEntry Entry { get; private set; }
        public static Settings Settings { get; private set; }

        private static Harmony harmony;

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

            try
            {
                harmony = new Harmony(Entry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex) { Debug.OnCriticalFailure(ex, "patching miscellaneous assemblies"); }

            try { Debug.Commands.Register(); }
            catch (Exception ex) { Debug.LogWarning(() => $"Failed to register debug commands:\n{ex}"); }
        }
    }
}
