using AutoFake.Abstractions.Setup.Mocks;
using System.Collections.Generic;

namespace AutoFake.Abstractions.Setup;

// todo: get rid of this interface
public interface IMockCollection
{
	IList<IMock> Mocks { get; }
}
