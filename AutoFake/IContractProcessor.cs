using System.Collections.Generic;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
	internal interface IContractProcessor
	{
		void ProcessAllOriginalMethodContractsWithMocks(MethodDefinition methodDef, HashSet<IMock> replaceContractMocks);
		void ProcessOriginalMethodContract(MethodDefinition methodDef);
		void ProcessCommonOriginalContracts(IEnumerable<SourceMemberMock> sourceMemberMocks, HashSet<IMock> replaceContractMocks);
	}
}
