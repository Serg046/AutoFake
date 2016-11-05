using System;
using System.Threading;
using AutoFake.Exceptions;

namespace AutoFake.Setup
{
    internal class SetupContext : IDisposable
    {
#pragma warning disable 169
        private static IEventRecorder _eventRecorder;
#pragma warning restore 169
        private static readonly object _syncObject = new object();

        private static volatile IFakeArgumentChecker _currentChecker;
        private static volatile bool _isCheckerSet;
        private static volatile int _instanceCount;

        public SetupContext()
        {
#pragma warning disable 420
            Interlocked.Increment(ref _instanceCount);
#pragma warning restore 420
        }

        internal bool IsCheckerSet => _isCheckerSet;

#if DEBUG
        internal static void SetEventRecorder(IEventRecorder eventRecorder) => _eventRecorder = eventRecorder;
#endif

        internal static void SetCurrentChecker(IFakeArgumentChecker checker)
        {
            lock (_syncObject)
            {
#if DEBUG
                _eventRecorder?.Record(nameof(SetCurrentChecker));
#endif
                if (_isCheckerSet)
                    throw new SetupContextStateException("Checker is already set");
                ThrowIfNoInstance();

                _isCheckerSet = true;
                _currentChecker = checker;
            }
        }

        public IFakeArgumentChecker PopChecker()
        {
            lock (_syncObject)
            {
#if DEBUG
                _eventRecorder?.Record(nameof(PopChecker));
#endif
                ThrowIfNoInstance();
                if (!IsCheckerSet)
                    throw new SetupContextStateException("Checker is not set");

                _isCheckerSet = false;
                return _currentChecker;
            }
        }

        private static void ThrowIfNoInstance()
        {
            if (_instanceCount < 1)
                throw new MissedSetupContextException("There is no active SetupContext instance.");
        }

        public void Dispose()
        {
            lock (_syncObject)
            {
#if DEBUG
                _eventRecorder?.Record(nameof(Dispose));
#endif
                ThrowIfNoInstance();
                _instanceCount--;
                _isCheckerSet = false;
            }
        }
    }
}
