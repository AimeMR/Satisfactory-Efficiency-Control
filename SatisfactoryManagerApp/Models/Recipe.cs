using System.Collections.Generic;

namespace SatisfactoryManagerApp.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public double CycleTimeSeconds { get; set; } // Cuántos segundos tarda en completarse un ciclo

        // Relación con la máquina que la fabrica
        public int MachineId { get; set; }
        public Machine? Machine { get; set; }

        // Relación con los ingredientes (lo que entra y lo que sale)
        public List<RecipeIngredient> Ingredients { get; set; } = new();
    }
}