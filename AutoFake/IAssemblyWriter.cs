using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
	internal interface IAssemblyWriter
	{
		TypeReference ImportToSourceAsm(Type type);
		FieldReference ImportToSourceAsm(FieldInfo field);
		MethodReference ImportToSourceAsm(MethodBase method);
		TypeReference ImportToSourceAsm(TypeReference type);
		TypeReference ImportToFieldsAsm(Type type);
		FieldReference ImportToFieldsAsm(FieldInfo field);
		MethodReference ImportToFieldsAsm(MethodBase method);
		void AddField(FieldDefinition field);
		bool TryAddAffectedAssembly(AssemblyDefinition assembly);
		FakeObjectInfo CreateFakeObject(IEnumerable<IMock> mocks, object?[] dependencies);
	}
}