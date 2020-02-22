#region using

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace ExplorerWpf {
    public static class SettingsHandler {
        public const string ROOT_FOLDER = "/";

        public static bool ConsoleAutoChangeDisc = true;
        public static bool ConsoleAutoChangePath = true;
        public static bool ConsolePresent        = true;


        public static string ParentDirectoryPrefix = "⤴ ";

        public static List<string> ExtenstionWithSpecialIcons;

        public static string InitCall = Init();

        private static string Init() {
            ExtenstionWithSpecialIcons = new List<string>();
            ExtenstionWithSpecialIcons.Add( ".exe" );
            ExtenstionWithSpecialIcons.Add( ".lnk" );
            ExtenstionWithSpecialIcons.Add( ".url" );

            return "supersede";
        }

        public static void OnError(Exception ex) {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine( "Module: "                                  + ex.Source );
            Console.WriteLine( "Message: "                                 + ex.Message );
            Console.WriteLine( "HResult: " + ex.HResult + "    HelpLink: " + ex.HelpLink );
            //Console.WriteLine( "StackTrace: " + ex.StackTrace );
            Console.ResetColor();
        }

        #region DllImport

        public struct DllImport {
            [DllImport( "user32.dll" )] public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport( "kernel32" )] public static extern bool AllocConsole();

            [DllImport( "kernel32.dll", SetLastError = true )]
            public static extern bool AttachConsole(uint dwProcessId);

            [DllImport( "kernel32.dll" )] public static extern IntPtr GetConsoleWindow();

            [DllImport( "kernel32.dll", SetLastError = true, ExactSpelling = true )]
            public static extern bool FreeConsole();

            [DllImport( "user32.dll", EntryPoint = "SetLayeredWindowAttributes" )]
            public static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

            [DllImport( "user32.dll" )] public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            [DllImport( "user32.dll", SetLastError = true )]
            public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


            [DllImport( "USER32.DLL" )] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            [DllImport( "user32.dll" )] public static extern bool DrawMenuBar(IntPtr hWnd);

            [DllImport( "user32.dll", EntryPoint = "SetWindowPos" )]
            public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

            [DllImport( "user32.dll" )] public static extern
                bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

            [DllImport( "user32.dll", SetLastError = true )]
            public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

            [DllImport( "user32", ExactSpelling = true, SetLastError = true )]
            public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs( UnmanagedType.U4 )]
                int cPoints);

            [DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
            public static extern IntPtr GetDesktopWindow();

            [StructLayout( LayoutKind.Sequential )]
            public struct RECT {
                public int left, top, bottom, right;
            }

            public const int GWL_STYLE      = -16;        //hex constant for style changing
            public const int WS_BORDER      = 0x00800000; //window with border
            public const int WS_CAPTION     = 0x00C00000; //window with a title bar
            public const int WS_SYSMENU     = 0x00080000; //window with no borders etc.
            public const int WS_MINIMIZEBOX = 0x00020000; //window with minimizebox
            public const int SW_HIDE        = 0;
            public const int SW_SHOWNORMAL  = 1;
            public const int SW_RESTORE     = 9;
        }

        #endregion

    }
}
