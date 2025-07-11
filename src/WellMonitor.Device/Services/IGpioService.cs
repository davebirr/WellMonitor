namespace WellMonitor.Device.Services
{
    public interface IGpioService
    {
        void SetRelayState(bool on);
        bool GetRelayState();
        event EventHandler<RelayStateChangedEventArgs> RelayStateChanged;
    }

    public class RelayStateChangedEventArgs : EventArgs
    {
        public bool NewState { get; set; }
    }
}
