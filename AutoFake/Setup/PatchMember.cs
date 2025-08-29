using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace AutoFake.Setup;

internal class PatchMember(MemberReference patch)
{
    private readonly Lazy<IReadOnlyDictionary<string, TypeReference>> _patchGenericArguments = new(() => GetGenericArguments(patch));
    
    protected bool IsMatch(MemberReference member)
    {
        return patch.ContainsGenericParameter
            ? patch.ToString() == member.ToString() && IsMatch(GetGenericArguments(member))
            : patch == member;
    }

    private bool IsMatch(IReadOnlyDictionary<string, TypeReference> currentMemberGenerics)
    {
        foreach (var generic in _patchGenericArguments.Value)
        {
            if (!currentMemberGenerics.TryGetValue(generic.Key, out var currentGenericType) || generic.Value != currentGenericType) // TODO: this type could be also generic
            {
                return false;
            }
        }

        return true;
    }

    protected bool TryGetGenericReturnType(TypeReference originalReturnType, [NotNullWhen(true)]out TypeReference? returnType)
    {
        // TODO: Test this scenario
        if (originalReturnType is GenericParameter genericParameter)
        {
            returnType = _patchGenericArguments.Value.TryGetValue(genericParameter.FullName, out var generic)
                ? generic
                : throw NoGenericParameter(genericParameter);
            return true;
        }
        
        if (originalReturnType is GenericInstanceType genericReturnType && genericReturnType.GenericArguments.Any(g => g.IsGenericParameter))
        {
            var returnTypeDef = originalReturnType.AsTypeDefinition();
            genericReturnType = new GenericInstanceType(returnTypeDef);
            foreach (var genericPrm in returnTypeDef.GenericParameters)
            {
                if (!_patchGenericArguments.Value.TryGetValue(genericPrm.FullName, out var generic))
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
    
    private static IReadOnlyDictionary<string, TypeReference> GetGenericArguments(MemberReference member)
    {
        var generics = new Dictionary<string, TypeReference>();
        if (member is IGenericInstance generic)
        {
            var genericDef = GetPatchGenericParameterProvider(member);
            AddGenerics(generic.GenericArguments, genericDef.GenericParameters);
        }
            
        if (member.DeclaringType is GenericInstanceType genericType)
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

    private static IGenericParameterProvider GetPatchGenericParameterProvider(MemberReference member)
    {
        return (member as IMemberDefinition) as IGenericParameterProvider 
               ?? member.Resolve() as IGenericParameterProvider
               ?? throw new InvalidOperationException("Cannot get generic parameters");
    }
}