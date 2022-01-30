using System.Collections.Generic;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake.Abstractions
{
	internal interface IContractProcessor
	{
		void ProcessAllOriginalMethodContractsWithMocks(MethodDefinition methodDef, HashSet<IMock> replaceContractMocks);
		void ProcessOriginalMethodContract(MethodDefinition methodDef);
		void ProcessCommonOriginalContracts(IEnumerable<ISourceMemberMock> sourceMemberMocks, HashSet<IMock> replaceContractMocks);
	}
}
