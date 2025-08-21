using System.Diagnostics.CodeAnalysis;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup;

internal class PatchMethod(MethodReference patch) : IPatchMember
{
    public string Name => patch.Name;
    public bool HasThis => patch.HasThis;
    public TypeReference ReturnType => TryGetGenericReturnType(out var returnType) ? returnType : patch.ReturnType;

    public IReadOnlyList<ParameterDefinition> GetParameters() => patch.Parameters.ToArray();

    public bool IsMatch(Instruction instruction)
    {
        return instruction.OpCode.Code is Code.Call or Code.Callvirt // TODO: Code.Calli?
               && instruction.Operand is MethodReference methodRef
               && methodRef.Compare(patch);
    }
    
    private bool TryGetGenericReturnType([NotNullWhen(true)]out TypeReference? returnType)
    {
        if (patch.ReturnType is GenericInstanceType genericReturnType && genericReturnType.GenericArguments.Any(g => g.IsGenericParameter))
        {
            var generics = new Dictionary<string, TypeReference>();
            if (patch is GenericInstanceMethod genericMethod)
            {
                var methodDef = genericMethod.AsMethodDefinition();
                AddGenerics(generics, genericMethod.GenericArguments, methodDef.GenericParameters);
            }
            
            if (patch.DeclaringType is GenericInstanceType genericType)
            {
                var typeDef = genericType.AsTypeDefinition();
                AddGenerics(generics, genericType.GenericArguments, typeDef.GenericParameters);
            }
            
            var returnTypeDef = patch.ReturnType.AsTypeDefinition();
            genericReturnType = new GenericInstanceType(returnTypeDef);
            foreach (var genericParameter in returnTypeDef.GenericParameters)
            {
                if (!generics.TryGetValue(genericParameter.FullName, out var generic))
                {
                    throw new InvalidOperationException($"Cannot find {genericParameter.FullName} generic parameter");
                }
                    
                genericReturnType.GenericArguments.Add(generic);
            }

            returnType = patch.Module.ImportReference(genericReturnType);
            return true;
        }

        returnType = null;
        return false;
    }

    private void AddGenerics(IDictionary<string, TypeReference> generics, IList<TypeReference> genericArguments, IList<GenericParameter> genericParameters)
    {
        for (var i = 0; i < genericArguments.Count; i++)
        {
            if (i < genericParameters.Count)
            {
                generics.Add(genericParameters[i].FullName, genericArguments[i]);
            }
        }
    }
}