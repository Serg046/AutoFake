using System.Collections.Generic;

namespace AutoFake
{
    internal class ClosureDescriptor : MethodDescriptor
    {
        public ClosureDescriptor(string declaringType, string name, ICollection<CapturedMember> capturedMembers)
            : base(declaringType, name)
        {
            CapturedMembers = capturedMembers;
        }

        public ICollection<CapturedMember> CapturedMembers { get; }
    }
}
