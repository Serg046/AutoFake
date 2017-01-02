using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal class MockedMemberInfo
    {
        private readonly string _suffixName;
        private readonly IList<IList<FieldDefinition>> _argumentFields;

        public MockedMemberInfo(Mock mock, MethodInfo testMethodInfo, string suffixName)
        {
            _suffixName = suffixName;
            Mock = mock;
            TestMethodInfo = testMethodInfo;
            _argumentFields = new List<IList<FieldDefinition>>();
        }

        public Mock Mock { get; }
        public MethodInfo TestMethodInfo { get; }
        public FieldDefinition RetValueField { get; internal set; }
        public FieldDefinition ActualCallsField { get; internal set; }
        public FieldDefinition CallbackField { get; internal set; }
        public int SourceCodeCallsCount { get; internal set; }

        public IList<FieldDefinition> GetArguments(int index) => _argumentFields[index];
        public void AddArguments(IList<FieldDefinition> arguments) => _argumentFields.Add(arguments);
        public int ArgumentsCount => _argumentFields.Count;
        public string EvaluateRetValueFieldName()
        {
            return new StringBuilder(Mock.Method.ReturnType.ToString().Replace(".", ""))
                .Append("_").Append(Mock.Method.Name).Append("_")
                .Append(ArgumentsToString(Mock.Method))
                .Append(_suffixName)
                .ToString();
        }

        private string ArgumentsToString(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return parameters.Any()
                ? string.Join(string.Empty, parameters.Select(p => p.ParameterType.ToString().Replace(".", ""))) + "_"
                : string.Empty;
        }
    }
}
