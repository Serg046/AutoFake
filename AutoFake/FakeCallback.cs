using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Patches;
using AutoFake.Setup.Configurations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake;

internal class FakeCallback(IPatchCollection patches,
    Func<MethodReference, IPatchMember> createPatchMethod, Func<FieldReference, IPatchMember> createPatchField,
    Func<MethodDefinition, IPatchMember, IPatch> createPatch, Func<MethodBody, Instruction, IEmitter> createEmitter) : IFakeCallback
{
    public void Patch(MethodBase callback)
    {
        if (callback.DeclaringType == null) throw new ArgumentNullException("callback.DeclaringType");

        var asmDef = AssemblyDefinition.ReadAssembly(callback.DeclaringType.Module.FullyQualifiedName);
        var methodDef = asmDef.MainModule.ImportReference(callback).AsMethodDefinition();
        foreach (var entryPointCfg in GetEntryPoints(methodDef))
        {
            var entryPointMethod = GetEntryPoint(entryPointCfg);
            var patch = GetPatch(entryPointCfg, entryPointMethod);
            Patch(entryPointMethod, patch);
        }
    }

    private void Patch(MethodDefinition method, IPatch patch)
    {
        // TODO: recursion and nullable Body
        foreach (var cmd in method.Body.Instructions.ToList()) // TODO: Check if needed here and nearby
        {
            if (patch.IsMatch(cmd))
            {
                patch.Inject(createEmitter(method.Body, cmd));
            }
            else if (cmd.Operand is MethodReference methodRef)
            {
                var methodDef = methodRef.AsMethodDefinition();
                if (IsSameModule(methodDef, method))
                {
                    Patch(methodDef, patch);
                }
            }
        }
    }

    private bool IsSameModule(MethodDefinition method1, MethodDefinition method2)
    {
        //TODO: Should be deeper in reality
        return method1.DeclaringType.Module == method2.DeclaringType.Module;
    }

    private IEnumerable<Instruction> GetEntryPoints(MethodDefinition method)
    {
        foreach (var cmd in method.Body.Instructions)
        {
            if (cmd.Operand is MethodReference methodRef)
            {
                if (methodRef.Name == nameof(Fake.Patch) && methodRef.DeclaringType.FullName == typeof(Fake).FullName)
                {
                    yield return cmd;
                }
                else
                {
                    var methodDef = methodRef.AsMethodDefinition();
                    if (IsSameModule(methodDef, method))
                    {
                        foreach (var patch in GetEntryPoints(methodDef))
                        {
                            yield return patch;
                        }
                    }
                }
            }
        }
    }

    private MethodDefinition GetEntryPoint(Instruction instruction)
    {
        var callback = FindCallback(instruction);
        foreach (var cmd in callback.Body.Instructions.Reverse())
        {
            if (cmd.Operand is MethodReference methodRef)
            {
                return methodRef.AsMethodDefinition();
            }
        }

        throw new MissingMemberException("Cannot find a patch");
    }
    
    private IPatch GetPatch(Instruction instruction, MethodDefinition entryPoint)
    {
        var patchCfg = GetPatchCfg(instruction);
        var patchMethod = FindCallback(patchCfg);
        foreach (var cmd in patchMethod.Body.Instructions.Reverse())
        {
            if (cmd.Operand is MethodReference methodRef)
            {
                var patch = createPatch(entryPoint, createPatchMethod(methodRef));
                patches.AddPatch(patchMethod, patch);
                return patch;
            }
            else if (cmd.Operand is FieldReference fieldRef)
            {
                var patch = createPatch(entryPoint, createPatchField(fieldRef));
                patches.AddPatch(patchMethod, patch);
                return patch;
            }
        }

        throw new MissingMemberException("Cannot find a patch member");
    }

    private Instruction GetPatchCfg(Instruction patchCmd)
    {
        while (patchCmd.Next != null)
        {
            patchCmd = patchCmd.Next;
            if (patchCmd.Operand is MethodReference methodRef && methodRef.DeclaringType.FullName == typeof(IPatchConfiguration).FullName)
            {
                switch (methodRef.Name)
                {
                    case nameof(PatchConfiguration.Replace):
                        return patchCmd;
                }
            }
        }

        throw new InvalidOperationException("Cannot find configuration");
    }

    private MethodDefinition FindCallback(Instruction patchCmd)
    {
        while (patchCmd.Previous != null)
        {
            patchCmd = patchCmd.Previous;
            if (patchCmd.OpCode.Code == Code.Ldftn && patchCmd.Operand is MethodReference patcher)
            {
                return patcher.AsMethodDefinition();
            }
        }

        throw new InvalidOperationException("Cannot find configuration");
    }
}