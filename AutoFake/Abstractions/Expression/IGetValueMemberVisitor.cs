using System;

namespace AutoFake.Abstractions.Expression;

public interface IGetValueMemberVisitor : IExecutableMemberVisitor<(Type Type, object? Value)>
{
}
