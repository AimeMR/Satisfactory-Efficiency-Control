namespace SatisfactoryManagerApp.Graph.Nodes
{
    /// <summary>
    /// Nodo logístico que combina el flujo de MÚLTIPLES entradas en UNA sola salida.
    /// No consume energía ni tiene receta.
    ///
    /// Comportamiento: la salida es la suma de todos los flujos de entrada.
    /// Si la suma supera la capacidad de la cinta de salida, se genera un cuello de botella.
    /// </summary>
    public class MergerNode : NodeModel
    {
        /// <summary>Número de entradas del unificador (por defecto 3, máximo 3 en Satisfactory).</summary>
        public int InputCount
        {
            get => _inputCount;
            set
            {
                _inputCount = Math.Clamp(value, 1, 3);
                RebuildPorts();
            }
        }
        private int _inputCount = 3;

        public MergerNode(string name = "Unificador", int inputs = 3)
        {
            Name = name;
            RebuildPorts();
            InputCount = inputs;
        }

        private void RebuildPorts()
        {
            Inputs.Clear();
            Outputs.Clear();
            for (int i = 1; i <= _inputCount; i++)
                AddInput($"Entrada {i}");
            AddOutput("Salida");
        }
    }
}
