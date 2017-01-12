using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class VerifiableMock : Mock
    {
        private readonly Parameters _parameters;

        public VerifiableMock(ISourceMember sourceMember, IList<FakeArgument> setupArguments, Parameters parameters)
            : base(sourceMember, setupArguments)
        {
            _parameters = parameters;
        }

        private bool NeedCallsCounter => _parameters.NeedCheckArguments || _parameters.ExpectedCallsCountFunc != null;

        public override void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction)
        {
            if (NeedCallsCounter)
            {
                methodMocker.InjectCurrentPositionSaving(ilProcessor, instruction);
                methodMocker.MemberInfo.SourceCodeCallsCount++;
            }
            if (SetupArguments.Any() && _parameters.NeedCheckArguments)
            {
                var arguments = methodMocker.PopMethodArguments(ilProcessor, instruction);
                methodMocker.MemberInfo.AddArguments(arguments);
                methodMocker.PushMethodArguments(ilProcessor, instruction, arguments);
            }
        }

        public override void Verify(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
            => Verify(mockedMemberInfo, generatedObject, _parameters.NeedCheckArguments, _parameters.ExpectedCallsCountFunc);

        public override void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject)
        {
        }

        public override void PrepareForInjecting(IMocker mocker)
        {
            if (NeedCallsCounter)
                mocker.GenerateCallsCounter();
        }

        internal class Parameters
        {
            public bool NeedCheckArguments { get; set; }
            public Func<int, bool> ExpectedCallsCountFunc { get; set; }
        }
    }
}
