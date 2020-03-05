#region using

using System;
using System.Windows.Controls;

#endregion

namespace ExplorerWpf.Pages {
    public interface IPage : IDisposable {
        bool    ShowTreeView           { get; }
        bool    HideConsole            { get; }
        bool    HideNavigation         { get; }
        bool    HideExplorerNavigation { get; }
        TabItem ParentTapItem          { get; set; }

        void OnReFocus();
    }
    internal class EmptyPage : IPage {

        #region IDisposable

        /// <inheritdoc />
        public void Dispose() { }

        #endregion

        #region Implementation of IPage

        /// <inheritdoc />
        public bool ShowTreeView => false;

        /// <inheritdoc />
        public bool HideConsole => false;

        /// <inheritdoc />
        public bool HideNavigation => false;

        /// <inheritdoc />
        public bool HideExplorerNavigation => true;

        /// <inheritdoc />
        public TabItem ParentTapItem { get; set; }

        /// <inheritdoc />
        public void OnReFocus() { }

        #endregion

    }
}
