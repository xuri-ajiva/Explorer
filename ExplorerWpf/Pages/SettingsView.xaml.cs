#region using

using System.Windows;
using System.Windows.Controls;

#endregion

namespace ExplorerWpf.Pages {
    /// <summary>
    ///     Interaktionslogik für SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl, IPage {

        public SettingsView() {
            InitializeComponent();
            UpdateToUi();
        }


        #region Implementation of IDisposable

        /// <inheritdoc />
        public void Dispose() { }

        #endregion

        private void UpdateToUi() {
            this.PerformanceMode.IsChecked       = SettingsHandler.PerformanceMode;
            this.ConsoleAutoChangeDisc.IsChecked = SettingsHandler.ConsoleAutoChangeDisc;
            this.ConsoleAutoChangePath.IsChecked = SettingsHandler.ConsoleAutoChangeDisc;
            this.ConsolePresent.IsChecked        = SettingsHandler.ConsolePresent;
            this.UserPowerShell.IsChecked        = SettingsHandler.ChangeUserPowerShell;
            this.ExecuteInNewProcess.IsChecked   = SettingsHandler.ExecuteInNewProcess;
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

        private void ExecuteInNewProcess_OnClick(object sender, RoutedEventArgs e) {
            var isChecked = this.ExecuteInNewProcess.IsChecked;

            if ( isChecked != null ) SettingsHandler.ExecuteInNewProcess = isChecked.Value;
        }

        private void UserPowerShell_OnClick(object sender, RoutedEventArgs e) {
            var isChecked = this.UserPowerShell.IsChecked;

            if ( isChecked != null ) SettingsHandler.ChangeUserPowerShell = isChecked.Value;
        }

        private void PerformanceMode_OnClick(object sender, RoutedEventArgs e) {
            var isChecked = this.PerformanceMode.IsChecked;

            if ( isChecked != null ) SettingsHandler.PerformanceMode = isChecked.Value;
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
        public bool HideExplorerNavigation => true;

        /// <inheritdoc />
        public TabItem ParentTapItem { get; set; }

        /// <inheritdoc />
        public void OnReFocus() { UpdateToUi(); }

        #endregion

    }
}
