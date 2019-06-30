using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class MockedMemberInfo
    {
        private readonly string _suffixName;
        private readonly IList<IList<FieldDefinition>> _argumentFields;

        public MockedMemberInfo(Mock mock, string testMethodName, string suffixName)
        {
            _suffixName = suffixName;
            Mock = mock;
            TestMethodName = testMethodName;
            _argumentFields = new List<IList<FieldDefinition>>();
        }

        public Mock Mock { get; }
        public string TestMethodName { get; }
        public FieldDefinition SetupBodyField { get; internal set; }
        public FieldDefinition RetValueField { get; internal set; }
        public FieldDefinition ActualCallsField { get; internal set; }
        public VariableDefinition ActualCallsAccumulator { get; internal set; }
        public FieldDefinition ExpectedCallsFuncField { get; internal set; }
        public FieldDefinition CallbackField { get; internal set; }

        public IList<FieldDefinition> GetArguments(int index) => _argumentFields[index];
        public void AddArguments(IList<FieldDefinition> arguments) => _argumentFields.Add(arguments);
        public int ArgumentsCount => _argumentFields.Count;

        public string GenerateFieldName()
        {
            return new StringBuilder(Mock.SourceMember.ReturnType.ToString().Replace(".", ""))
                .Append("_").Append(Mock.SourceMember.Name).Append("_")
                .Append(ArgumentsToString(Mock.SourceMember))
                .Append(_suffixName)
                .ToString();
        }

        private string ArgumentsToString(ISourceMember member)
        {
            var parameters = member.GetParameters();
            return parameters.Any()
                ? string.Join(string.Empty, parameters.Select(p => p.ParameterType.ToString().Replace(".", ""))) + "_"
                : string.Empty;
        }
    }
}
