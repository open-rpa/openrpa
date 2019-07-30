using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenRPA.ViewModel
{
    public class MenuItemViewModel : BaseViewModel
    {
        #region Properties

        public string Header { get; set; }
        public bool IsCheckable { get; set; }
        public List<MenuItemViewModel> Items { get; private set; }
        public ICommand Command { get; private set; }

        #region IsChecked
        private bool _IsChecked;
        public bool IsChecked
        {
            get { return _IsChecked; }
            set
            {
                if (_IsChecked != value)
                {
                    _IsChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }
        #endregion

        #endregion

        public MenuItemViewModel()
        {
            this.Items = new List<MenuItemViewModel>();
        }
    }
}
