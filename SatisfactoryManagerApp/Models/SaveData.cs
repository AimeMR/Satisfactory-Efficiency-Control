using SatisfactoryManagerApp.Graph;
using SatisfactoryManagerApp.Graph.Nodes;

namespace SatisfactoryManagerApp.Models
{
    /// <summary>
    /// Contenedor para serializar el estado completo de un proyecto.
    /// </summary>
    public class SaveData
    {
        /// <summary>Versión del formato del archivo para futuras migraciones.</summary>
        public string Version { get; set; } = "1.0";

        /// <summary>Nodos raíz del proyecto (los que no tienen padre).</summary>
        public List<NodeModel> Nodes { get; set; } = new();

        /// <summary>Todas las conexiones del proyecto (plana).</summary>
        public List<ConnectionModel> Connections { get; set; } = new();
    }
}
