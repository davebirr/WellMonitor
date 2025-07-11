using System;

namespace WellMonitor.Device.Services
{
    public class GpioService : IGpioService
    {
        private bool _relayState;
        public event EventHandler<RelayStateChangedEventArgs>? RelayStateChanged;

        public void SetRelayState(bool on)
        {
            _relayState = on;
            RelayStateChanged?.Invoke(this, new RelayStateChangedEventArgs { NewState = on });
            // TODO: Implement actual GPIO logic
        }

        public bool GetRelayState() => _relayState;
    }
}
