using System.Collections.Generic;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal class MockedMemberInfo
    {
        private readonly IList<IList<FieldDefinition>> _argumentFields;

        public MockedMemberInfo(FakeSetupPack setup)
        {
            Setup = setup;
            _argumentFields = new List<IList<FieldDefinition>>();
        }

        public FakeSetupPack Setup { get; }
        public FieldDefinition RetValueField { get; internal set; }
        public FieldDefinition ActualCallsField { get; internal set; }
        public int SourceCodeCallsCount { get; internal set; }

        public IList<FieldDefinition> GetArguments(int index) => _argumentFields[index];

        public void AddArguments(IList<FieldDefinition> arguments)
        {
            _argumentFields.Add(arguments);
        }
    }
}
