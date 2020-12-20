using System;
using System.Collections.Generic;

namespace AutoFake
{
    internal class FakeObjectInfo
    {
	    public FakeObjectInfo(IList<object> parameters, Type sourceType, Type fieldsType, object instance)
        {
	        Parameters = parameters ?? new object[0];
            SourceType = sourceType;
            FieldsType = fieldsType;
            Instance = instance;
        }

        public Type SourceType { get; }
        public Type FieldsType { get; }
        public object Instance { get; }
        public IList<object> Parameters { get; }
    }
}
