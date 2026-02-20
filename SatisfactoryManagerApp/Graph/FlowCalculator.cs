using SatisfactoryManagerApp.Graph.Nodes;

namespace SatisfactoryManagerApp.Graph
{
    /// <summary>
    /// Motor de cálculo que recorre el grafo y actualiza todos los flujos de ítems/minuto.
    ///
    /// ALGORITMO (en orden de ejecución):
    ///   Paso 0  – Reseteo de todos los flujos.
    ///   Paso 1  – Detección de ciclos (DFS). Aborta subgrafos circulares.
    ///   Paso 2  – Generación: calcula producción de nodos fuente (mineros, extractores).
    ///   Paso 3  – Propagación en orden topológico (Kahn).
    ///   Paso 4  – Evaluación de eficiencia por máquina (multi-ingrediente).
    ///   Paso 5  – Sincronización de puertos externos de grupos anidados.
    /// </summary>
    public class FlowCalculator
    {
        // ── Estado del último cálculo ─────────────────────────────────────────

        /// <summary>Nodos que forman parte de un ciclo detectado en el último cálculo.</summary>
        public HashSet<Guid> CyclicNodeIds { get; private set; } = new();

        /// <summary>Conexiones que actuaron como cuello de botella en el último cálculo.</summary>
        public HashSet<Guid> BottleneckConnectionIds { get; private set; } = new();

        // ─────────────────────────────────────────────────────────────────────
        // PUNTO DE ENTRADA PRINCIPAL
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Recalcula todos los flujos del grafo.
        /// </summary>
        /// <param name="nodes">Todos los nodos del lienzo (incluyendo grupos raíz).</param>
        /// <param name="connections">Todas las conexiones del lienzo.</param>
        public void Calculate(IList<NodeModel> nodes, IList<ConnectionModel> connections)
        {
            // Índices para acceso rápido O(1)
            var nodeById = nodes.ToDictionary(n => n.Id);
            var connsBySource = connections
                .GroupBy(c => c.SourceNodeId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var connsByTarget = connections
                .GroupBy(c => c.TargetNodeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ── Paso 0: Reseteo ───────────────────────────────────────────────
            ResetFlows(nodes, connections);

            // ── Paso 1: Detección de ciclos ───────────────────────────────────
            CyclicNodeIds = DetectCycles(nodes, connsBySource);

            // ── Paso 2 + 3 + 4: Orden topológico y propagación ───────────────
            var order = TopologicalSort(nodes, connsBySource, connsByTarget, CyclicNodeIds);
            PropagateFlows(order, nodeById, connections, connsBySource, connsByTarget);

            // ── Paso 5: Sincronizar puertos externos de grupos (todos los niveles) ────
            // CollectAllGroups desciende recursivamente para incluir sub-grupos anidados.
            foreach (var group in CollectAllGroups(nodes))
                group.SyncBoundaryPorts(connections, nodeById);

            // Marcar bottlenecks
            BottleneckConnectionIds = connections
                .Where(c => c.IsBottleneck)
                .Select(c => c.Id)
                .ToHashSet();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PASO 0 — RESETEO
        // ─────────────────────────────────────────────────────────────────────

        private static void ResetFlows(IList<NodeModel> nodes, IList<ConnectionModel> connections)
        {
            foreach (var node in nodes)
            {
                foreach (var p in node.Inputs) p.CurrentFlow = 0;
                foreach (var p in node.Outputs) p.CurrentFlow = 0;
                if (node is MachineNode m) m.Efficiency = 1.0;
            }
            foreach (var conn in connections)
            {
                conn.ActualFlow = 0;
                conn.IsBottleneck = false;
                conn.IsCrossBoundary = false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PASO 1 — DETECCIÓN DE CICLOS (DFS con 3 colores)
        // ─────────────────────────────────────────────────────────────────────

        private enum VisitState { White, Gray, Black }

        private static HashSet<Guid> DetectCycles(
            IList<NodeModel> nodes,
            Dictionary<Guid, List<ConnectionModel>> connsBySource)
        {
            var state = nodes.ToDictionary(n => n.Id, _ => VisitState.White);
            var cyclic = new HashSet<Guid>();

            foreach (var node in nodes)
                if (state[node.Id] == VisitState.White)
                    DfsVisit(node.Id, state, connsBySource, cyclic);

            return cyclic;
        }

        private static void DfsVisit(
            Guid nodeId,
            Dictionary<Guid, VisitState> state,
            Dictionary<Guid, List<ConnectionModel>> connsBySource,
            HashSet<Guid> cyclic)
        {
            state[nodeId] = VisitState.Gray; // En proceso

            if (connsBySource.TryGetValue(nodeId, out var outConns))
            {
                foreach (var conn in outConns)
                {
                    var neighborId = conn.TargetNodeId;
                    if (!state.ContainsKey(neighborId)) continue;

                    if (state[neighborId] == VisitState.Gray)
                    {
                        // Arco de retroceso → ciclo detectado
                        cyclic.Add(nodeId);
                        cyclic.Add(neighborId);
                    }
                    else if (state[neighborId] == VisitState.White)
                    {
                        DfsVisit(neighborId, state, connsBySource, cyclic);
                        if (cyclic.Contains(neighborId)) cyclic.Add(nodeId);
                    }
                }
            }

            state[nodeId] = VisitState.Black; // Finalizado
        }

        // ─────────────────────────────────────────────────────────────────────
        // PASO 2/3 — ORDEN TOPOLÓGICO (Algoritmo de Kahn)
        // ─────────────────────────────────────────────────────────────────────

        private static List<NodeModel> TopologicalSort(
            IList<NodeModel> nodes,
            Dictionary<Guid, List<ConnectionModel>> connsBySource,
            Dictionary<Guid, List<ConnectionModel>> connsByTarget,
            HashSet<Guid> cyclic)
        {
            // Grado de entrada de cada nodo (excluyendo nodos cíclicos)
            var inDegree = nodes
                .Where(n => !cyclic.Contains(n.Id))
                .ToDictionary(n => n.Id, n =>
                    connsByTarget.TryGetValue(n.Id, out var inc)
                        ? inc.Count(c => !cyclic.Contains(c.SourceNodeId))
                        : 0);

            var queue = new Queue<NodeModel>(nodes.Where(n => !cyclic.Contains(n.Id) && inDegree[n.Id] == 0));
            var result = new List<NodeModel>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                if (!connsBySource.TryGetValue(current.Id, out var outConns)) continue;

                foreach (var conn in outConns)
                {
                    if (cyclic.Contains(conn.TargetNodeId)) continue;
                    if (!inDegree.ContainsKey(conn.TargetNodeId)) continue;

                    inDegree[conn.TargetNodeId]--;
                    if (inDegree[conn.TargetNodeId] == 0)
                        queue.Enqueue(nodes.First(n => n.Id == conn.TargetNodeId));
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PASO 3/4 — PROPAGACIÓN Y EVALUACIÓN DE EFICIENCIA
        // ─────────────────────────────────────────────────────────────────────

        private static void PropagateFlows(
            List<NodeModel> orderedNodes,
            Dictionary<Guid, NodeModel> nodeById,
            IList<ConnectionModel> allConnections,
            Dictionary<Guid, List<ConnectionModel>> connsBySource,
            Dictionary<Guid, List<ConnectionModel>> connsByTarget)
        {
            // Nota: los nodos cíclicos fueron excluidos del orden topológico, por lo que
            // sus puertos NO están en estos diccionarios. Las conexiones hacia nodos cíclicos
            // fallan silenciosamente en TryGetValue — comportamiento correcto e intencional.
            var portById_In = orderedNodes.SelectMany(n => n.Inputs).ToDictionary(p => p.Id);
            var portById_Out = orderedNodes.SelectMany(n => n.Outputs).ToDictionary(p => p.Id);

            foreach (var node in orderedNodes)
            {
                // ── Paso 2: Nodos fuente (sin entradas activas) ───────────────
                if (!connsByTarget.TryGetValue(node.Id, out var inConns) || inConns.Count == 0)
                {
                    // Es un nodo fuente: calcular su producción base
                    CalculateSourceOutput(node, portById_Out);
                }

                // ── Paso 4: Evaluación de eficiencia para máquinas ───────────
                if (node is MachineNode machine)
                {
                    ApplyMachineEfficiency(machine, portById_In);
                }

                // ── Paso 3: Propagación a las conexiones salientes ────────────
                if (!connsBySource.TryGetValue(node.Id, out var outConns)) continue;

                foreach (var conn in outConns)
                {
                    if (!portById_Out.TryGetValue(conn.SourcePortId, out var srcPort)) continue;

                    double flowToSend = srcPort.CurrentFlow;

                    // ── Detección de cuello de botella ────────────────────────
                    if (flowToSend > conn.MaxCapacity)
                    {
                        conn.IsBottleneck = true;
                        flowToSend = conn.MaxCapacity;
                    }

                    conn.ActualFlow = flowToSend;

                    // Acumular en el puerto de entrada del nodo destino
                    if (portById_In.TryGetValue(conn.TargetPortId, out var tgtPort))
                        tgtPort.CurrentFlow += flowToSend;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Para nodos fuente (Mineros, Extractores) que no tienen entradas,
        /// calcula y asigna la producción en sus puertos de salida.
        /// Los MachineNode con receta calculan su output basado en la receta + overclock.
        /// </summary>
        private static void CalculateSourceOutput(
            NodeModel node,
            Dictionary<Guid, Ports.OutputPort> portById_Out)
        {
            if (node is not MachineNode machine || machine.ActiveRecipe is null) return;

            var outputs = machine.ActiveRecipe.Ingredients.Where(i => !i.IsInput).ToList();
            foreach (var ingredient in outputs)
            {
                var matchingPort = machine.Outputs
                    .FirstOrDefault(p => p.EmittedItem?.Id == ingredient.Item?.Id);
                if (matchingPort is null) continue;

                // ítems/minuto base × factor de overclock
                double baseRate = ingredient.ItemsPerMinute * machine.OverclockFactor;
                matchingPort.CurrentFlow = baseRate;
            }
        }

        /// <summary>
        /// Evalúa la eficiencia de una máquina comparando el flujo recibido
        /// en cada puerto de entrada con lo que pide la receta activa.
        ///
        /// REGLA MULTI-INGREDIENTE:
        ///   El ingrediente más restrictivo (menor ratio recibido/requerido)
        ///   determina la eficiencia global → reduce la producción de TODOS los outputs.
        /// </summary>
        /// <summary>
        /// Enumera todos los FactoryGroupNode del grafo de forma recursiva,
        /// incluyendo los anidados a cualquier profundidad.
        /// </summary>
        private static IEnumerable<FactoryGroupNode> CollectAllGroups(IEnumerable<NodeModel> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is FactoryGroupNode group)
                {
                    yield return group;
                    foreach (var sub in CollectAllGroups(group.Children))
                        yield return sub;
                }
            }
        }

        private static void ApplyMachineEfficiency(
            MachineNode machine,
            Dictionary<Guid, Ports.InputPort> portById_In)
        {
            if (machine.ActiveRecipe is null) return;

            var inputs = machine.ActiveRecipe.Ingredients.Where(i => i.IsInput).ToList();
            if (inputs.Count == 0) return; // Nodo fuente, ya calculado

            double minRatio = 1.0;

            foreach (var ingredient in inputs)
            {
                // Puerto de entrada correspondiente a este ingrediente
                var port = machine.Inputs.FirstOrDefault(p => p.AcceptedItem?.Id == ingredient.Item?.Id);
                if (port is null) continue;

                double required = ingredient.ItemsPerMinute * machine.OverclockFactor;
                if (required <= 0) continue;

                double ratio = port.CurrentFlow / required;
                ratio = Math.Min(ratio, 1.0); // No puede ser > 100% (el excedente no aumenta la producción)

                if (ratio < minRatio)
                    minRatio = ratio;
            }

            machine.Efficiency = minRatio;

            // Escalar todos los outputs por la eficiencia real
            var outputIngredients = machine.ActiveRecipe.Ingredients.Where(i => !i.IsInput).ToList();
            foreach (var ingredient in outputIngredients)
            {
                var port = machine.Outputs.FirstOrDefault(p => p.EmittedItem?.Id == ingredient.Item?.Id);
                if (port is null) continue;

                double baseRate = ingredient.ItemsPerMinute * machine.OverclockFactor;
                port.CurrentFlow = baseRate * minRatio;
            }
        }
    }
}
