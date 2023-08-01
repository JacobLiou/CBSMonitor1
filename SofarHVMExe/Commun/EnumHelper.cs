using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Commun
{
    public static class EnumHelper
    {
        public static string GetDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            if(fieldInfo == null) { return value.ToString(); }
            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static string GetCombinedDescription(Enum combinedValue)
        {
            Type enumType = combinedValue.GetType();
            var flagValues = Enum.GetValues(enumType).Cast<Enum>().Where(combinedValue.HasFlag);

            string description = string.Join(", ", flagValues.Select(value =>
            {
                MemberInfo[] memberInfos = enumType.GetMember(value.ToString());
                if (memberInfos.Length > 0)
                {
                    var descriptionAttribute = memberInfos[0].GetCustomAttributes(typeof(DescriptionAttribute), false)
                        .FirstOrDefault() as DescriptionAttribute;
                    return descriptionAttribute?.Description ?? value.ToString();
                }
                return value.ToString();
            }));

            return description;
        }

        public static bool GetEnumValueFromDescription<T>(string description, out T enumValue) where T : Enum
        {
            enumValue = default;
            foreach (var field in typeof(T).GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null && attribute.Description == description)
                {
                    enumValue = (T)field.GetValue(null);
                    return true;
                }
            }

            return false;
            //throw new ArgumentException($"No enum value with description '{description}' found.");
        }
    }
}
