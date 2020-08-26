using System;
using System.Collections.Generic;

namespace AutoFake
{
    internal class FakeObjectInfo : IDisposable
    {
	    private readonly IDisposable _handle;

	    public FakeObjectInfo(IDisposable handle, IList<object> parameters, Type type, object instance = null)
        {
	        _handle = handle;
	        Parameters = parameters ?? new object[0];
            Type = type;
            Instance = instance;
        }

        public bool IsDisposed { get; private set; }
        public Type Type { get; }
        public object Instance { get; }
        public IList<object> Parameters { get; }

        public void Dispose()
        {
	        _handle?.Dispose();
	        IsDisposed = true;
        }
    }
}
