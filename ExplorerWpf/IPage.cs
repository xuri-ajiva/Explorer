using System;
using System.Windows.Controls;

namespace ExplorerWpf {
    public interface IPage : IDisposable {
        bool    ShowTreeView   { get; }
        bool    HideConsole    { get; }
        bool    HideNavigation { get; }
        TabItem ParentTapItem  { get; set; }

        void OnReFocus();
    }
    class EmptyPage : IPage {

        #region Implementation of IPage

        /// <inheritdoc />
        public bool ShowTreeView => false;

        /// <inheritdoc />
        public bool HideConsole => false;

        /// <inheritdoc />
        public bool HideNavigation => false;

        /// <inheritdoc />
        public TabItem ParentTapItem { get; set; }

        /// <inheritdoc />
        public void OnReFocus() { }

        #endregion

        #region IDisposable

        /// <inheritdoc />
        public void Dispose() { }

        #endregion

    }
}
