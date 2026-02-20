using SatisfactoryManagerApp.Models;

namespace SatisfactoryManagerApp.Graph.Nodes
{
    /// <summary>
    /// Representa una máquina de producción en el lienzo (Ensambladora, Fundidora, etc.).
    /// Enlaza con los datos de la Fase 1 (Machine + Recipe) y añade el modificador de Overclock.
    /// </summary>
    public class MachineNode : NodeModel
    {
        // ── Referencia a Fase 1 ───────────────────────────────────────────────

        /// <summary>El tipo de máquina (de la base de datos de Fase 1).</summary>
        public Machine? MachineType { get; set; }

        /// <summary>La receta activa que está ejecutando esta máquina.</summary>
        public Recipe? ActiveRecipe { get; set; }

        // ── Modificadores del juego ───────────────────────────────────────────

        /// <summary>
        /// Porcentaje de overclock aplicado a la máquina.
        /// Mínimo: 1.0 (1%), máximo: 250.0 (250%). Valor por defecto: 100%.
        /// Afecta tanto la velocidad de producción como el consumo de energía.
        /// </summary>
        public double OverclockPercent
        {
            get => _overclockPercent;
            set => _overclockPercent = Math.Clamp(value, 1.0, 250.0);
        }
        private double _overclockPercent = 100.0;

        /// <summary>Factor decimal del overclock (ej. 100% → 1.0, 250% → 2.5).</summary>
        public double OverclockFactor => OverclockPercent / 100.0;

        // ── Estado calculado (actualizado por FlowCalculator) ─────────────────

        /// <summary>
        /// Eficiencia real de la máquina tras el cálculo de flujos (0.0 a 1.0).
        /// Determinada por el ingrediente más restrictivo (starvation).
        /// 1.0 = produce al 100% del overclock configurado.
        /// </summary>
        public double Efficiency { get; set; } = 1.0;

        /// <summary>
        /// Consumo real de energía (MW), teniendo en cuenta el overclock.
        /// Fórmula: PowerBase * (OverclockFactor ^ 1.321928)
        /// </summary>
        public double ActualPowerMW =>
            MachineType is null ? 0.0
            : MachineType.PowerConsumption * Math.Pow(OverclockFactor, 1.321928);

        // ── Constructors ──────────────────────────────────────────────────────

        /// <summary>
        /// Constructor sin parámetros para el serializador JSON.
        /// </summary>
        public MachineNode() { }

        /// <summary>
        /// Crea un MachineNode y genera automáticamente sus puertos
        /// a partir de la receta proporcionada.
        /// </summary>
        public MachineNode(Machine? machine = null, Recipe? recipe = null)
        {
            MachineType = machine;
            Name = machine?.Name ?? "Máquina";
            SetRecipe(recipe);
        }

        /// <summary>
        /// Cambia la receta activa y reconstruye los puertos del nodo.
        /// </summary>
        public void SetRecipe(Recipe? recipe)
        {
            ActiveRecipe = recipe;
            Inputs.Clear();
            Outputs.Clear();

            if (recipe is null) return;

            Name = $"{MachineType?.Name ?? "Máquina"} - {recipe.Name}";

            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.IsInput)
                    AddInput(ingredient.Item?.Name ?? "Entrada", ingredient.Item);
                else
                    AddOutput(ingredient.Item?.Name ?? "Salida", ingredient.Item);
            }
        }
    }
}
