using Mono.Cecil;

namespace AutoFake
{
    internal class CapturedMember
    {
        public CapturedMember(FieldDefinition closureField, FieldDefinition generatedField, object instance)
        {
            ClosureField = closureField;
            Instance = instance;
            GeneratedField = generatedField;
        }

        public FieldDefinition ClosureField { get; }

        public FieldDefinition GeneratedField { get; }

        public object Instance { get; }
    }
}
