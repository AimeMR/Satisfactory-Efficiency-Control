namespace SatisfactoryManagerApp.Graph.Nodes
{
    /// <summary>
    /// Nodo logístico que distribuye el flujo de UNA entrada entre MÚLTIPLES salidas.
    /// No consume energía ni tiene receta.
    ///
    /// Comportamiento de reparto:
    ///  - Si todas las salidas están desbalanceadas (SplitEvenly=true): el flujo
    ///    entrante se divide a partes iguales entre todas las salidas activas.
    ///  - Si la salida tiene una capacidad fija asignada (TargetFlow > 0), el
    ///    FlowCalculator respetará ese tope, enviando el excedente a las demás.
    /// </summary>
    public class SplitterNode : NodeModel
    {
        /// <summary>Número de salidas que tendrá el divisor (por defecto 3, máximo 3 en Satisfactory).</summary>
        public int OutputCount
        {
            get => _outputCount;
            set
            {
                _outputCount = Math.Clamp(value, 1, 3);
                RebuildPorts();
            }
        }
        private int _outputCount = 3;

        /// <summary>
        /// Si es true, el FlowCalculator divide el flujo entrante en partes iguales.
        /// Si es false, respeta los TargetFlow de cada salida.
        /// </summary>
        public bool SplitEvenly { get; set; } = true;

        public SplitterNode(string name = "Divisor", int outputs = 3)
        {
            Name = name;
            RebuildPorts();
            OutputCount = outputs; // triggers clamp and rebuild
        }

        private void RebuildPorts()
        {
            Inputs.Clear();
            Outputs.Clear();
            AddInput("Entrada");
            for (int i = 1; i <= _outputCount; i++)
                AddOutput($"Salida {i}");
        }
    }
}
