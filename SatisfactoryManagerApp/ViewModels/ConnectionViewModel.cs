using SatisfactoryManagerApp.Graph;

namespace SatisfactoryManagerApp.ViewModels
{
    /// <summary>
    /// Observable wrapper for a ConnectionModel (belt / pipe).
    /// Nodify binds Source and Target to PortViewModels to draw the wire.
    /// </summary>
    public class ConnectionViewModel : ViewModelBase
    {
        private bool _isBottleneck;
        private double _usageRatio;
        private double _actualFlow;

        public Guid ConnectionId { get; }
        public ConnectionModel Model { get; }

        /// <summary>Source port ViewModel (OutputPort side).</summary>
        public PortViewModel Source { get; }

        /// <summary>Target port ViewModel (InputPort side).</summary>
        public PortViewModel Target { get; }

        /// <summary>True if flow exceeds belt capacity — renders wire in red.</summary>
        public bool IsBottleneck
        {
            get => _isBottleneck;
            set => SetField(ref _isBottleneck, value);
        }

        /// <summary>0.0–1.0+ usage ratio. Used to color-code the wire (green/orange/red).</summary>
        public double UsageRatio
        {
            get => _usageRatio;
            set => SetField(ref _usageRatio, value);
        }

        /// <summary>Actual ítems/minuto flowing through this connection.</summary>
        public double ActualFlow
        {
            get => _actualFlow;
            set => SetField(ref _actualFlow, value);
        }

        /// <summary>Human-readable capacity label, e.g. "Mk.3 (270/min)".</summary>
        public string CapacityLabel =>
            $"Mk.{(int)Model.BeltMk} ({Model.MaxCapacity}/min)";

        public ConnectionViewModel(ConnectionModel model, PortViewModel source, PortViewModel target)
        {
            Model = model;
            ConnectionId = model.Id;
            Source = source;
            Target = target;
            Sync();
        }

        /// <summary>Pulls updated values from the domain model after recalculation.</summary>
        public void Sync()
        {
            ActualFlow = Model.ActualFlow;
            IsBottleneck = Model.IsBottleneck;
            UsageRatio = Model.UsageRatio;
        }
    }
}
