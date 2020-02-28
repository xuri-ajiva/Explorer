#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

#endregion

namespace ExplorerWpf {
    public static class SettingsHandler {
        private static CurrentSettings current;

        public const string ROOT_FOLDER = CurrentSettings.S_ROOT_FOLDER;

        public static bool ConsoleAutoChangeDisc { get => current.SConsoleAutoChangeDisc; set => current.SConsoleAutoChangeDisc = value; }

        public static bool ConsoleAutoChangePath { get => current.SConsoleAutoChangePath; set => current.SConsoleAutoChangePath = value; }

        public static bool ConsolePresent { get => current.SConsolePresent; set => current.SConsolePresent = value; }

        public static string ParentDirectoryPrefix { get => current.SParentDirectoryPrefix; set => current.SParentDirectoryPrefix = value; }

        public static List<string> ExtenstionWithSpecialIcons { get => current.SExtenstionWithSpecialIcons; set => current.SExtenstionWithSpecialIcons = value; }
        public static double       TreeViewWith = 105;

        public static string InitCall = Init();

        static SettingsHandler() { }

        private static string Init() {
            current = new CurrentSettings( true );
            // = new CurrentSettings {
            //ConsoleAutoChangeDisc = true,
            //ConsoleAutoChangePath = true,
            //ConsolePresent        = false,
            //ParentDirectoryPrefix = "⤴ ",
            //};
            // ExtenstionWithSpecialIcons = new List<string> { ".exe", ".lnk", ".url" };

            LoadCurrentState();

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

        #region NativeMethods

        public struct NativeMethods {

            #region ContextMenu

            // Retrieves the IShellFolder interface for the desktop folder, which is the root of the Shell's namespace.
            [DllImport( "shell32.dll" )]
            internal static extern int SHGetDesktopFolder(out IntPtr ppshf);

            // Takes a STRRET structure returned by IShellFolder::GetDisplayNameOf, converts it to a string, and places the result in a buffer. 
            [DllImport( "shlwapi.dll", EntryPoint = "StrRetToBuf", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true )]
            internal static extern int StrRetToBuf(IntPtr pstr, IntPtr pidl, StringBuilder pszBuf, int cchBuf);

            // The CreatePopupMenu function creates a drop-down menu, submenu, or shortcut menu. The menu is initially empty. You can insert or append menu items by using the InsertMenuItem function. You can also use the InsertMenu function to insert menu items and the AppendMenu function to append menu items.
            [DllImport( "user32", SetLastError = true, CharSet = CharSet.Auto )]
            internal static extern IntPtr CreatePopupMenu();

            // The DestroyMenu function destroys the specified menu and frees any memory that the menu occupies.
            [DllImport( "user32", SetLastError = true, CharSet = CharSet.Auto )]
            internal static extern bool DestroyMenu(IntPtr hMenu);

            // Determines the default menu item on the specified menu
            [DllImport( "user32", SetLastError = true, CharSet = CharSet.Auto )]
            internal static extern int GetMenuDefaultItem(IntPtr hMenu, bool fByPos, uint gmdiFlags);

            #endregion

            [DllImport( "user32.dll" )] internal static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport( "kernel32" )] internal static extern bool AllocConsole();

            [DllImport( "kernel32.dll", SetLastError = true )]
            internal static extern bool AttachConsole(uint dwProcessId);

            [DllImport( "kernel32.dll" )] internal static extern IntPtr GetConsoleWindow();

            [DllImport( "kernel32.dll", SetLastError = true, ExactSpelling = true )]
            internal static extern bool FreeConsole();

            [DllImport( "user32.dll", EntryPoint = "SetLayeredWindowAttributes" )]
            internal static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

            [DllImport( "user32.dll" )] internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            [DllImport( "user32.dll", SetLastError = true )]
            internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


            [DllImport( "USER32.DLL" )] internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            [DllImport( "user32.dll" )] internal static extern bool DrawMenuBar(IntPtr hWnd);

            [DllImport( "user32.dll", EntryPoint = "SetWindowPos" )]
            internal static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

            [DllImport( "user32.dll" )] internal static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

            [DllImport( "user32.dll", SetLastError = true )]
            internal static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

            [DllImport( "user32", ExactSpelling = true, SetLastError = true )]
            internal static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs( UnmanagedType.U4 )]
                int cPoints);

            [DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
            internal static extern IntPtr GetDesktopWindow();

            [StructLayout( LayoutKind.Sequential )]
            internal struct RECT {
                public int left, top, bottom, right;
            }

            internal const int GWL_STYLE      = -16;        //hex constant for style changing
            internal const int WS_BORDER      = 0x00800000; //window with border
            internal const int WS_CAPTION     = 0x00C00000; //window with a title bar
            internal const int WS_SYSMENU     = 0x00080000; //window with no borders etc.
            internal const int WS_MINIMIZEBOX = 0x00020000; //window with minimizebox
            internal const int SW_HIDE        = 0;
            internal const int SW_SHOWNORMAL  = 1;
            internal const int SW_RESTORE     = 9;
        }

        #endregion

        public static void SaveCurrentState() {
            XmlSerializer xr = new XmlSerializer( typeof(CurrentSettings) );

            using ( var fs = File.Open( CurrentSettings.S_SETTINGS_FILE, FileMode.Create ) ) {
                xr.Serialize( fs, current );
            }
        }

        public static void LoadCurrentState() {
            if ( !File.Exists( CurrentSettings.S_SETTINGS_FILE ) ) return;

            try {
                var xr = new XmlSerializer( typeof(CurrentSettings) );

                using ( var fs = File.Open( CurrentSettings.S_SETTINGS_FILE, FileMode.Open ) ) {
                    var tmp = (CurrentSettings) xr.Deserialize( fs );

                    if ( tmp.Equals( new CurrentSettings() ) ) {
                        current = new CurrentSettings( true );
                    }

                    current = tmp;
                }
            } catch (Exception e) {
                OnError( e );
                current = new CurrentSettings( true );
                SaveCurrentState();
            }
        }

        [Serializable]
        public struct CurrentSettings {
            public bool   SConsoleAutoChangeDisc;
            public bool   SConsoleAutoChangePath;
            public bool   SConsolePresent;
            public string SParentDirectoryPrefix;

            public       List<string> SExtenstionWithSpecialIcons;
            public const string       S_ROOT_FOLDER   = "/";
            public const string       S_SETTINGS_FILE = "settimgs.xmlx";

            public CurrentSettings(bool defaultInit) {
                this.SConsoleAutoChangeDisc      = true;
                this.SConsoleAutoChangePath      = true;
                this.SConsolePresent             = false;
                this.SParentDirectoryPrefix      = "⤴ ";
                this.SExtenstionWithSpecialIcons = new List<string> { ".exe", ".lnk", ".url" };
            }
        }
    }
}
