using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SofarHVMExe;
using SofarHVMExe.Utilities;
using SofarHVMExe.ViewModel;

namespace SofarHVMExe.ViewModel
{
    /// <summary>
    /// ViewModel基类
    /// 所有的view model都需要继承此类
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
