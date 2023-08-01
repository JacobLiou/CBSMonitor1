using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SofarHVMExe.ViewModel;

namespace SofarHVMExe.Utilities
{

    public class CommonTreeView : ViewModelBase
    {
        /// <summary>
        /// 父
        /// </summary>
        public CommonTreeView Parent
        {
            get;
            set;
        }

        /// <summary>
        /// 子
        /// </summary>
        public List<CommonTreeView> Children
        {
            get;
            set;
        }

        /// <summary>
        /// 节点的名字
        /// </summary>
        public string NodeName
        {
            get;
            set;
        }

        public bool? _isChecked;
        /// <summary>
        /// CheckBox是否选中
        /// </summary>
        public bool? IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                SetIsChecked(value, true, true);
            }
        }

        public CommonTreeView(string name)
        {
            this.NodeName = name;
            this.Children = new List<CommonTreeView>();
        }
        public CommonTreeView() { }

        private void SetIsChecked(bool? value, bool checkedChildren, bool checkedParent)
        {
            if (_isChecked == value) return;
            _isChecked = value;

            //选中和取消子类
            if (checkedChildren && value.HasValue && Children != null)
                Children.ForEach(ch => ch.SetIsChecked(value, true, false));

            //选中和取消父类
            if (checkedParent && this.Parent != null)
                this.Parent.CheckParentCheckState();

            //通知更改
            this.SetProperty(x => x.IsChecked);
        }

        /// <summary>
        /// 检查父类是否选 中
        /// 如果父类的子类中有一个和第一个子类的状态不一样父类ischecked为null
        /// </summary>
        private void CheckParentCheckState()
        {
            List<CommonTreeView> checkedItems = new List<CommonTreeView>();
            string checkedNames = string.Empty;
            bool? _currentState = this.IsChecked;
            bool? _firstState = null;
            for (int i = 0; i < this.Children.Count(); i++)
            {
                bool? childrenState = this.Children[i].IsChecked;
                if (i == 0)
                {
                    _firstState = childrenState;
                }
                else if (_firstState != childrenState)
                {
                    _firstState = null;
                }
            }
            if (_firstState != null) _currentState = _firstState;
            SetIsChecked(_firstState, false, true);
        }

        /// <summary>
        /// 创建树
        /// </summary>
        /// <param name="children"></param>
        /// <param name="isChecked"></param>

        public void CreateTreeWithChildre(CommonTreeView children, bool? isChecked = false)
        {
            this.Children.Add(children);
            //必须先把孩子加入再为Parent赋值，
            //否则当只有一个子节点时Parent的IsChecked状态会出错

            children.Parent = this;
            children.IsChecked = isChecked;
        }
    }


    /// <summary>
    /// 扩展方法
    /// 避免硬编码问题
    /// </summary>
    public static class NotifyPropertyBaseEx
    {
        public static void SetProperty<T, U>(this T tvm, Expression<Func<T, U>> expre) where T : ViewModelBase, new()
        {
            string _pro = CommonFun.GetPropertyName(expre);
            tvm.OnPropertyChanged(_pro);
        }
    }

    public class CommonFun
    {
        /// <summary>
        /// 返回属性名
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static string GetPropertyName<T, U>(Expression<Func<T, U>> expr)
        {
            string _propertyName = "";
            if (expr.Body is MemberExpression)
            {
                _propertyName = (expr.Body as MemberExpression).Member.Name;
            }
            else if (expr.Body is UnaryExpression)
            {
                _propertyName = ((expr.Body as UnaryExpression).Operand as MemberExpression).Member.Name;
            }
            return _propertyName;
        }
    }
}
