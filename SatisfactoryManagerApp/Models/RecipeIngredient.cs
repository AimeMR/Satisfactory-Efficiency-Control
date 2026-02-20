using System.ComponentModel.DataAnnotations.Schema;

namespace SatisfactoryManagerApp.Models
{
    public class RecipeIngredient
    {
        public int Id { get; set; }

        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        public int ItemId { get; set; }
        public Item? Item { get; set; }

        public double Amount { get; set; } // Cantidad exacta por cada ciclo
        public bool IsInput { get; set; } // True si el ítem entra (costo), False si el ítem sale (producto)

        // [NotMapped] le dice a la base de datos que ignore esta propiedad, 
        // solo existe en la memoria de nuestro programa para facilitarnos la vida.
        [NotMapped]
        public double ItemsPerMinute
        {
            get
            {
                // Si no hay receta cargada o el tiempo es 0, evitamos dividir por cero
                if (Recipe == null || Recipe.CycleTimeSeconds <= 0)
                    return 0;

                // Fórmula mágica de Satisfactory: (Cantidad / Segundos) * 60
                return (Amount / Recipe.CycleTimeSeconds) * 60.0;
            }
        }
    }
}