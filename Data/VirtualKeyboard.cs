using System.Runtime.InteropServices;

namespace AppClient.Data;

public class VirtualKeyboard
{
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    private const uint SPI_SETINPUTMETHOD = 0x0085;

    public static void ShowTouchKeyboard()
    {
        SystemParametersInfo(SPI_SETINPUTMETHOD, 0, IntPtr.Zero, 0);
    }
}