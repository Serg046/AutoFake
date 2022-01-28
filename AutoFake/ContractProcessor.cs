using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
	internal class ContractProcessor : IContractProcessor
	{
		private readonly ITypeInfo _typeInfo;
		private readonly IAssemblyWriter _assemblyWriter;
		private readonly IMockFactory _mockFactory;
		private readonly Dictionary<string, TypeReference> _importedTypes;

		public ContractProcessor(ITypeInfo typeInfo, IAssemblyWriter assemblyWriter, IMockFactory mockFactory)
		{
			_typeInfo = typeInfo;
			_assemblyWriter = assemblyWriter;
			_mockFactory = mockFactory;
			_importedTypes = new Dictionary<string, TypeReference>();
		}

		public void ProcessAllOriginalMethodContractsWithMocks(MethodDefinition methodDef, HashSet<IMock> replaceContractMocks)
		{
			foreach (var method in _typeInfo.GetAllImplementations(methodDef))
			{
				ProcessOriginalMethodContractWithMocks(method, replaceContractMocks);
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

		public void ProcessCommonOriginalContracts(IEnumerable<ISourceMemberMock> sourceMemberMocks, HashSet<IMock> replaceContractMocks)
		{
			foreach (var mock in sourceMemberMocks)
			{
				if (mock.SourceMemberMetaData.SourceMember.ReturnType != typeof(void) && mock.SourceMemberMetaData.SourceMember.ReturnType.Module == _typeInfo.SourceType.Module)
				{
					AddReplaceContractMocks(_typeInfo.GetTypeDefinition(mock.SourceMemberMetaData.SourceMember.ReturnType), replaceContractMocks);
				}

				if (mock.SourceMemberMetaData.SourceMember.OriginalMember is MethodBase method &&
				    method.Module == _typeInfo.SourceType.Module && method.DeclaringType != null)
				{
					var typeDef = _typeInfo.GetTypeDefinition(method.DeclaringType);
					var methodRef = _assemblyWriter.ImportToSourceAsm(method);
					var methodDef = _typeInfo.GetMethod(typeDef, methodRef);
					if (methodDef != null)
					{
						ProcessAllOriginalMethodContractsWithMocks(methodDef, replaceContractMocks);
					}
				}
			}
		}

		private void ProcessOriginalMethodContractWithMocks(MethodDefinition methodDef, HashSet<IMock> replaceContractMocks)
		{
			if (methodDef.ReturnType != null && methodDef.ReturnType.FullName != "System.Void" && _typeInfo.IsInFakeModule(methodDef.ReturnType))
			{
				AddReplaceContractMocks(methodDef.ReturnType.ToTypeDefinition(), replaceContractMocks);
				methodDef.ReturnType = _assemblyWriter.ImportToSourceAsm(methodDef.ReturnType);
			}

			foreach (var parameterDef in methodDef.Parameters.Where(parameterDef => _typeInfo.IsInFakeModule(parameterDef.ParameterType)))
			{
				var typeDefinition = parameterDef.ParameterType.ToTypeDefinition();
				AddReplaceContractMocks(typeDefinition, replaceContractMocks);
				parameterDef.ParameterType = _assemblyWriter.ImportToSourceAsm(parameterDef.ParameterType);
			}
		}

		private void AddReplaceContractMocks(TypeDefinition typeDef, HashSet<IMock> replaceContractMocks)
		{
			foreach (var mockTypeDef in _typeInfo.TypeMap.GetAllParentsAndDescendants(typeDef))
			{
				var importedTypeRef = _assemblyWriter.ImportToSourceAsm(mockTypeDef);
				if (mockTypeDef.IsInterface)
				{
					replaceContractMocks.Add(_mockFactory.GetReplaceInterfaceCallMock(importedTypeRef));
				}
				else
				{
					replaceContractMocks.Add(mockTypeDef.IsValueType ?
						_mockFactory.GetReplaceValueTypeCtorMock(importedTypeRef)
						: _mockFactory.GetReplaceReferenceTypeCtorMock(importedTypeRef));
				}

				replaceContractMocks.Add(_mockFactory.GetReplaceTypeCastMock(importedTypeRef));
				TryAddImportedType(mockTypeDef, importedTypeRef);
			}
		}

		private void TryAddImportedType(TypeDefinition typeDef, TypeReference typeRef)
		{
			var contract = typeDef.ToString();
			if (!_importedTypes.ContainsKey(contract)) _importedTypes.Add(contract, typeRef);
		}
	}
}
