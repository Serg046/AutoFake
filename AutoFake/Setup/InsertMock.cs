using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake.Setup
{
    internal class InsertMock : IMock
    {
        private readonly Location _location;

        public InsertMock(Action action, Location location)
        {
            _location = location;
            Action = new MethodDescriptor(action.Method.DeclaringType.FullName, action.Method.Name);
        }

        public MethodDescriptor Action { get; }

        public string UniqueName => "Callback";

        public bool CheckSourceMemberCalls => false;

        public void AfterInjection(IMocker mocker, ILProcessor ilProcessor)
        {
        }

        public void BeforeInjection(IMocker mocker)
        {
        }

        public IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            return new List<object>();
        }

        public void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            methodMocker.InjectCallback(ilProcessor, instruction, Action);
        }

        public bool IsSourceInstruction(ITypeInfo typeInfo, MethodBody method, Instruction instruction)
        {
            switch (_location)
            {
                case Location.Top: return instruction == method.Instructions.First();
                case Location.Bottom: return instruction == method.Instructions.Last();
                default: return false;
            }
        }

        public enum Location
        {
            Top,
            Bottom
        }
    }
}
