using System;

namespace Sut
{
    public class SystemUnderTest
    {
        public void SimpleMethod()
        {
        }

        internal void InternalMethod()
        {
        }

        public DateTime GetCurrentDate() => DateTime.Now;
    }
}
