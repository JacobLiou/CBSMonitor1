using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    public class ProjectConfigModel
    {
        public ProjectConfigModel() { }

        public ProjectConfigModel(string title, string value)
        {
            Title = title;
            Value = value;
        }

        public string Title { get; set; }
        public string Value { get; set; }
    }
}
