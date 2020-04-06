#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

#endregion

namespace ExplorerWpf {
    public static class SettingsHandler {
        public const   string          ROOT_FOLDER = CurrentSettings.S_ROOT_FOLDER;
        private static CurrentSettings _current;
        private static ColorSettings   _color;

        static SettingsHandler() {
            _current = new CurrentSettings( true );
            _color   = new ColorSettings();

            try {
                LoadCurrentState();
            } catch (Exception ex) {
                OnError( ex );
            }

            UserPowerShell = _current.SUserPowerShell;
        }

        public static readonly bool UserPowerShell;

        public static bool   ChangeUserPowerShell  { get => _current.SUserPowerShell;        set => _current.SUserPowerShell = value; }
        public static bool   PerformanceMode       { get => _current.SPerformanceMode;       set => _current.SPerformanceMode = value; }
        public static bool   ExecuteInNewProcess   { get => _current.SExecuteInNewProcess;   set => _current.SExecuteInNewProcess = value; }
        public static bool   ConsoleAutoChangeDisc { get => _current.SConsoleAutoChangeDisc; set => _current.SConsoleAutoChangeDisc = value; }
        public static bool   ConsoleAutoChangePath { get => _current.SConsoleAutoChangePath; set => _current.SConsoleAutoChangePath = value; }
        public static bool   ConsolePresent        { get => _current.SConsolePresent;        set => _current.SConsolePresent = value; }
        public static string ParentDirectoryPrefix { get => _current.SParentDirectoryPrefix; set => _current.SParentDirectoryPrefix = value; }

        public static List<string> ExtenstionWithSpecialIcons { get => _current.SExtenstionWithSpecialIcons; set => _current.SExtenstionWithSpecialIcons = value; }

        public static ColorSettings Color1 {
            [DebuggerStepThrough] get => _color;
        }

        public static byte ConTransSub => 40;

        public static void OnError(Exception ex) {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine( "Module: "                                  + ex.Source );
            Console.WriteLine( "Message: "                                 + ex.Message );
            Console.WriteLine( "HResult: " + ex.HResult + "    HelpLink: " + ex.HelpLink );
            //Console.WriteLine( "StackTrace: " + ex.StackTrace );
            Console.ResetColor();
        }


        public static void SaveCurrentState() {
            var xr = new XmlSerializer( typeof(CurrentSettings) );

            using ( var fs = File.Open( CurrentSettings.S_SETTINGS_FILE, FileMode.Create ) ) {
                xr.Serialize( fs, _current );
            }
        }

        public static void LoadCurrentState() {
            if ( !File.Exists( CurrentSettings.S_SETTINGS_FILE ) ) return;

            try {
                var xr = new XmlSerializer( typeof(CurrentSettings) );

                using ( var fs = File.Open( CurrentSettings.S_SETTINGS_FILE, FileMode.Open ) ) {
                    var tmp = (CurrentSettings) xr.Deserialize( fs );

                    _current = tmp.Equals( new CurrentSettings() ) ? new CurrentSettings( true ) : tmp;
                }
            } catch (Exception e) {
                OnError( e );
                _current = new CurrentSettings( true );
                SaveCurrentState();
            }
        }

        public static void SaveCurrentColor() {
            _color.SyncFromApplication();
            var xr = new XmlSerializer( typeof(ColorSettings) );

            using ( var fs = File.Open( ColorSettings.S_SETTINGS_FILE, FileMode.Create ) ) {
                xr.Serialize( fs, _color );
            }
        }

        public static void LoadCurrentColor() {
            if ( !File.Exists( ColorSettings.S_SETTINGS_FILE ) ) return;

            try {
                var xr = new XmlSerializer( typeof(ColorSettings) );

                using ( var fs = File.Open( ColorSettings.S_SETTINGS_FILE, FileMode.Open ) ) {
                    var tmp = (ColorSettings) xr.Deserialize( fs );

                    _color = /* TODO: check for null tmp.Equals( new ColorSettings() ) ? new ColorSettings( true ) :*/ tmp;
                }
            } catch (Exception e) {
                OnError( e );
                _color = new ColorSettings( true );
                SaveCurrentColor();
            }

            _color.SyncToApplication();
        }

        [Serializable]
        public struct CurrentSettings {
            public bool   SConsoleAutoChangeDisc;
            public bool   SConsoleAutoChangePath;
            public bool   SConsolePresent;
            public string SParentDirectoryPrefix;
            public bool   SExecuteInNewProcess;
            public bool   SUserPowerShell;
            public bool   SPerformanceMode;

            public       List<string> SExtenstionWithSpecialIcons;
            public const string       S_ROOT_FOLDER   = "/";
            public const string       S_SETTINGS_FILE = "settimgs.xmlx";

            public CurrentSettings(bool defaultInit) {
                this.SExtenstionWithSpecialIcons = new List<string> { ".exe", ".lnk", ".url" };
                this.SConsoleAutoChangeDisc      = true;
                this.SConsoleAutoChangePath      = true;
                this.SUserPowerShell             = true;
                this.SConsolePresent             = false;
                this.SExecuteInNewProcess        = false;
                this.SPerformanceMode            = false;
                this.SParentDirectoryPrefix      = "⤴ ";
            }
        }
        [Serializable]
        public struct ColorSettings {
            public const string S_SETTINGS_FILE = "colors.xmlx";

            public XmlColor Background;
            public XmlColor Border;
            public XmlColor WindowBorder;
            public XmlColor ScrollBarBackground;
            public XmlColor HeaderOver;
            public XmlColor Selected;
            public XmlColor Expander;


            public XmlColor ForegroundGrad1;
            public XmlColor ForegroundGrad2;
            public XmlColor BackgroundGrad1;
            public XmlColor BackgroundGrad2;

            public ColorSettings(bool defaultInit) : this() { SyncFromApplication(); }

            public void SyncFromApplication() {
                this.Background          = ( (SolidColorBrush) Application.Current.Resources["DefBack"] ).Color;
                this.Border              = ( (SolidColorBrush) Application.Current.Resources["Border"] ).Color;
                this.WindowBorder        = ( (SolidColorBrush) Application.Current.Resources["WindowBorder"] ).Color;
                this.ScrollBarBackground = ( (SolidColorBrush) Application.Current.Resources["ScrollBarBackground"] ).Color;
                this.HeaderOver          = (Color) Application.Current.Resources["ControlLightColor"];
                this.Selected            = (Color) Application.Current.Resources["SelectedBackgroundColor"];
                this.Expander            = (Color) Application.Current.Resources["GlyphColor"];

                var backG = (LinearGradientBrush) Application.Current.Resources["Background"];
                var foreG = (LinearGradientBrush) Application.Current.Resources["Foreground"];

                this.ForegroundGrad1 = foreG.GradientStops[0].Color;
                this.ForegroundGrad2 = foreG.GradientStops[1].Color;
                this.BackgroundGrad1 = backG.GradientStops[0].Color;
                this.BackgroundGrad2 = backG.GradientStops[1].Color;
            }

            public void SyncToApplication() {
                Application.Current.Resources["DefBack"] = new SolidColorBrush( this.Background );

                Application.Current.Resources["WindowBorder"] = new SolidColorBrush( this.WindowBorder );

                Application.Current.Resources["Border"] = new SolidColorBrush( this.Border );
                Application.Current.Resources["Accent"] = new SolidColorBrush( this.Border );

                Application.Current.Resources["ScrollBarBackground"] = new SolidColorBrush( this.ScrollBarBackground );

                Application.Current.Resources["ControlLightColor"]              = (Color) this.HeaderOver;
                Application.Current.Resources["ControlMediumColor"]             = (Color) this.HeaderOver;
                Application.Current.Resources["DynamicResourceControlBrushKey"] = new SolidColorBrush( this.HeaderOver );
                Application.Current.Resources["ControlMouseOverColor"]          = (Color) this.HeaderOver;

                Application.Current.Resources["SelectedBackgroundColor"] = (Color) this.Selected;
                Application.Current.Resources["SelectedUnfocusedColor"]  = (Color) this.Selected;

                Application.Current.Resources["GlyphColor"] = (Color) this.Expander;

                Application.Current.Resources["Foreground"]                         = new LinearGradientBrush( this.ForegroundGrad1, this.ForegroundGrad2, new Point( 0, 0 ), new Point( 1, 0 ) );
                Application.Current.Resources["DynamicResourceControlTextBrushKey"] = Application.Current.Resources["Foreground"];

                Application.Current.Resources["Background"]      = new LinearGradientBrush( this.BackgroundGrad1,                                                           this.BackgroundGrad2,                                                           new Point( 0, 0 ), new Point( 1, 0 ) );
                Application.Current.Resources["BackgroundLight"] = new LinearGradientBrush( Color.Subtract( this.BackgroundGrad1, Color.FromArgb( ConTransSub, 1, 1, 0 ) ), Color.Subtract( this.BackgroundGrad2, Color.FromArgb( ConTransSub, 1, 1, 1 ) ), new Point( 0, 0 ), new Point( 1, 0 ) );
            }

            [Serializable]
            public struct XmlColor {

                private Color _colorM;

                public XmlColor(Color c) => this._colorM = c;


                public Color ToColor() => this._colorM;

                public void FromColor(Color c) { this._colorM = c; }

                public static implicit operator Color(XmlColor x) => x.ToColor();

                public static implicit operator XmlColor(Color c) => new XmlColor( c );

                [XmlAttribute]
                public string Web { get => this._colorM.ToString(); set => this._colorM = (Color) ColorConverter.ConvertFromString( value ); }
            }
        }
    }

    #region NativeMethods

// ReSharper disable InconsistentNaming
    public struct NativeMethods {

        #region Blur

        [DllImport( "user32.dll" )] private static extern int SetWindowCompositionAttribute(IntPtr hWnd, ref WindowCompositionAttributeData data);

        [StructLayout( LayoutKind.Sequential )]
        private struct WindowCompositionAttributeData {
            public WindowCompositionAttribute Attribute;
            public IntPtr                     Data;
            public int                        SizeOfData;
        }

        private enum WindowCompositionAttribute {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        private enum AccentState {
            // ReSharper disable UnusedMember.Local

            ACCENT_DISABLED                   = 0,
            ACCENT_ENABLE_GRADIENT            = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND          = 3,
            ACCENT_INVALID_STATE              = 4

            // ReSharper restore UnusedMember.Local
        }

        [StructLayout( LayoutKind.Sequential )]
        private struct AccentPolicy {
            public           AccentState AccentState;
            private readonly int         AccentFlags;
            private readonly int         GradientColor;
            private readonly int         AnimationId;
        }

        public static void EnableBlur(IntPtr handle) { SetAccent( handle, AccentState.ACCENT_ENABLE_BLURBEHIND ); }

        public static void DisableBlur(IntPtr handle) { SetAccent( handle, AccentState.ACCENT_DISABLED ); }

        private static void SetAccent(IntPtr handle, AccentState state) {
            var accent           = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf( accent );
            accent.AccentState = state;

            var accentPtr = Marshal.AllocHGlobal( accentStructSize );
            Marshal.StructureToPtr( accent, accentPtr, false );

            var data = new WindowCompositionAttributeData { Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY, SizeOfData = accentStructSize, Data = accentPtr };

            SetWindowCompositionAttribute( handle, ref data );

            Marshal.FreeHGlobal( accentPtr );
        }

        #endregion

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
        internal const int WS_MAXIMIZEBOX = 0x00010000; //window with MAXIMIZEBOX   
        internal const int WS_THICKFRAME  = 0x00040000; //The window has a sizing border. Same as the WS_SIZEBOX style.
        internal const int SW_HIDE        = 0;
        internal const int SW_SHOWNORMAL  = 1;
        internal const int SW_RESTORE     = 9;
    }

    #endregion

}
