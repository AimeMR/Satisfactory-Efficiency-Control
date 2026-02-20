using SatisfactoryManagerApp.Models;

namespace SatisfactoryManagerApp.Graph.Ports
{
    /// <summary>
    /// Representa un punto de salida en un nodo.
    /// Las cintas/tuberías salen DESDE este puerto para llevar ítems al siguiente nodo.
    /// </summary>
    public class OutputPort
    {
        /// <summary>Identificador único del puerto.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>Nombre visible (ej. "Placas de Hierro", "Salida 1").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// El ítem que este puerto emite.
        /// Null significa que el puerto emite cualquier ítem
        /// (útil para nodos logísticos como Splitter).
        /// </summary>
        public Item? EmittedItem { get; set; }

        /// <summary>
        /// Flujo actual que este puerto está produciendo (ítems/minuto).
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
