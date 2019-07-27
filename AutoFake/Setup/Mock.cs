using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal abstract class Mock : IMock
    {
        private const string ASYNC_STATE_MACHINE_ATTRIBUTE = "AsyncStateMachineAttribute";

        private readonly IInvocationExpression _invocationExpression;

        protected Mock(IInvocationExpression invocationExpression)
        {
            _invocationExpression = invocationExpression;
            SourceMember = invocationExpression.GetSourceMember();
        }

        public abstract bool CheckArguments { get; }
        public abstract Func<byte, bool> ExpectedCalls { get; }

        public ISourceMember SourceMember { get; }

        public abstract void PrepareForInjecting(IMocker mocker);
        public abstract void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction);

        public virtual IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            if (mockedMemberInfo.SetupBodyField != null)
            {
                var field = GetField(type, mockedMemberInfo.SetupBodyField.Name);
                field.SetValue(null, _invocationExpression);
            }
            return new object[0];
        }

        public bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod)
        {
            //for .net 4, it is available in .net 4.5
            dynamic asyncAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == ASYNC_STATE_MACHINE_ATTRIBUTE);
            if (asyncAttribute != null)
            {
                TypeReference generatedAsyncType = asyncAttribute.ConstructorArguments[0].Value;
                asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }

        protected FieldInfo GetField(Type type, string fieldName)
            => type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
    }
}
