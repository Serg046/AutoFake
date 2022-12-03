using System.Collections.Generic;

namespace AutoFake.Abstractions.Expression;

public interface IGetArgumentsMemberVisitor : IMemberVisitor<IReadOnlyList<IFakeArgument>>
{
}
