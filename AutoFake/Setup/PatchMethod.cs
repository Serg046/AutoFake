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
        if (patch.ReturnType is GenericParameter genericParameter)
        {
            var generics = GetGenerics();
            returnType = generics.TryGetValue(genericParameter.FullName, out var generic)
                ? generic
                : throw NoGenericParameter(genericParameter);
            return true;
        }
        
        if (patch.ReturnType is GenericInstanceType genericReturnType && genericReturnType.GenericArguments.Any(g => g.IsGenericParameter))
        {
            var generics = GetGenerics();
            var returnTypeDef = patch.ReturnType.AsTypeDefinition();
            genericReturnType = new GenericInstanceType(returnTypeDef);
            foreach (var genericPrm in returnTypeDef.GenericParameters)
            {
                if (!generics.TryGetValue(genericPrm.FullName, out var generic))
                {
                    throw NoGenericParameter(genericPrm);
                }
                    
                genericReturnType.GenericArguments.Add(generic);
            }

            returnType = patch.Module.ImportReference(genericReturnType);
            return true;
        }

        returnType = null;
        return false;
    }

    private InvalidOperationException NoGenericParameter(GenericParameter genericParameter)
        => new($"Cannot find {genericParameter.FullName} generic parameter");

    private Dictionary<string, TypeReference> GetGenerics()
    {
        var generics = new Dictionary<string, TypeReference>();
        if (patch is GenericInstanceMethod genericMethod)
        {
            var methodDef = genericMethod.AsMethodDefinition();
            AddGenerics(genericMethod.GenericArguments, methodDef.GenericParameters);
        }
            
        if (patch.DeclaringType is GenericInstanceType genericType)
        {
            var typeDef = genericType.AsTypeDefinition();
            AddGenerics(genericType.GenericArguments, typeDef.GenericParameters);
        }

        return generics;
        
        void AddGenerics(IList<TypeReference> genericArguments, IList<GenericParameter> genericParameters)
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
}