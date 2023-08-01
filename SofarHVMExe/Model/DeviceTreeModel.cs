using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    public class DeviceTreeModel : INotifyPropertyChanged
    {

        public DeviceTreeModel()
        {

        }

        public DeviceTreeModel(string name, int id, int parentID, params DeviceTreeModel[] movies)
        {
            ID = id;
            Name = name;
            ParentId = parentID;
            if (movies != null && movies.Length > 0)
            {
                Children = new ObservableCollection<DeviceTreeModel>(movies);
            }
        }

        public string Name { get; set; }
        public int ID { get; set; }
        public int ParentId { get; set; }
        public ObservableCollection<DeviceTreeModel> Children { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
