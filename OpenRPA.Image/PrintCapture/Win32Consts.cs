using System.ComponentModel;

namespace OpenRPA.Image.PrintCapture
{
    public sealed class Win32Consts
    {
        public enum DibColorMode : uint
        {
            DIB_RGB_COLORS = 0x00,
            DIB_PAL_COLORS = 0x01,
            DIB_PAL_INDICES = 0x02
        }

        public enum BitmapCompressionMode : uint
        {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }

        public enum RasterOperationMode : uint
        {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062,
            CAPTUREBLT = 0x40000000 //only if WinVer >= 5.0.0 (see wingdi.h)
        }

        public enum PrintWindowMode : uint
        {
            [Description(
                "Only the client area of the window is copied to hdcBlt. By default, the entire window is copied.")]
            PW_CLIENTONLY = 0x00000001,

            [Description("works on windows that use DirectX or DirectComposition")]
            PW_RENDERFULLCONTENT = 0x00000002
        }
    }
}
