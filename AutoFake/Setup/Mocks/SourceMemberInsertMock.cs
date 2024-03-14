using System;
using System.Collections.Generic;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks;

internal class SourceMemberInsertMock : ISourceMemberInsertMock
{
	private readonly Func<ISourceMemberMetaData, ISourceMemberInsertMockInjector> _createInjector;
	private readonly IInsertMock.Location _location;
	private readonly ITypeInfo _typeInfo;
	private readonly IOptions _options;
	private FieldDefinition? _closureField;

	public SourceMemberInsertMock(
		ISourceMemberMetaData sourceMemberMetaData,
		Func<ISourceMemberMetaData, ISourceMemberInsertMockInjector> createInjector,
		Action closure, IInsertMock.Location location,
		ITypeInfo typeInfo,
		IOptions options)
	{
		SourceMemberMetaData = sourceMemberMetaData;
		_createInjector = createInjector;
		_location = location;
		_typeInfo = typeInfo;
		_options = options;
		Closure = closure;
	}

	public ISourceMemberMetaData SourceMemberMetaData { get; }
	public Action Closure { get; }

	public bool IsSourceInstruction(MethodDefinition method, Instruction instruction, IEnumerable<IGenericArgument> genericArguments)
	{
		return SourceMemberMetaData.SourceMember.IsSourceInstruction(instruction, genericArguments);
	}

	public void BeforeInjection(MethodDefinition method)
	{
		SourceMemberMetaData.BeforeInjection(method);
		_closureField = SourceMemberMetaData.PrePostProcessor.GenerateField(
			$"{method.Name}InsertCallback{Guid.NewGuid()}", Closure.GetType());
	}

	public void Inject(IEmitter emitter, Instruction instruction)
	{
		if (_closureField == null) throw new InvalidOperationException("Closure field should be set");
		var module = emitter.Body.Method.Module;
		var closureRef = _options.IsMultipleAssembliesMode
			? module.ImportReference(_closureField)
			: _closureField;
		var injector = _createInjector(SourceMemberMetaData);
		injector.Inject(emitter, instruction, closureRef, _location);
	}

	public void AfterInjection(IEmitter emitter)
	{
		SourceMemberMetaData.AfterInjection(emitter);
	}

	public void Initialize(Type? type, string rewriteMethodName)
	{
		SourceMemberMetaData.Initialize(type, rewriteMethodName);
		InsertMock.InitializeClosure(type, _closureField, Closure);
	}
}
