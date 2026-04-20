using System;

namespace TS.Sdl.Video
{
    [Flags]
    public enum WindowFlags : ulong
    {
        None = 0,
        Fullscreen = 0x0000000000000001,
        Hidden = 0x0000000000000008,
        Borderless = 0x0000000000000010,
        Resizable = 0x0000000000000020,
        HighPixelDensity = 0x0000000000002000,
        AlwaysOnTop = 0x0000000000010000,
        Vulkan = 0x0000000010000000,
        Metal = 0x0000000020000000,
        Transparent = 0x0000000040000000,
        NotFocusable = 0x0000000080000000
    }
}

