using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    internal interface IMemberVisitor
    {
        void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo);
        void Visit(PropertyInfo propertyInfo);
        void Visit(FieldInfo fieldInfo);
    }
}
