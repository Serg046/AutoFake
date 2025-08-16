using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Patches;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Pure.DI;
using static Pure.DI.Tag;

namespace AutoFake;

internal class FakeCallback(
    IPatchCollection patches,
    [Tag(Method)] Func<MethodReference, IPatchMember> createPatchMethod,
    [Tag(Field)] Func<FieldReference, IPatchMember> createPatchField,
    Func<string, (MethodDefinition EntryPoint, MethodDefinition PatchCallback), IPatchMember, IPatch> createPatch) : IFakeCallback
{
    public void Patch(MethodBase callback)
    {
        if (callback.DeclaringType == null) throw new ArgumentNullException("callback.DeclaringType");

        var asmDef = AssemblyDefinition.ReadAssembly(callback.DeclaringType.Module.FullyQualifiedName);
        var methodDef = asmDef.MainModule.ImportReference(callback).AsMethodDefinition();
        foreach (var entryPointCfg in GetEntryPoints(methodDef))
        {
            var entryPoint = GetEntryPoint(entryPointCfg);
            var patch = GetPatch(entryPointCfg, entryPoint.Method,entryPoint.Key);
            Patch(entryPoint.Method, patch);
        }
    }

    private void Patch(MethodDefinition method, IPatch patch)
    {
        // TODO: recursion and nullable Body
        foreach (var cmd in method.Body.Instructions.ToList()) // TODO: Check if needed here and nearby
        {
            if (patch.IsMatch(cmd))
            {
                patch.Inject(method.Body, cmd);
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

    private (MethodDefinition Method, string Key) GetEntryPoint(Instruction instruction)
    {
        var callback = FindCallback(instruction);
        var patchKey = PatchCollection.GetPatchKey(callback);
        foreach (var cmd in callback.Body.Instructions.Reverse())
        {
            if (cmd.Operand is MethodReference methodRef)
            {
                return (methodRef.AsMethodDefinition(), patchKey);
            }
        }

        throw new MissingMemberException("Cannot find a patch");
    }
    
    private IPatch GetPatch(Instruction instruction, MethodDefinition entryPoint, string patchKey)
    {
        var patchCfg = GetPatchCfg(instruction);
        var patchCallback = FindCallback(patchCfg);
        foreach (var cmd in patchCallback.Body.Instructions.Reverse())
        {
            if (cmd.Operand is MethodReference methodRef)
            {
                var patch = createPatch(patchKey, (entryPoint, patchCallback), createPatchMethod(methodRef));
                patches.AddPatch(patch);
                return patch;
            }
            else if (cmd.Operand is FieldReference fieldRef)
            {
                var patch = createPatch(patchKey, (entryPoint, patchCallback), createPatchField(fieldRef));
                patches.AddPatch(patch);
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