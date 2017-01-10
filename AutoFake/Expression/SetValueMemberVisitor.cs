using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;

namespace AutoFake.Expression
{
    internal class SetValueMemberVisitor : IMemberVisitor
    {
        private readonly object _instance;
        private readonly object _value;

        public SetValueMemberVisitor(GeneratedObject generatedObject, object value)
        {
            _instance = generatedObject.Instance;
            _value = value;
        }

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
            throw new NotSupportedExpressionException("Cannot set value for the method.");
        }

        public void Visit(PropertyInfo propertyInfo)
        {
            if (!propertyInfo.CanWrite)
                throw new NotSupportedExpressionException("Cannot set value for the read-only property");
            propertyInfo.SetValue(_instance, _value, null);
        }

        public void Visit(FieldInfo fieldInfo) => fieldInfo.SetValue(_instance, _value);
    }
}
