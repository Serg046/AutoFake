using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Xunit;
using Moq;

namespace AutoFake.IntegrationTests
{
    public class SetupContextMultithreadingTests : IDisposable
    {
        private const int PARALLEL_INVOKE_ITERATION_COUNT = 10000;

        private readonly Mock<IFakeArgumentChecker> _checkerMock;

        public SetupContextMultithreadingTests()
        {
            _checkerMock = new Mock<IFakeArgumentChecker>();
            _checkerMock.Setup(c => c.Check(It.IsAny<object>())).Returns((int arg) => arg == 7);
        }

        public void Dispose()
        {
            RemoveCheckerIfExists();

            try
            {
                var setupContext = new SetupContext();
                while (true)
                    setupContext.Dispose();
            }
            catch (MissedSetupContextException)
            {
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void RemoveCheckerIfExists()
        {
            var setupContext = new SetupContext();
            if (setupContext.IsCheckerSet)
                setupContext.PopChecker();
        }

        //----------------------------------------------------------------------------------------------------------------

        [Fact(Skip = "It takes a long time")]
        public void SetCurrentChecker_ParallelInvoke_ThreadSafe()
        {
            using (new SetupContext())
            {
                for (var i = 0; i < PARALLEL_INVOKE_ITERATION_COUNT; i++)
                {
                    var exceptionCount = 0;

                    var t1 = Task.Run(() => RunTestTask<SetupContextStateException>(ref exceptionCount,
                        () => SetupContext.SetCurrentChecker(_checkerMock.Object)));
                    var t2 = Task.Run(() => RunTestTask<SetupContextStateException>(ref exceptionCount,
                        () => SetupContext.SetCurrentChecker(_checkerMock.Object)));

                    Task.WaitAll(t1, t2);
                    Assert.True(exceptionCount == 1, $"Failed on {i} iteration. Exception count = {exceptionCount}.");
                    RemoveCheckerIfExists();
                }
            }
        }
        
        [Fact(Skip = "It takes a long time")]
        public void PopChecker_ParallelInvoke_ThreadSafe()
        {
            var setupContext = new SetupContext();
            for (var i = 0; i < PARALLEL_INVOKE_ITERATION_COUNT; i++)
            {
                SetupContext.SetCurrentChecker(_checkerMock.Object);
                var exceptionCount = 0;

                var t1 = Task.Run(() => RunTestTask<SetupContextStateException>(ref exceptionCount, () => setupContext.PopChecker()));
                var t2 = Task.Run(() => RunTestTask<SetupContextStateException>(ref exceptionCount, () => setupContext.PopChecker()));

                Task.WaitAll(t1, t2);
                Assert.True(exceptionCount == 1, $"Failed on {i} iteration. Exception count = {exceptionCount}.");
                RemoveCheckerIfExists();
            }
        }
        
        [Fact(Skip = "It takes a long time")]
        public void Dispose_ParallelInvoke_ThreadSafe()
        {
            for (var i = 0; i < PARALLEL_INVOKE_ITERATION_COUNT; i++)
            {
                var setupContext = new SetupContext();
                var exceptionCount = 0;

                var t1 = Task.Run(() => RunTestTask<MissedSetupContextException>(ref exceptionCount, () => setupContext.Dispose()));
                var t2 = Task.Run(() => RunTestTask<MissedSetupContextException>(ref exceptionCount, () => setupContext.Dispose()));

                Task.WaitAll(t1, t2);
                Assert.True(exceptionCount == 1, $"Failed on {i} iteration. Exception count = {exceptionCount}.");
            }
        }

        private void RunTestTask<T>(ref int exceptionCount, Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                Interlocked.Increment(ref exceptionCount);
            }
        }

        //----------------------------------------------------------------------------------------------------------------
        
        [Fact(Skip = "It takes a long time")]
        public void SetCurrentChecker_DisposeInvocation_ThreadSafe()
        {
            var eventRecorder = new SingleStringEventRecorder();
            SetupContext.SetEventRecorder(eventRecorder);

            for (var i = 0; i < PARALLEL_INVOKE_ITERATION_COUNT; i++)
            {
                var setupContext = new SetupContext();
                var t1 = Task.Run(() =>
                {
                    try
                    {
                        SetupContext.SetCurrentChecker(_checkerMock.Object);
                    }
                    catch (MissedSetupContextException)
                    {
                        if (eventRecorder.Events.StartsWith("SetCurrentChecker"))
                            throw new Exception($"Failed on {i} iteration.");
                    }
                });
                var t2 = Task.Run(() => setupContext.Dispose());

                Task.WaitAll(t1, t2);
                Dispose();
                eventRecorder.Reset();
            }
        }
        
        [Fact(Skip = "It takes a long time")]
        public void PopChecker_SetCurrentCheckerInvocation_ThreadSafe()
        {
            var setupContext = new SetupContext();
            var eventRecorder = new SingleStringEventRecorder();
            SetupContext.SetEventRecorder(eventRecorder);

            var currentChecker = _checkerMock.Object;
            var failChecker = Mock.Of<IFakeArgumentChecker>();
            Assert.False(ReferenceEquals(currentChecker, failChecker));

            for (var i = 0; i < PARALLEL_INVOKE_ITERATION_COUNT; i++)
            {
                SetupContext.SetCurrentChecker(currentChecker);
                var t1 = Task.Run(() => setupContext.PopChecker());
                var t2 = Task.Run(() =>
                {
                    try
                    {
                        SetupContext.SetCurrentChecker(failChecker);
                    }
                    catch (SetupContextStateException)
                    {
                        if (eventRecorder.Events.StartsWith("SetCurrentCheckerPopChecker"))
                            throw new Exception($"Failed on {i} iteration.");
                    }
                });

                Task.WaitAll(t1, t2);
                RemoveCheckerIfExists();
                eventRecorder.Reset();
            }
        }
        
        [Fact(Skip = "It takes a long time")]
        public void PopChecker_DisposeInvocation_ThreadSafe()
        {
            const string invocationOrder = "SetCurrentCheckerPopChecker";

            var eventRecorder = new SingleStringEventRecorder();
            SetupContext.SetEventRecorder(eventRecorder);

            for (var i = 0; i < PARALLEL_INVOKE_ITERATION_COUNT; i++)
            {
                var setupContext = new SetupContext();
                SetupContext.SetCurrentChecker(_checkerMock.Object);
                var t1 = Task.Run(() =>
                {
                    try
                    {
                        setupContext.PopChecker();
                    }
                    catch (MissedSetupContextException)
                    {
                        if (eventRecorder.Events.StartsWith(invocationOrder))
                            throw new Exception($"Failed on {i} iteration.");
                    }
                    catch (SetupContextStateException)
                    {
                        if (eventRecorder.Events.StartsWith(invocationOrder))
                            throw new Exception($"Failed on {i} iteration.");
                    }
                });
                var t2 = Task.Run(() => setupContext.Dispose());

                Task.WaitAll(t1, t2);
                Dispose();
                eventRecorder.Reset();
            }
        }
    }
}
