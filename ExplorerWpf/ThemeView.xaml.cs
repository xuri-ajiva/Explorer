using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExplorerWpf {
    /// <summary>
    /// Interaktionslogik für ThemeView.xaml
    /// </summary>
    public partial class ThemeView : UserControl, IPage {
        public ThemeView() {
            InitializeComponent();
            OnReFocus();
        }

        #region Implementation of IDisposable

        /// <inheritdoc />
        public void Dispose() { }

        #endregion

        #region Implementation of IPage

        /// <inheritdoc />
        public bool ShowTreeView => false;

        /// <inheritdoc />
        public bool HideConsole => false;

        /// <inheritdoc />
        public bool HideNavigation => true;

        /// <inheritdoc />
        public bool HideExplorerNavigation => true;

        /// <inheritdoc />
        public TabItem ParentTapItem { get; set; }

        /// <inheritdoc />
        public void OnReFocus() {
            this.ClrPeckerBackground.SelectedColor          = ( (SolidColorBrush) Application.Current.Resources["DefBack"] ).Color;
            this.ClrPeckerBorder.SelectedColor              = ( (SolidColorBrush) Application.Current.Resources["Border"] ).Color;
            this.ClrPeckerWindowBorder.SelectedColor        = ( (SolidColorBrush) Application.Current.Resources["WindowBorder"] ).Color;
            this.ClrPeckerScrollBarBackground.SelectedColor = ( (SolidColorBrush) Application.Current.Resources["ScrollBarBackground"] ).Color;
            this.ClrHeaderOver.SelectedColor                = (Color) Application.Current.Resources["ControlLightColor"];
            this.ClrPeckerSelected.SelectedColor            = (Color) Application.Current.Resources["SelectedBackgroundColor"];
            this.ClrExpander.SelectedColor                  = (Color) Application.Current.Resources["GlyphColor"];

            var foreG = (LinearGradientBrush) Application.Current.Resources["Foreground"];

            this.ClrPeckerForegroundGrad1.SelectedColor = foreG.GradientStops[0].Color;
            this.ClrPeckerForegroundGrad2.SelectedColor = foreG.GradientStops[1].Color;

            var backG = (LinearGradientBrush) Application.Current.Resources["Background"];

            this.ClrPeckerBackgroundGrad1.SelectedColor = backG.GradientStops[0].Color;
            this.ClrPeckerBackgroundGrad2.SelectedColor = backG.GradientStops[1].Color;

            this.TxtHeader.Foreground   = new SolidColorBrush( ClrHeaderOver.SelectedColor.Value );
            this.TxtSelected.Foreground = new SolidColorBrush( ClrPeckerSelected.SelectedColor.Value );
            this.TxtExpander.Foreground = new SolidColorBrush( ClrExpander.SelectedColor.Value );
        }

        #endregion

        private void ClrPcker_Background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( e.NewValue.HasValue ) {
                Application.Current.Resources["DefBack"] = new SolidColorBrush( e.NewValue.Value );
            }
        }

        private void ClrPcker_WindowBorder_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( e.NewValue.HasValue ) {
                Application.Current.Resources["WindowBorder"] = new SolidColorBrush( e.NewValue.Value );
            }
        }

        private void ClrPcker_Border_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( e.NewValue.HasValue ) {
                Application.Current.Resources["Border"] = new SolidColorBrush( e.NewValue.Value );
                Application.Current.Resources["Accent"] = new SolidColorBrush( e.NewValue.Value );
            }
        }

        private void ClrPcker_ScrollBarBackground_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( e.NewValue.HasValue ) {
                Application.Current.Resources["ScrollBarBackground"] = new SolidColorBrush( e.NewValue.Value );
            }
        }

        private void ClrPcker_ForegroundGRAD_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( this.ClrPeckerForegroundGrad1.SelectedColor.HasValue && this.ClrPeckerForegroundGrad2.SelectedColor.HasValue )
                Application.Current.Resources["Foreground"] = new LinearGradientBrush( this.ClrPeckerForegroundGrad1.SelectedColor.Value, this.ClrPeckerForegroundGrad2.SelectedColor.Value, new Point( 0, 0 ), new Point( 1, 0 ) );
        }

        private void ClrPcker_BackgroundGRAD_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( this.ClrPeckerBackgroundGrad1.SelectedColor.HasValue && this.ClrPeckerBackgroundGrad2.SelectedColor.HasValue ) {
                var v1 = this.ClrPeckerBackgroundGrad1.SelectedColor.Value;
                var v2 = this.ClrPeckerBackgroundGrad2.SelectedColor.Value;
                Application.Current.Resources["Background"] = new LinearGradientBrush( v1, v2, new Point( 0, 0 ), new Point( 1, 0 ) );
                Application.Current.Resources["BackgroundLight"] = new LinearGradientBrush( Color.Subtract( v1, Color.FromArgb( SettingsHandler.ConTransSub,1,1,0 ) ), Color.Subtract( v2, Color.FromArgb( SettingsHandler.ConTransSub, 1,1,1) ), new Point( 0, 0 ), new Point( 1, 0 ) );
            }
        }

        private void ClrPcker_Header_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( e.NewValue.HasValue ) {
                Application.Current.Resources["ControlLightColor"]              = e.NewValue.Value;
                Application.Current.Resources["ControlMediumColor"]             = e.NewValue.Value;
                Application.Current.Resources["DynamicResourceControlBrushKey"] = e.NewValue.Value;
                Application.Current.Resources["ControlMouseOverColor"]          = e.NewValue.Value;

                //Application.Current.Resources["GlyphColor"]                     = e.NewValue.Value; treeView expander

                //Application.Current.Resources["BackgroundLight"] = new LinearGradientBrush( Colors.Transparent, Color.Add( e.NewValue.Value, Color.FromArgb( 255, 30, 30, 03 ) ), new Point( .5, 0 ), new Point( .5, 1 ) );

                this.TxtHeader.Foreground = new SolidColorBrush( e.NewValue.Value );
            }
        }

        private void ClrPcker_Selected_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( e.NewValue.HasValue ) {
                Application.Current.Resources["SelectedBackgroundColor"] = e.NewValue.Value;
                Application.Current.Resources["SelectedUnfocusedColor"]  = e.NewValue.Value;
                //Application.Current.Resources["DynamicResourceControlBrushKey"] = new SolidColorBrush( e.NewValue.Value );

                this.TxtSelected.Foreground = new SolidColorBrush( e.NewValue.Value );
            }
        }

        private void ClrPcker_Expander_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e) {
            if ( e.NewValue.HasValue ) {
                Application.Current.Resources["GlyphColor"] = e.NewValue.Value;
                //Application.Current.Resources["DynamicResourceControlBrushKey"] = new SolidColorBrush( e.NewValue.Value );

                this.TxtExpander.Foreground = new SolidColorBrush( e.NewValue.Value );
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { System.Diagnostics.Process.Start( "https://github.com/xceedsoftware/wpftoolkit/blob/master/license.md" ); }

        private void Save_Click(object sender, RoutedEventArgs e) { SettingsHandler.SaveCurrentColor(); }

        private void Load_Click(object sender, RoutedEventArgs e) { SettingsHandler.LoadCurrentColor(); }
    }
}
