using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace AutoFake.Setup;

internal class PatchMember(MemberReference patch)
{
    protected bool TryGetGenericReturnType(TypeReference originalReturnType, [NotNullWhen(true)]out TypeReference? returnType)
    {
        if (originalReturnType is GenericParameter genericParameter)
        {
            var generics = GetGenerics();
            returnType = generics.TryGetValue(genericParameter.FullName, out var generic)
                ? generic
                : throw NoGenericParameter(genericParameter);
            return true;
        }
        
        if (originalReturnType is GenericInstanceType genericReturnType && genericReturnType.GenericArguments.Any(g => g.IsGenericParameter))
        {
            var generics = GetGenerics();
            var returnTypeDef = originalReturnType.AsTypeDefinition();
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
        if (patch is IGenericInstance generic)
        {
            var genericDef = patch as IGenericParameterProvider
                             ?? patch.Resolve() as IGenericParameterProvider
                             ?? throw new InvalidOperationException("Cannot get generic parameters");
            AddGenerics(generic.GenericArguments, genericDef.GenericParameters);
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