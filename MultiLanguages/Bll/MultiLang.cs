using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiLanguages.Bll
{
   public class MultiLang
    {
        public static void InitializeFormLang(Control rootControl)
        {
            Control.ControlCollection childrenControls = rootControl.Controls;

            //遍历所有控件
            foreach (Control control in childrenControls)
            {

                if (control is MenuStrip)
                {
                    var menuStrip = control as MenuStrip;

                    foreach (ToolStripItem item in menuStrip.Items)
                    {
                        item.Text = MultiLangApi.GetLangValue(rootControl.Name, item.Name);
                        InitializeStripMenuLang(rootControl, item);
                    }
                }

                if (control.Controls.Count > 0)
                {
                    InitializeContainerLang(rootControl, control);
                }

                control.Text = string.IsNullOrEmpty(MultiLangApi.GetLangValue(rootControl.Name, control.Name)) ? control.Text :
                   MultiLangApi.GetLangValue(rootControl.Name, control.Name);

                if (control.Controls != null)
                {
                    InitializeFormLang(control);
                }
            }
        }
        private static void InitializeStripMenuLang(Control rootControl, ToolStripItem tsi)
        {
            if (tsi is ToolStripMenuItem)
            {
                ToolStripMenuItem tsmi = tsi as ToolStripMenuItem;
                foreach (ToolStripItem item in tsmi.DropDownItems)
                {
                    item.Text = string.IsNullOrEmpty(MultiLangApi.GetLangValue(rootControl.Name, item.Name)) ? item.Text :
                         MultiLangApi.GetLangValue(rootControl.Name, item.Name);

                    InitializeStripMenuLang(rootControl, item);
                }
            }
        }

        private static void InitializeContainerLang(Control rootControl, Control containerControl)
        {
            Control.ControlCollection sonControls = containerControl.Controls;
            foreach (Control control in sonControls)
            {
                control.Text = string.IsNullOrEmpty(MultiLangApi.GetLangValue(rootControl.Name, control.Name)) ? control.Text :
                      MultiLangApi.GetLangValue(rootControl.Name, control.Name);

                if (control.Controls != null)
                {
                    InitializeContainerLang(rootControl, control);
                }
            }
        }

    }
}
