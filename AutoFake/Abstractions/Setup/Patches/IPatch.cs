using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup.Patches;

public interface IPatch
{
    TypeDefinition Type { get; }
    FieldDefinition RetValueField { get; }
    bool IsMatch(Instruction instruction);
    void Inject(IEmitter emitter);
    Assembly? PatchedAssembly { get; set; }
}