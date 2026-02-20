using SatisfactoryManagerApp.Graph.Ports;

namespace SatisfactoryManagerApp.Graph.Nodes
{
    /// <summary>
    /// Contenedor jerárquico que agrupa nodos hijos en un contexto navegable independiente.
    ///
    /// AGRUPACIÓN MULTINIVEL:
    /// Un FactoryGroupNode puede contener tanto MachineNodes como otros FactoryGroupNodes,
    /// permitiendo una jerarquía arbitrariamente profunda:
    ///
    ///   FactoryGroupNode (Fábrica Principal)
    ///   ├── FactoryGroupNode (Línea de Hierro)   ← 5 máquinas para hierro
    ///   │   ├── MachineNode (Minero)
    ///   │   └── MachineNode (Fundidora x4)
    ///   ├── FactoryGroupNode (Línea de Cobre)
    ///   └── MachineNode (Ensambladora Final)
    ///
    /// Los puertos externos del grupo se sincronizan automáticamente mediante
    /// SyncBoundaryPorts(), reflejando las conexiones que cruzan su perímetro.
    /// </summary>
    public class FactoryGroupNode : NodeModel
    {
        /// <summary>Descripción visible del propósito del grupo.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Todos los nodos que viven dentro de este grupo.
        /// Puede contener MachineNode, SplitterNode, MergerNode u otros FactoryGroupNode.
        /// </summary>
        public List<NodeModel> Children { get; init; } = new();

        // ── Gestión de hijos ──────────────────────────────────────────────────

        /// <summary>
        /// Añade un nodo al grupo, estableciendo su ParentGroupId.
        /// </summary>
        public void AddChild(NodeModel node)
        {
            node.ParentGroupId = Id;
            Children.Add(node);
        }

        /// <summary>
        /// Elimina un nodo del grupo y limpia su referencia al padre.
        /// </summary>
        public void RemoveChild(NodeModel node)
        {
            if (Children.Remove(node))
                node.ParentGroupId = null;
        }

        // ── Sincronización de puertos externos ────────────────────────────────

        /// <summary>
        /// Regenera los puertos externos de este grupo a partir de las conexiones
        /// que cruzan su perímetro (ConnectionModel.IsCrossBoundary == true).
        ///
        /// Llamado por FlowCalculator después de calcular todos los nodos hijos.
        /// </summary>
        /// <param name="allConnections">Lista completa de conexiones del grafo.</param>
        /// <param name="allNodes">Diccionario de todos los nodos del grafo (Id → NodeModel).</param>
        public void SyncBoundaryPorts(
            IEnumerable<ConnectionModel> allConnections,
            IReadOnlyDictionary<Guid, NodeModel> allNodes)
        {
            Inputs.Clear();
            Outputs.Clear();

            var childIds = GetAllDescendantIds();

            foreach (var conn in allConnections)
            {
                bool sourceIsChild = allNodes.TryGetValue(conn.SourceNodeId, out var srcNode)
                                     && childIds.Contains(srcNode.Id);
                bool targetIsChild = allNodes.TryGetValue(conn.TargetNodeId, out var tgtNode)
                                     && childIds.Contains(tgtNode.Id);

                // Conexión entra al grupo desde fuera → puerto de ENTRADA en el grupo
                if (!sourceIsChild && targetIsChild)
                {
                    var port = new InputPort
                    {
                        Name = $"Entrada desde {srcNode?.Name ?? "externo"}",
                        AcceptedItem = conn.Item,
                        OwnerNodeId = Id,
                        CurrentFlow = conn.ActualFlow
                    };
                    Inputs.Add(port);
                    conn.IsCrossBoundary = true;
                }
                // Conexión sale del grupo hacia fuera → puerto de SALIDA en el grupo
                else if (sourceIsChild && !targetIsChild)
                {
                    var port = new OutputPort
                    {
                        Name = $"Salida hacia {tgtNode?.Name ?? "externo"}",
                        EmittedItem = conn.Item,
                        OwnerNodeId = Id,
                        CurrentFlow = conn.ActualFlow
                    };
                    Outputs.Add(port);
                    conn.IsCrossBoundary = true;
                }
            }
        }

        /// <summary>
        /// Obtiene los Ids de todos los descendientes (hijos, nietos, etc.) del grupo.
        /// Útil para determinar si un nodo pertenece a este grupo en cualquier nivel.
        /// </summary>
        public HashSet<Guid> GetAllDescendantIds()
        {
            var result = new HashSet<Guid>();
            CollectDescendants(this, result);
            return result;
        }

        private static void CollectDescendants(FactoryGroupNode group, HashSet<Guid> result)
        {
            foreach (var child in group.Children)
            {
                result.Add(child.Id);
                if (child is FactoryGroupNode subGroup)
                    CollectDescendants(subGroup, result);
            }
        }

        /// <summary>
        /// Obtiene una lista plana de todos los MachineNodes dentro de este grupo
        /// (incluyendo los de subgrupos anidados), para cálculo de energía total.
        /// </summary>
        public IEnumerable<MachineNode> GetAllMachines()
        {
            foreach (var child in Children)
            {
                if (child is MachineNode m) yield return m;
                else if (child is FactoryGroupNode g)
                    foreach (var m2 in g.GetAllMachines())
                        yield return m2;
            }
        }

        /// <summary>
        /// Consumo total de energía de todas las máquinas dentro del grupo (MW).
        /// </summary>
        public double TotalPowerMW => GetAllMachines().Sum(m => m.ActualPowerMW);
    }
}
