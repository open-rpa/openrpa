// BSD 2-Clause License

// Copyright(c) 2017, Arvie Delgado
// All rights reserved.

// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:

// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.

// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;

namespace OpenRPA.Input
{
    public enum KeyboardKey : Int16
    {
        None = 0x00,

        //LeftButton = 0x01,

        //RightButton = 0x02,

        Break = 0x03 + 0x100,

        //MiddleButton = 0x04,

        //XButton1 = 0x05,

        //XButton2 = 0x06,

        //RESERVED_07 = 0x07,

        Back = 0x08,

        Tab = 0x09,

        //RESERVED_0A = 0x0A,

        //RESERVED_0B = 0x0B,

        //Clear = 0x0C,

        Enter = 0x0D,

        NumpadEnter = 0x0D + 0x100,

        //RESERVED_0E = 0x0E,

        //RESERVED_0F = 0x0F,

        //Shift = 0x10,

        //Ctrl = 0x11,

        //Alt = 0x12,

        Pause = 0x13,

        //CapsLock = 0x14,

        //Kana__Hangul = 0x15,

        //RESERVED_16 = 0x16,

        //Junja = 0x17,

        //Final = 0x18,

        //Hanja__Kanji = 0x19,

        //RESERVED_1A = 0x1A,

        Escape = 0x1B,

        //Convert = 0x1C,

        //NonConvert = 0x1D,

        //Accept = 0x1E,

        //ModeChange = 0x1F,

        Space = 0x20,

        PageUp = 0x21 + 0x100,

        NumpadPageUp = 0x21,

        PageDown = 0x22 + 0x100,

        NumpadPageDown = 0x22,

        End = 0x23 + 0x100,

        NumpadEnd = 0x23,

        Home = 0x24 + 0x100,

        NumpadHome = 0x24,

        Left = 0x25 + 0x100,

        NumpadLeft = 0x25,

        Up = 0x26 + 0x100,

        NumpadUp = 0x26,

        Right = 0x27 + 0x100,

        NumpadRight = 0x27,

        Down = 0x28 + 0x100,

        NumpadDown = 0x28,

        //Select = 0x29,

        //Print = 0x2A,

        //Execute = 0x2B,

        PrintScreen = 0x2C + 0x100,

        Insert = 0x2D + 0x100,

        NumpadInsert = 0x2D,

        Delete = 0x2E + 0x100,

        NumpadDelete = 0x2E,

        //Help = 0x2F,

        D0 = 0x30,

        D1 = 0x31,

        D2 = 0x32,

        D3 = 0x33,

        D4 = 0x34,

        D5 = 0x35,

        D6 = 0x36,

        D7 = 0x37,

        D8 = 0x38,

        D9 = 0x39,

        //RESERVED_3A = 0x3A,

        //RESERVED_3B = 0x3B,

        //RESERVED_3C = 0x3C,

        //RESERVED_3D = 0x3D,

        //RESERVED_3E = 0x3E,

        //RESERVED_3F = 0x3F,

        //RESERVED_40 = 0x40,

        A = 0x41,

        B = 0x42,

        C = 0x43,

        D = 0x44,

        E = 0x45,

        F = 0x46,

        G = 0x47,

        H = 0x48,

        I = 0x49,

        J = 0x4A,

        K = 0x4B,

        L = 0x4C,

        M = 0x4D,

        N = 0x4E,

        O = 0x4F,

        P = 0x50,

        Q = 0x51,

        R = 0x52,

        S = 0x53,

        T = 0x54,

        U = 0x55,

        V = 0x56,

        W = 0x57,

        X = 0x58,

        Y = 0x59,

        Z = 0x5A,

        LeftWin = 0x5B + 0x100,

        RightWin = 0x5C + 0x100,

        Menu = 0x5D + 0x100,

        //RESERVED_5E = 0x5E,

        Sleep = 0x5F,

        Numpad0 = 0x60,

        Numpad1 = 0x61,

        Numpad2 = 0x62,

        Numpad3 = 0x63,

        Numpad4 = 0x64,

        Numpad5 = 0x65,

        Numpad6 = 0x66,

        Numpad7 = 0x67,

        Numpad8 = 0x68,

        Numpad9 = 0x69,

        NumpadMultiply = 0x6A,

        NumpadAdd = 0x6B,

        NumpadSeparator = 0x6C,

        NumpadSubtract = 0x6D,

        NumpadDecimal = 0x6E,

        NumpadDivide = 0x6F + 0x100,

        F1 = 0x70,


        F2 = 0x71,

        F3 = 0x72,

        F4 = 0x73,

        F5 = 0x74,

        F6 = 0x75,

        F7 = 0x76,

        F8 = 0x77,

        F9 = 0x78,

        F10 = 0x79,

        F11 = 0x7A,

        F12 = 0x7B,

        //F13 = 0x7C,

        //F14 = 0x7D,

        //F15 = 0x7E,

        //F16 = 0x7F,

        //F17 = 0x80,

        //F18 = 0x81,

        //F19 = 0x82,

        //F20 = 0x83,

        //F21 = 0x84,

        //F22 = 0x85,


        //F23 = 0x86,

        //F24 = 0x87,

        //RESERVED_88 = 0x88,

        //RESERVED_89 = 0x89,

        //RESERVED_8A = 0x8A,

        //RESERVED_8B = 0x8B,

        //RESERVED_8C = 0x8C,

        //RESERVED_8D = 0x8D,

        //RESERVED_8E = 0x8E,

        //RESERVED_8F = 0x8F,

        //NumLock = 0x90,

        //ScrollLock = 0x91,

        //OEM_FJ_JISHO = 0x92,

        //OEM_FJ_MASSHOU = 0x93,

        //OEM_FJ_TOUROKU = 0x94,

        //OEM_FJ_LOYA = 0x95,

        //OEM_FJ_ROYA = 0x96,

        //RESERVED_97 = 0x97,

        //RESERVED_98 = 0x98,

        //RESERVED_99 = 0x99,

        //RESERVED_9A = 0x9A,

        //RESERVED_9B = 0x9B,

        //RESERVED_9C = 0x9C,

        //RESERVED_9D = 0x9D,

        //RESERVED_9E = 0x9E,

        //RESERVED_9F = 0x9F,

        LeftShift = 0xA0,

        RightShift = 0xA1 + 0x100,


        LeftCtrl = 0xA2,


        RightCtrl = 0xA3 + 0x100,

        LeftAlt = 0xA4,

        RightAlt = 0xA5 + 0x100,

        //BROWSER_BACK = 0xA6,

        //BROWSER_FORWARD = 0xA7,

        //BROWSER_REFRESH = 0xA8,

        //BROWSER_STOP = 0xA9,

        //BROWSER_SEARCH = 0xAA,

        //BROWSER_FAVORITES = 0xAB,

        //BROWSER_HOME = 0xAC,

        //VOLUME_MUTE = 0xAD,

        //VOLUME_DOWN = 0xAE,

        //VOLUME_UP = 0xAF,

        //MEDIA_NEXT_TRACK = 0xB0,

        //MEDIA_PREV_TRACK = 0xB1,

        //MEDIA_STOP = 0xB2,

        //MEDIA_PLAY_PAUSE = 0xB3,

        //LAUNCH_MAIL = 0xB4,

        //LAUNCH_MEDIA_SELECT = 0xB5,

        //LAUNCH_APP1 = 0xB6,

        //LAUNCH_APP2 = 0xB7,

        //RESERVED_B8 = 0xB8,

        //RESERVED_B9 = 0xB9,

        OEM_1_Colons = 0xBA,

        OEM_Plus = 0xBB,

        OEM_Comma = 0xBC,

        OEM_Minus = 0xBD,

        OEM_Period = 0xBE,

        OEM_2_Slash = 0xBF,

        OEM_3_Tilde = 0xC0,

        //RESERVED_C1 = 0xC1,

        //RESERVED_C2 = 0xC2,

        //RESERVED_C3 = 0xC3,

        //RESERVED_C4 = 0xC4,

        //RESERVED_C5 = 0xC5,

        //RESERVED_C6 = 0xC6,

        //RESERVED_C7 = 0xC7,

        //RESERVED_C8 = 0xC8,

        //RESERVED_C9 = 0xC9,

        //RESERVED_CA = 0xCA,

        //RESERVED_CB = 0xCB,

        //RESERVED_CC = 0xCC,

        //RESERVED_CD = 0xCD,

        //RESERVED_CE = 0xCE,

        //RESERVED_CF = 0xCF,

        //RESERVED_D0 = 0xD0,

        //RESERVED_D1 = 0xD1,

        //RESERVED_D2 = 0xD2,

        //RESERVED_D3 = 0xD3,

        //RESERVED_D4 = 0xD4,

        //RESERVED_D5 = 0xD5,

        //RESERVED_D6 = 0xD6,

        //RESERVED_D7 = 0xD7,

        //RESERVED_D8 = 0xD8,

        //RESERVED_D9 = 0xD9,

        //RESERVED_DA = 0xDA,

        OEM_4_OpenBrackets = 0xDB,

        OEM_5_BackSlash = 0xDC,

        OEM_6_CloseBrackets = 0xDD,

        OEM_7_Quotes = 0xDE,
        
        //OEM_8 = 0xDF,
        
        //RESERVED_E0 = 0xE0,
        
        //OEM_AX = 0xE1,
        
        //OEM_102 = 0xE2,
        
        //ICO_HELP = 0xE3,
        
        //ICO_00 = 0xE4,
        
        //PROCESSKEY = 0xE5,
        
        //ICO_CLEAR = 0xE6,
        
        //PACKET = 0xE7,
        
        //RESERVED_E8 = 0xE8,
        
        //OEM_RESET = 0xE9,
        
        //OEM_JUMP = 0xEA,
        
        //OEM_PA1 = 0xEB,
        
        //OEM_PA2 = 0xEC,
        
        //OEM_PA3 = 0xED,
        
        //OEM_WSCTRL = 0xEE,
        
        //OEM_CUSEL = 0xEF,
        
        //OEM_ATTN = 0xF0,
        
        //OEM_FINISH = 0xF1,
        
        //OEM_COPY = 0xF2,
        
        //OEM_AUTO = 0xF3,
        
        //OEM_ENLW = 0xF4,
        
        //OEM_BACKTAB = 0xF5,
        
        //ATTN = 0xF6,
        
        //CRSEL = 0xF7,
        
        //EXSEL = 0xF8,
        
        //EREOF = 0xF9,
        
        //PLAY = 0xFA,
        
        //ZOOM = 0xFB,
        
        //NONAME = 0xFC,
        
        //PA1 = 0xFD,
        
        //OEM_CLEAR = 0xFE,
        
        //RESERVED_FF = 0xFF,
    }
}
