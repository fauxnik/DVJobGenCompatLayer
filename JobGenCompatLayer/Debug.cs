using CommandTerminal;
using System;

namespace JobGenCompatLayer
{
    internal static class Debug
    {
        public static void LogDebug(System.Func<object> messageFactory)
        {
            if (Mod.Settings.selectedLogLevel > LogLevel.Debug) { return; }


        }

        public static void Log(System.Func<object> messageFactory)
        {
            if (Mod.Settings.selectedLogLevel > LogLevel.Info) { return; }

            var message = messageFactory();
            if (message is string) { Mod.Entry.Logger.Log(message as string); }
            else
            {
                Mod.Entry.Logger.Log("Logging object via UnityEngine.Debug...");
                UnityEngine.Debug.Log(message);
            }
        }

        public static void LogWarning(System.Func<object> messageFactory)
        {
            if (Mod.Settings.selectedLogLevel > LogLevel.Warning) { return; }

            var message = messageFactory();
            if (message is string) { Mod.Entry.Logger.Warning(message as string); }
            else
            {
                Mod.Entry.Logger.Warning("Logging object via UnityEngine.Debug...");
                UnityEngine.Debug.LogWarning(message);
            }
        }

        public static void LogError(System.Func<object> messageFactory)
        {
            if (Mod.Settings.selectedLogLevel > LogLevel.Error) { return; }

            var message = messageFactory();
            if (message is string) { Mod.Entry.Logger.Error(message as string); }
            else
            {
                Mod.Entry.Logger.Error("Logging object via UnityEngine.Debug...");
                UnityEngine.Debug.LogError(message);
            }
        }

        public static void OnCriticalFailure(Exception exception, string action)
        {
            // TODO: show floaty message (and offer to open log folder?) before quitting the game
            UnityEngine.Debug.LogError(exception);
#if DEBUG
#else
            Mod.Entry.Enabled = false;
            Mod.Entry.Logger.Critical($"Deactivating mod {Mod.Entry.Info.Id} due to unrecoverable failure!");
#endif
            Mod.Entry.Logger.Critical($"This happened while {action}.");
            Mod.Entry.Logger.Critical($"You can reactivate {Mod.Entry.Info.Id} after restarting the game, but this failure type likely indicates an incompatibility between the mod and a recent game update. Please search the mod's Github issue tracker for a relevant report. If none is found, please open one. Include this log file and a detailed description of what you were doing when this error occurred.");
            UnityEngine.Application.Quit();
        }

        internal static class Commands
        {
            internal static void Register()
            {
                // TODO: register terminal commands
                // Terminal.Shell.AddCommand("JGCL.CommandName", CommandMethod, min_args: 0, max_args: 0);
            }

            // private static void CommandMethod(CommandArg[] args) { }
        }
    }
}
