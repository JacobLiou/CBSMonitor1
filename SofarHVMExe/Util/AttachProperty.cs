using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SofarHVMExe.Utilities
{
    /// <summary>
    /// TextBox附加属性
    /// </summary>
    public class TextBoxAttachedProperties
    {
        public static bool GetIsOnlyNumber(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsOnlyNumberProperty);
        }

        public static void SetIsOnlyNumber(DependencyObject obj, bool value)
        {
            obj.SetValue(IsOnlyNumberProperty, value);
        }

        public static readonly DependencyProperty IsOnlyNumberProperty =
            DependencyProperty.RegisterAttached("IsOnlyNumber", typeof(bool), typeof(TextBox), new PropertyMetadata(false,
                (s, e) =>
                {
                    if (s is TextBox textBox)
                    {
                        textBox.SetValue(InputMethod.IsInputMethodEnabledProperty, !(bool)e.NewValue);
                        textBox.PreviewTextInput -= TxtInput;
                        if (!(bool)e.NewValue)
                        {
                            textBox.PreviewTextInput += TxtInput;
                        }
                    }
                }));

        private static void TxtInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

    }//class
}
