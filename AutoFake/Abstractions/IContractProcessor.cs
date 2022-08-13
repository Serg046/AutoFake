using System.Collections.Generic;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake.Abstractions
{
	internal interface IContractProcessor
	{
		void ProcessAllOriginalMethodContractsWithMocks(MethodDefinition methodDef);
		void ProcessOriginalMethodContract(MethodDefinition methodDef);
		void ProcessCommonOriginalContracts(IEnumerable<ISourceMemberMock> sourceMemberMocks);
		void AddReplaceContractMocks(TypeDefinition typeDef);
	}
}
