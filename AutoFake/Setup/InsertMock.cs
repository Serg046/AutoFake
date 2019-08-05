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

        public InsertMock(MethodDescriptor action, Location location)
        {
            _location = location;
            Action = action;
        }

        public MethodDescriptor Action { get; }

        [ExcludeFromCodeCoverage]
        public string UniqueName => null;

        [ExcludeFromCodeCoverage]
        public void AfterInjection(IMocker mocker, ILProcessor ilProcessor)
        {
        }

        [ExcludeFromCodeCoverage]
        public void BeforeInjection(IMocker mocker)
        {
        }

        public IList<object> Initialize(MockedMemberInfo mockedMemberInfo, Type type)
        {
            return new List<object>();
        }

        public void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            methodMocker.InjectCallback(ilProcessor, instruction, Action, beforeInstruction: true);
        }

        [ExcludeFromCodeCoverage] // No way to exclude default statement, see https://github.com/OpenCover/opencover/issues/907
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
