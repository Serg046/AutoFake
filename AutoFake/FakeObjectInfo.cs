using System;
using System.Collections.Generic;

namespace AutoFake
{
    internal class FakeObjectInfo
    {
	    public FakeObjectInfo(IList<object> parameters, Type type, object instance = null)
        {
	        Parameters = parameters ?? new object[0];
            Type = type;
            Instance = instance;
        }

        public Type Type { get; }
        public object Instance { get; }
        public IList<object> Parameters { get; }
    }
}
