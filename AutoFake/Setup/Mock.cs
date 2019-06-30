using System;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
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

        public virtual void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
        {
            if (mockedMemberInfo.SetupBodyField != null)
            {
                var field = GetField(generatedObject, mockedMemberInfo.SetupBodyField.Name);
                field.SetValue(null, _invocationExpression);
            }
        }

        public bool IsInstalledInstruction(ITypeInfo typeInfo, Instruction instruction)
            => SourceMember.IsCorrectInstruction(typeInfo, instruction);

        public bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod)
        {
            //for .net 4, it is available in .net 4.5
            dynamic asyncAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == ASYNC_STATE_MACHINE_ATTRIBUTE);
            if (asyncAttribute != null)
            {
                if (asyncAttribute.ConstructorArguments.Count != 1)
                    throw new FakeGeneretingException("Unexpected exception. AsyncStateMachine has several arguments or 0.");
                TypeReference generatedAsyncType = asyncAttribute.ConstructorArguments[0].Value;
                asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }

        protected FieldInfo GetField(GeneratedObject generatedObject, string fieldName)
            => generatedObject.Type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
    }
}
