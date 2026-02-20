using SatisfactoryManagerApp.Graph.Ports;

namespace SatisfactoryManagerApp.Graph.Nodes
{
    /// <summary>
    /// Clase base para todos los elementos que se pueden colocar en el lienzo.
    /// Define la estructura mínima que cualquier nodo del grafo debe tener.
    /// </summary>
    public abstract class NodeModel
    {
        /// <summary>Identificador único del nodo.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>Nombre visible en el lienzo.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Id del nodo padre (FactoryGroupNode) que contiene a este nodo.
        /// Null si el nodo vive en el nivel raíz del lienzo.
        /// </summary>
        public Guid? ParentGroupId { get; set; } = null;

        /// <summary>Puertos de entrada: cintas/tuberías que llegan a este nodo.</summary>
        public List<InputPort> Inputs { get; init; } = new();

        /// <summary>Puertos de salida: cintas/tuberías que salen de este nodo.</summary>
        public List<OutputPort> Outputs { get; init; } = new();

        // ── Posición en el lienzo (para la futura UI) ─────────────────────────
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Añade un puerto de entrada al nodo y lo vincula con el OwnerNodeId.
        /// </summary>
        protected InputPort AddInput(string name, Models.Item? item = null)
        {
            var port = new InputPort { Name = name, AcceptedItem = item, OwnerNodeId = Id };
            Inputs.Add(port);
            return port;
        }

        /// <summary>
        /// Añade un puerto de salida al nodo y lo vincula con el OwnerNodeId.
        /// </summary>
        protected OutputPort AddOutput(string name, Models.Item? item = null)
        {
            var port = new OutputPort { Name = name, EmittedItem = item, OwnerNodeId = Id };
            Outputs.Add(port);
            return port;
        }
    }
}
