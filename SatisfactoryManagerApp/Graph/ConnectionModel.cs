using SatisfactoryManagerApp.Models;

namespace SatisfactoryManagerApp.Graph
{
    /// <summary>
    /// Representa una cinta transportadora o tubería que une dos puertos.
    /// Va desde un OutputPort (origen) hasta un InputPort (destino).
    /// </summary>
    public class ConnectionModel
    {
        /// <summary>Identificador único de la conexión.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        // ── Mapeo origen → destino ────────────────────────────────────────────

        /// <summary>Id del nodo de origen (dueño del OutputPort).</summary>
        public Guid SourceNodeId { get; set; }

        /// <summary>Id del OutputPort específico de origen.</summary>
        public Guid SourcePortId { get; set; }

        /// <summary>Id del nodo destino (dueño del InputPort).</summary>
        public Guid TargetNodeId { get; set; }

        /// <summary>Id del InputPort específico de destino.</summary>
        public Guid TargetPortId { get; set; }

        // ── Ítem transportado ─────────────────────────────────────────────────

        /// <summary>El ítem que fluye por esta conexión.</summary>
        public Item? Item { get; set; }

        // ── Capacidad de la cinta ─────────────────────────────────────────────

        /// <summary>
        /// Nivel de la cinta transportadora / tubería. Determina la capacidad máxima.
        /// </summary>
        public ConveyorBeltMk BeltMk { get; set; } = ConveyorBeltMk.Mk1;

        /// <summary>Capacidad máxima en ítems/minuto según el nivel de la cinta.</summary>
        public double MaxCapacity => BeltMk switch
        {
            ConveyorBeltMk.Mk1 => 60,
            ConveyorBeltMk.Mk2 => 120,
            ConveyorBeltMk.Mk3 => 270,
            ConveyorBeltMk.Mk4 => 480,
            ConveyorBeltMk.Mk5 => 780,
            ConveyorBeltMk.Pipe_Mk1 => 300,   // Tuberías para fluidos
            ConveyorBeltMk.Pipe_Mk2 => 600,
            _ => double.MaxValue
        };

        // ── Estado calculado (actualizado por FlowCalculator) ─────────────────

        /// <summary>Flujo real que pasa por esta conexión (ítems/minuto).</summary>
        public double ActualFlow { get; set; } = 0.0;

        /// <summary>
        /// Porcentaje de uso de la cinta (0.0 a 1.0+).
        /// Si supera 1.0, la cinta está saturada.
        /// </summary>
        public double UsageRatio => MaxCapacity > 0 ? ActualFlow / MaxCapacity : 0;

        /// <summary>
        /// True si el flujo supera la capacidad máxima de la cinta.
        /// FlowCalculator limita ActualFlow a MaxCapacity y levanta este flag.
        /// </summary>
        public bool IsBottleneck { get; set; } = false;

        /// <summary>
        /// True si esta conexión cruza el perímetro de un FactoryGroupNode.
        /// Usado por FactoryGroupNode.SyncBoundaryPorts() para crear puertos externos.
        /// </summary>
        public bool IsCrossBoundary { get; set; } = false;
    }

    /// <summary>Niveles de cinta transportadora y tubería disponibles en Satisfactory.</summary>
    public enum ConveyorBeltMk
    {
        Mk1 = 1,
        Mk2 = 2,
        Mk3 = 3,
        Mk4 = 4,
        Mk5 = 5,
        Pipe_Mk1 = 10,
        Pipe_Mk2 = 11
    }
}
