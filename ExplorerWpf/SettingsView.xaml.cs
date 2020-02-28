using System.Windows;
using System.Windows.Controls;

namespace ExplorerWpf {
    /// <summary>
    /// Interaktionslogik für SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl, IPage {
        public SettingsView() {
            InitializeComponent();
            UpdateToUi();
        }

        private void UpdateToUi() {
            this.ConsoleAutoChangeDisc.IsChecked = SettingsHandler.ConsoleAutoChangeDisc;
            this.ConsoleAutoChangePath.IsChecked = SettingsHandler.ConsoleAutoChangeDisc;
            this.ConsolePresent.IsChecked        = SettingsHandler.ConsolePresent;
        }

        private void Save_Click(object sender, RoutedEventArgs e) { SettingsHandler.SaveCurrentState(); }

        private void Load_Click(object sender, RoutedEventArgs e) { SettingsHandler.LoadCurrentState(); }


        private void ConsoleAutoChangeDisc_Checked(object sender, RoutedEventArgs e) {
            var isChecked = this.ConsoleAutoChangeDisc.IsChecked;

            if ( isChecked != null ) SettingsHandler.ConsoleAutoChangeDisc = isChecked.Value;
        }

        private void ConsoleAutoChangePath_Checked(object sender, RoutedEventArgs e) {
            var isChecked = this.ConsoleAutoChangePath.IsChecked;

            if ( isChecked != null ) SettingsHandler.ConsoleAutoChangePath = isChecked.Value;
        }

        private void ConsolePresent_Checked(object sender, RoutedEventArgs e) {
            var isChecked = this.ConsolePresent.IsChecked;

            if ( isChecked != null ) SettingsHandler.ConsolePresent = isChecked.Value;
        }

        private void SettingsView_OnLoaded(object sender, RoutedEventArgs e) { }

        #region Implementation of IPage

        /// <inheritdoc />
        public bool ShowTreeView => false;

        /// <inheritdoc />
        public bool HideConsole => false;

        /// <inheritdoc />
        public bool HideNavigation => true;

        /// <inheritdoc />
        public TabItem ParentTapItem { get; set; }

        /// <inheritdoc />
        public void OnReFocus() { UpdateToUi(); }

        #endregion


        #region Implementation of IDisposable

        /// <inheritdoc />
        public void Dispose() { }

        #endregion

    }
}
