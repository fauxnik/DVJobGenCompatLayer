using UnityModManagerNet;

namespace JobGenCompatLayer
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Log level")]
        public LogLevel selectedLogLevel =
#if DEBUG
            LogLevel.Debug;
#else
            LogLevel.Warn;
#endif

        public void OnChange() { }
    }
}
