/*######     Copyright (c) 1997-2012 Ufasoft  http://ufasoft.com  mailto:support@ufasoft.com,  Sergey Pavlov  mailto:dev@ufasoft.com #####
# This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published #
# by the Free Software Foundation; either version 3, or (at your option) any later version. This program is distributed in the hope that #
# it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. #
# See the GNU General Public License for more details. You should have received a copy of the GNU General Public License along with this #
# program; If not, see <http://www.gnu.org/licenses/>                                                                                    #
########################################################################################################################################*/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

using BYTE = System.Byte;
using SHORT = System.Int16;
using WORD = System.UInt16;
using DWORD = System.UInt32;
using LONG = System.Int32;
using HANDLE = System.IntPtr;
using WCHAR = System.Char;

using CHAR = System.Byte;
using BOOL = System.Int32;

namespace Win32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public SHORT X, Y;

        public COORD(short x, short y)
            : this()
        {
            X = x;
            Y = y;
        }

        public LONG ToInt32()
        {
            return (WORD)X | (Y << 16);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMALL_RECT
    {
        public SHORT Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CHAR_INFO
    {
        public char Char;
        public WORD Attributes;
    }

    public struct CONSOLE_CURSOR_INFO
    {
        public DWORD dwSize;
        public bool bVisible;
    }

    public struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public WORD wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    public enum ScreenBufferType
    {
        CONSOLE_TEXTMODE_BUFFER = 1
    }

    public enum ConsoleMode : uint
    {
        ENABLE_ECHO_INPUT = 4,
        ENABLE_INSERT_MODE = 0x20
    }

    public enum EventType : ushort
    {
        KEY_EVENT = 1,
        MOUSE_EVENT = 2,
        WINDOW_BUFFER_SIZE_EVENT = 4,
        MENU_EVENT = 8,
        FOCUS_EVENT = 0x10
    }

    [Flags]
    public enum ControlKeyState : uint
    {
        RIGHT_ALT_PRESSED = 1,
        LEFT_ALT_PRESSED = 2,
        RIGHT_CTRL_PRESSED = 4,
        LEFT_CTRL_PRESSED = 8,
        SHIFT_PRESSED = 0x10,
        NUMLOCK_ON = 0x20,
        SCROLLLOCK_ON = 0x40,
        CAPSLOCK_ON = 0x80,
        ENHANCED_KEY = 0x100
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CharUnion
    {
        [FieldOffset(0)]
        public WCHAR UnicodeChar;

        [FieldOffset(0)]
        public CHAR AsciiChar;
    }

    [StructLayout(LayoutKind.Sequential | LayoutKind.Explicit)]
    public struct KEY_EVENT_RECORD
    {
        [FieldOffset(0)]
        public BOOL bKeyDown;

        [FieldOffset(4)]
        public WORD wRepeatCount;

        [FieldOffset(6)]
        public WORD wVirtualKeyCode;

        [FieldOffset(8)]
        public WORD wVirtualScanCode;

        [FieldOffset(10)]
        public CharUnion uChar;

        [FieldOffset(12)] public ControlKeyState dwControlKeyState;
    }

    public struct MOUSE_EVENT_RECORD
    {
        public COORD dwMousePosition;
        public DWORD dwButtonState, dwControlKeyState, dwEventFlags;
    }

    public struct FOCUS_EVENT_RECORD
    {
        public BOOL bSetFocus;
    }

    public struct MENU_EVENT_RECORD
    {
        public DWORD dwCommandId;
    }

    public struct WINDOW_BUFFER_SIZE_RECORD
    {
        public COORD dwSize;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct EventUnion
    {
        [FieldOffset(0)]
        public KEY_EVENT_RECORD KeyEvent;

        [FieldOffset(0)]
        public MOUSE_EVENT_RECORD MouseEvent;

        [FieldOffset(0)]
        public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;

        [FieldOffset(0)]
        public MENU_EVENT_RECORD MenuEvent;

        [FieldOffset(0)]
        public FOCUS_EVENT_RECORD FocusEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT_RECORD
    {
        public EventType EventType;
        public EventUnion Event;
    }

    public struct CONSOLE_FONT_INFO
    {
        public DWORD nFont;
        public COORD dwFontSize;
    }

    public partial class Api
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool ScrollConsoleScreenBuffer(SafeHandle h, ref SMALL_RECT rect, HANDLE clip, LONG dest, ref CHAR_INFO fill);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleActiveScreenBuffer(SafeHandle h);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCursorPosition(SafeHandle h, COORD coord);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCursorInfo(SafeHandle h, ref CONSOLE_CURSOR_INFO ci);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfo(SafeHandle h, out CONSOLE_SCREEN_BUFFER_INFO sbi);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleTextAttribute(SafeHandle h, WORD attr);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateConsoleScreenBuffer(Rights rights, Share shareMode, HANDLE sec, ScreenBufferType type, HANDLE buf);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(SafeHandle h, out ConsoleMode mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadConsoleOutputCharacter(SafeHandle h, char[] buf, DWORD nLength, COORD coord, out DWORD dwRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadConsoleOutputCharacterA(SafeHandle h, byte[] buf, DWORD nLength, COORD coord, out DWORD dwRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadConsoleOutputAttribute(SafeHandle h, WORD[] buf, DWORD nLength, COORD coord, out DWORD dwRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleOutputCharacterA(SafeHandle h, byte[] buf, DWORD nLength, COORD coord, out DWORD dwRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleOutputCharacter(SafeHandle h, char[] buf, DWORD nLength, COORD coord, out DWORD dwRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleOutputAttribute(SafeHandle h, WORD[] buf, DWORD nLength, COORD coord, out DWORD dwRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleA(SafeHandle h, byte[] buf, DWORD nLength, out DWORD dwRead, HANDLE reserv);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushConsoleInputBuffer(SafeHandle h);

        [DllImport("kernel32.dll")]
        public static extern DWORD GetConsoleOutputCP();

        [DllImport("kernel32.dll")]
        public static extern DWORD GetConsoleCP();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleOutputCP(DWORD cp);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCP(DWORD cp);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadConsoleInput(SafeHandle h, [Out] INPUT_RECORD[] ir, DWORD len, out DWORD dwRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetNumberOfConsoleInputEvents(SafeHandle h, out DWORD dw);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetNumberOfConsoleMouseButtons(out DWORD dwButtons);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetCurrentConsoleFont(SafeHandle h, bool bNaximumWindows, out CONSOLE_FONT_INFO lpConsoleCurrentFont);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwProcessId);

        public static IEnumerable<string> ReadFromBuffer(HANDLE hOutput, short x, short y, short width, short height)
        {
            HANDLE buffer = Marshal.AllocHGlobal(width * height * Marshal.SizeOf(typeof(CHAR_INFO)));
            if (buffer == null)
                throw new OutOfMemoryException();

            try
            {
                COORD coord = new COORD();
                SMALL_RECT rc = new SMALL_RECT();
                rc.Left = x;
                rc.Top = y;
                rc.Right = (short)(x + width - 1);
                rc.Bottom = (short)(y + height - 1);

                COORD size = new COORD();
                size.X = width;
                size.Y = height;

                if (!ReadConsoleOutput(hOutput, buffer, size, coord, ref rc))
                {
                    // 'Not enough storage is available to process this command' may be raised for buffer size > 64K (see ReadConsoleOutput doc.)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                HANDLE ptr = buffer;
                for (int h = 0; h < height; h++)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int w = 0; w < width; w++)
                    {
                        CHAR_INFO ci = (CHAR_INFO)Marshal.PtrToStructure(ptr, typeof(CHAR_INFO));
                        char[] chars = Console.OutputEncoding.GetChars(BitConverter.GetBytes(ci.Char));
                        sb.Append(chars[0]);
                        ptr += Marshal.SizeOf(typeof(CHAR_INFO));
                    }
                    yield return sb.ToString();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool ReadConsoleOutput(HANDLE hOutput, HANDLE buffer, COORD size, COORD coord, ref SMALL_RECT rc)
        {
            string[] colors = {
                "black",
                "darkblue",
                "darkgreen",
                "darkcyan",
                "darkred",
                "darkmagenta",
                "brown",
                "white",
                "lightgrey",
                "blue",
                "green",
                "cyan",
                "red",
                "magenta",
                "yellow",
                "white"
            };

            var _consoleH = new SafeHandleExt(hOutput);
            short _widthConsole = size.X;

            // _rowsList.Length
            for (int i = 0; i < size.Y; i++)
            {
                ushort[] lpAttr = new ushort[_widthConsole];

                COORD _coordReadAttr = new COORD(0, (short)i);

                uint lpReadALast;

                bool readAttr = ReadConsoleOutputAttribute(_consoleH, lpAttr, Convert.ToUInt32(_widthConsole), _coordReadAttr, out lpReadALast);

                if (!readAttr)
                {
                    if (!_consoleH.Release())
                        throw new Exception();

                    return false;
                }

                string[] attrText = new string[_widthConsole];

                for (int _attr = 0; _attr < lpAttr.Length; _attr++)
                {
                    string _text = colors[lpAttr[_attr] & 0x0F];
                    string _background = colors[((lpAttr[_attr] & 0xF0) >> 4) & 0x0F];

                    attrText[_attr] = _text + "|" + _background;
                }
            }

            if (!_consoleH.Release())
                throw new Exception();

            return true;
        }

        public static void Test(Process proc)
        {
            bool resultFree = FreeConsole();

            if (resultFree)
            {
                Console.WriteLine("FreeConsole: {0}", true);
            }
            else
            {
                Console.WriteLine("FreeConsole: {0}", false);
            }

            Console.WriteLine("Process ID: {0}", Convert.ToUInt32(proc.Id));

            bool result = AttachConsole(Convert.ToUInt32(proc.Id));

            Console.WriteLine("AttachConsole: {0}", result);

            SafeHandleExt _consoleH = new SafeHandleExt(GetStdHandle(StdHandleNumber.STD_OUTPUT_HANDLE));

            CONSOLE_SCREEN_BUFFER_INFO _bufferInfo;

            bool getInfo = GetConsoleScreenBufferInfo(_consoleH, out _bufferInfo);

            if (getInfo)
            {
                Console.WriteLine("GetConsoleScreenBufferInfo: {0}x{1}", _bufferInfo.dwSize.X, _bufferInfo.dwSize.Y);
            }
            else
            {
                Console.WriteLine("GetConsoleScreenBufferInfo: {0}", false);
            }

            short _widthConsole = _bufferInfo.dwSize.X;
            short _heightConsole = _bufferInfo.dwSize.Y;

            IEnumerable<string> rows = ReadFromBuffer(_consoleH.DangerousGetHandle(), 0, 0, _widthConsole, _heightConsole);

            foreach (string row in rows)
            {
                Console.WriteLine(row);
            }
        }
    }

    // Introduce this handle to replace internal SafeTokenHandle,
    // which is mainly used to hold Windows thread or process access token
    [SecurityCritical]
    public sealed class SafeHandleExt : SafeHandle
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        private SafeHandleExt()
            : base(IntPtr.Zero, true)
        { }

        // 0 is an Invalid Handle
        public SafeHandleExt(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public static SafeHandleExt InvalidHandle
        {
            [SecurityCritical]
            get { return new SafeHandleExt(IntPtr.Zero); }
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero || handle == new IntPtr(-1); }
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }

        public bool Release()
        {
            return CloseHandle(handle);
        }
    }
}