using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AutoFake.Exceptions;
using AutoFake.Expression;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal abstract class SourceMemberMock : IMock
    {
        private readonly IInvocationExpression _invocationExpression;
        private readonly Lazy<string> _uniqueName;

        protected SourceMemberMock(IInvocationExpression invocationExpression)
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

        public virtual void BeforeInjection(IMocker mocker)
        {
            if (CheckSourceMemberCalls) mocker.GenerateSetupBodyField();
            if (ExpectedCallsFunc != null) mocker.GenerateCallsCounterFuncField();
        }

        public abstract void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction);

        public virtual IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            if (mockedMemberInfo.SetupBodyField != null)
            {
                var field = GetField(type, mockedMemberInfo.SetupBodyField.Name);
                if (field == null) throw new FakeGeneretingException($"'{mockedMemberInfo.SetupBodyField.Name}' is not found in the generated object");
                field.SetValue(null, _invocationExpression);
            }
            if (ExpectedCallsFunc != null)
            {
                var field = GetField(type, mockedMemberInfo.ExpectedCallsFuncField.Name);
                if (field == null) throw new FakeGeneretingException($"'{mockedMemberInfo.ExpectedCallsFuncField.Name}' is not found in the generated object");
                field.SetValue(null, ExpectedCallsFunc);
            }
            return new List<object>();
        }

        public bool IsSourceInstruction(ITypeInfo typeInfo, Mono.Cecil.Cil.MethodBody method, Instruction instruction)
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

        public void AfterInjection(IMocker mocker, ILProcessor ilProcessor)
        {
            if (CheckSourceMemberCalls)
            {
                mocker.InjectVerification(ilProcessor, CheckArguments, ExpectedCallsFunc != null);
            }
        }
    }
}
