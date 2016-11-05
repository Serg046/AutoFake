namespace AutoFake.IntegrationTests
{
    internal class SingleStringEventRecorder : IEventRecorder
    {
        private string _events = string.Empty;

        public void Record(string @event) => _events += @event;

        public string Events => _events;

        public void Reset() => _events = string.Empty;
    }
}
