using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    public class MemoryModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public MemoryModel()
        {
            //Address = "0x0";
            //Type = "整型（E1）";
            //Value = "0x";
        }

        //public string Title { get; set; }
        private string addressOrName = "";
        public string AddressOrName
        {
            get => addressOrName;
            set
            {
                if (addressOrName != value)
                {
                    addressOrName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string type = "float";
        public string Type
        {
            get => type;
            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged();
                }
            }
        }
        private string value = "";
        public string Value
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    OnPropertyChanged();
                }
            }
        }
        private string remark = "";
        public string Remark
        {
            get => remark;
            set
            {
                if (remark != value)
                {
                    remark = value;
                    OnPropertyChanged();
                }
            }
        }
        private string address = "";
        public string Address
        {
            get => address;
            set
            {
                if (address != value)
                {
                    address = value;
                    OnPropertyChanged();
                }
            }
        }
    }//class
}
