using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Utilities
{
    /// <summary>
    /// 对象帮助类
    /// </summary>
    internal class ObjectHelper
    {
        /// <summary>
        /// 复制对象(深拷贝)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T? CopyObj<T>(T obj)
        {
            //如果是字符串或值类型则直接返回
            if (obj == null || (obj is string) || (obj.GetType().IsValueType)) return obj;

            object? retval = Activator.CreateInstance(obj.GetType());
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                try { field.SetValue(retval, CopyObj(field.GetValue(obj))); }
                catch { }
            }
            return (T)retval;
        }
    }
}
