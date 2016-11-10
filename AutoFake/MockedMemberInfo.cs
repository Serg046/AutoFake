using System.Collections.Generic;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal class MockedMemberInfo
    {
        private readonly string _suffixName;
        private readonly IList<IList<FieldDefinition>> _argumentFields;

        public MockedMemberInfo(FakeSetupPack setup, MethodInfo testMethodInfo, string suffixName)
        {
            _suffixName = suffixName;
            Setup = setup;
            TestMethodInfo = testMethodInfo;
            _argumentFields = new List<IList<FieldDefinition>>();
        }

        public FakeSetupPack Setup { get; }
        public MethodInfo TestMethodInfo { get; }
        public FieldDefinition RetValueField { get; internal set; }
        public FieldDefinition ActualCallsField { get; internal set; }
        public int SourceCodeCallsCount { get; internal set; }

        public IList<FieldDefinition> GetArguments(int index) => _argumentFields[index];
        public void AddArguments(IList<FieldDefinition> arguments) => _argumentFields.Add(arguments);
        public int ArgumentsCount => _argumentFields.Count;
        public string EvaluateRetValueFieldName() => Setup.ReturnObjectFieldName + "_" + _suffixName;
    }
}
