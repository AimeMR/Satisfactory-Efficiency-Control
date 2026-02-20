using SatisfactoryManagerApp.Graph.Ports;

namespace SatisfactoryManagerApp.ViewModels
{
    /// <summary>
    /// Observable wrapper for InputPort or OutputPort.
    /// Used by Nodify as el anchor (pin) de un nodo.
    /// </summary>
    public class PortViewModel : ViewModelBase
    {
        private double _currentFlow;

        public Guid PortId { get; }
        public string Name { get; }
        public string ItemName { get; }
        public bool IsInput { get; }

        /// <summary>Flujo actual (ítems/minuto). Se actualiza tras RecalculateCommand.</summary>
        public double CurrentFlow
        {
            get => _currentFlow;
            set => SetField(ref _currentFlow, value);
        }

        public PortViewModel(InputPort port)
        {
            PortId = port.Id;
            Name = port.Name;
            ItemName = port.AcceptedItem?.Name ?? string.Empty;
            IsInput = true;
            _currentFlow = port.CurrentFlow;
        }

        public PortViewModel(OutputPort port)
        {
            PortId = port.Id;
            Name = port.Name;
            ItemName = port.EmittedItem?.Name ?? string.Empty;
            IsInput = false;
            _currentFlow = port.CurrentFlow;
        }

        /// <summary>Sincroniza el CurrentFlow desde el modelo de dominio.</summary>
        public void Sync(double flow) => CurrentFlow = flow;
    }
}
