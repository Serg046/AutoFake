using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake;

internal class ContractProcessor : IContractProcessor
{
	private readonly ITypeInfo _typeInfo;
	private readonly IMockFactory _mockFactory;
	private readonly IMockCollection _mockCollection;
	private readonly Dictionary<string, TypeReference> _importedTypes;

	public ContractProcessor(ITypeInfo typeInfo, IMockFactory mockFactory, IMockCollection mockCollection)
	{
		_typeInfo = typeInfo;
		_mockFactory = mockFactory;
		_mockCollection = mockCollection;
		_importedTypes = new Dictionary<string, TypeReference>();
	}

	public void ProcessAllOriginalMethodContractsWithMocks(MethodDefinition methodDef)
	{
		foreach (var method in _typeInfo.GetAllImplementations(methodDef))
		{
			ProcessOriginalMethodContractWithMocks(method);
		}
	}

	public void ProcessOriginalMethodContract(MethodDefinition methodDef)
	{
		if (_importedTypes.TryGetValue(methodDef.ReturnType.ToString(), out var importedType))
		{
			methodDef.ReturnType = importedType;
		}

		foreach (var parameterDef in methodDef.Parameters)
		{
			if (_importedTypes.TryGetValue(parameterDef.ParameterType.ToString(), out importedType))
			{
				parameterDef.ParameterType = importedType;
			}
		}
	}

	public void ProcessCommonOriginalContracts(IEnumerable<ISourceMemberMock> sourceMemberMocks)
	{
		foreach (var mock in sourceMemberMocks)
		{
			if (mock.SourceMemberMetaData.SourceMember.ReturnType != typeof(void) && mock.SourceMemberMetaData.SourceMember.ReturnType.Module == _typeInfo.SourceType.Module)
			{
				AddReplaceContractMocks(_typeInfo.GetTypeDefinition(mock.SourceMemberMetaData.SourceMember.ReturnType));
			}

			if (mock.SourceMemberMetaData.SourceMember.OriginalMember is MethodBase method &&
				method.Module == _typeInfo.SourceType.Module && method.DeclaringType != null)
			{
				var typeDef = _typeInfo.GetTypeDefinition(method.DeclaringType);
				var methodRef = _typeInfo.ImportToSourceAsm(method);
				var methodDef = _typeInfo.GetMethod(typeDef, methodRef);
				if (methodDef != null)
				{
					ProcessAllOriginalMethodContractsWithMocks(methodDef);
				}
			}
		}
	}

	private void ProcessOriginalMethodContractWithMocks(MethodDefinition methodDef)
	{
		if (methodDef.ReturnType != null && methodDef.ReturnType.FullName != "System.Void" && _typeInfo.IsInFakeModule(methodDef.ReturnType))
		{
			AddReplaceContractMocks(methodDef.ReturnType.ToTypeDefinition());
			methodDef.ReturnType = _typeInfo.ImportToSourceAsm(methodDef.ReturnType);
		}

		foreach (var parameterDef in methodDef.Parameters.Where(parameterDef => _typeInfo.IsInFakeModule(parameterDef.ParameterType)))
		{
			var typeDefinition = parameterDef.ParameterType.ToTypeDefinition();
			AddReplaceContractMocks(typeDefinition);
			parameterDef.ParameterType = _typeInfo.ImportToSourceAsm(parameterDef.ParameterType);
		}
	}

	public void AddReplaceContractMocks(TypeDefinition typeDef)
	{
		foreach (var mockTypeDef in _typeInfo.TypeMap.GetAllParentsAndDescendants(typeDef))
		{
			var importedTypeRef = _typeInfo.ImportToSourceAsm(mockTypeDef);
			if (mockTypeDef.IsInterface)
			{
				_mockCollection.ContractMocks.Add(_mockFactory.GetReplaceInterfaceCallMock(importedTypeRef));
			}
			else
			{
				_mockCollection.ContractMocks.Add(mockTypeDef.IsValueType ?
					_mockFactory.GetReplaceValueTypeCtorMock(importedTypeRef)
					: _mockFactory.GetReplaceReferenceTypeCtorMock(importedTypeRef));
			}

			_mockCollection.ContractMocks.Add(_mockFactory.GetReplaceTypeCastMock(importedTypeRef));
			TryAddImportedType(mockTypeDef, importedTypeRef);
		}
	}

	private void TryAddImportedType(TypeDefinition typeDef, TypeReference typeRef)
	{
		var contract = typeDef.ToString();
		if (!_importedTypes.ContainsKey(contract)) _importedTypes.Add(contract, typeRef);
	}
}
