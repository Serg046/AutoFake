using System;

namespace AutoFake.Abstractions.Expression;

public interface IMemberVisitorFactory
{
	T GetMemberVisitor<T>();
	IGetValueMemberVisitor GetValueMemberVisitor(object? instance);
	ITargetMemberVisitor<T> GetTargetMemberVisitor<T>(IExecutableMemberVisitor<T> requestedVisitor, Type targetType);
}
