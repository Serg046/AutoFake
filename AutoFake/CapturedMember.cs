using Mono.Cecil;

namespace AutoFake
{
    internal class CapturedMember
    {
        public CapturedMember(FieldDefinition field, object instance)
        {
            Field = field;
            Instance = instance;
        }

        public FieldDefinition Field { get; }

        public object Instance { get; }
    }
}
