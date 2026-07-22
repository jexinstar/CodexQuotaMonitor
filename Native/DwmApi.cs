using System;
using System.Runtime.InteropServices;

namespace CodexQuotaMonitor.Native
{
    internal static class DwmApi
    {
        internal const int DWMWA_NCRENDERING_ENABLED = 1;
        internal const int DWMWA_NCRENDERING_POLICY = 2;
        internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        internal const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        internal const int DWMWA_BORDER_COLOR = 34;
        internal const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        internal const int DWMNCRP_ENABLED = 2;
        internal const int DWMWCP_ROUND = 2;
        internal const int DWMSBT_TRANSIENTWINDOW = 3;
        internal const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        internal static extern int DwmSetWindowAttribute(
            IntPtr windowHandle,
            int attribute,
            ref int attributeValue,
            int attributeSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        internal static extern int DwmGetWindowAttribute(
            IntPtr windowHandle,
            int attribute,
            out int attributeValue,
            int attributeSize);
    }
}
