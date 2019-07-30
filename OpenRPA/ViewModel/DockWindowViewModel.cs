using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.ViewModel
{
    using System.Windows.Input;
    public abstract class DockWindowViewModel : BaseViewModel
    {
        #region Properties

        #region CloseCommand
        private ICommand _CloseCommand;
        public ICommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                    _CloseCommand = new RelayCommand(call => Close());
                return _CloseCommand;
            }
        }
        #endregion

        #region IsClosed
        private bool _IsClosed;
        public bool IsClosed
        {
            get { return _IsClosed; }
            set
            {
                if (_IsClosed != value)
                {
                    _IsClosed = value;
                    OnPropertyChanged(nameof(IsClosed));
                }
            }
        }
        #endregion

        #region CanClose
        private bool _CanClose;
        public bool CanClose
        {
            get { return _CanClose; }
            set
            {
                if (_CanClose != value)
                {
                    _CanClose = value;
                    OnPropertyChanged(nameof(CanClose));
                }
            }
        }
        #endregion

        #region Title
        private string _Title;
        public string Title
        {
            get { return _Title; }
            set
            {
                if (_Title != value)
                {
                    _Title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }
        #endregion

        #endregion

        public DockWindowViewModel()
        {
            this.CanClose = true;
            this.IsClosed = false;
        }

        public void Close()
        {
            this.IsClosed = true;
        }
    }
}
