using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using CanProtocol.ProtocolModel;

namespace SofarHVMExe.Utilities
{
    // 自定义校验，用于界面中对数据的校验


    /// <summary>
    /// Can帧数据校验规则
    /// </summary>
    public class FrameDataValidationRule : ValidationRule
    {
        private string errorMsg = "请输入合法的值！";

        /// <summary>
        /// 校验
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            BindingExpression be = value as BindingExpression;
            if (be == null || be.BindingGroup == null || be.BindingGroup.Items.Count == 0)
                return new ValidationResult(false, errorMsg);

            CanFrameDataInfo dataInfo = be.BindingGroup.Items[0] as CanFrameDataInfo;

            if (ValidateData(dataInfo.Value))
            {
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, errorMsg);
        }

        private bool ValidateData(string strVal)
        {
            if (strVal == null || strVal == "")
                return false;

            //校验16进制数
            strVal = strVal.Replace("0x", "");
            strVal = strVal.Replace("0X", "");

            if (Regex.IsMatch(strVal, @"^[0-9A-Fa-f]+$"))
            {
                return true;
            }

            return false;
        }//func

    }//class
}
