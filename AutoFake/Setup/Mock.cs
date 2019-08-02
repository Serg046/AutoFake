using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal abstract class Mock : IMock
    {
        private readonly IInvocationExpression _invocationExpression;
        private readonly Lazy<string> _uniqueName;

        protected Mock(IInvocationExpression invocationExpression)
        {
            _invocationExpression = invocationExpression;
            SourceMember = invocationExpression.GetSourceMember();
            _uniqueName = new Lazy<string>(() => GetUniqueName(SourceMember));
        }

        public bool CheckArguments { get; set; }
        public Func<byte, bool> ExpectedCallsFunc { get; set; }

        public bool CheckSourceMemberCalls => CheckArguments || ExpectedCallsFunc != null;
        public ISourceMember SourceMember { get; }
        public string UniqueName => _uniqueName.Value;

        public abstract void BeforeInjection(IMocker mocker);
        public abstract void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction);
        public abstract void AfterInjection(IMocker mocker, ILProcessor ilProcessor);

        public virtual IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            if (mockedMemberInfo.SetupBodyField != null)
            {
                var field = GetField(type, mockedMemberInfo.SetupBodyField.Name);
                field.SetValue(null, _invocationExpression);
            }
            return new object[0];
        }

        public bool IsSourceInstruction(ITypeInfo typeInfo, Instruction instruction)
        {
            return SourceMember.IsSourceInstruction(typeInfo, instruction);
        }
        
        protected FieldInfo GetField(Type type, string fieldName)
            => type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        private static string GetUniqueName(ISourceMember sourceMember)
        {
            var fieldName = new StringBuilder(sourceMember.ReturnType.FullName)
                .Replace(".", "")
                .Append("_").Append(sourceMember.Name);
            foreach (var parameter in sourceMember.GetParameters())
            {
                fieldName.Append("_").Append(parameter.ParameterType.FullName.Replace(".", ""));
            }
            return fieldName.ToString();
        }
    }
}
