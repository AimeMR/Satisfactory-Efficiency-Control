using SatisfactoryManagerApp.Models;

namespace SatisfactoryManagerApp.Graph.Ports
{
    /// <summary>
    /// Representa un punto de entrada en un nodo.
    /// Las cintas/tuberías se conectan HACIA este puerto para suministrar ítems.
    /// </summary>
    public class InputPort
    {
        /// <summary>Identificador único del puerto.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>Nombre visible (ej. "Hierro", "Entrada 1").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// El ítem que este puerto acepta.
        /// Null significa que el puerto acepta cualquier ítem
        /// (útil para nodos logísticos como Merger).
        /// </summary>
        public Item? AcceptedItem { get; set; }

        /// <summary>
        /// Flujo actual que está llegando a este puerto (ítems/minuto).
        /// Calculado y actualizado por FlowCalculator en cada pasada.
        /// </summary>
        public double CurrentFlow { get; set; } = 0.0;

        /// <summary>
        /// Referencia al nodo dueño de este puerto.
        /// Se asigna cuando el puerto se añade a un nodo.
        /// </summary>
        public Guid OwnerNodeId { get; set; }
    }
}
