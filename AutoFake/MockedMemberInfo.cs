using System.Collections.Generic;
using Mono.Cecil;

namespace AutoFake
{
    internal class MockedMemberInfo
    {
        public MockedMemberInfo(FakeSetupPack setup)
        {
            Setup = setup;
            ArgumentFields = new List<IList<FieldDefinition>>();
        }

        public FakeSetupPack Setup { get; }
        public FieldDefinition ReturnValueField { get; internal set; }
        public int ActualCallsCount { get; internal set; }
        public IList<IList<FieldDefinition>> ArgumentFields { get; }
    }
}
