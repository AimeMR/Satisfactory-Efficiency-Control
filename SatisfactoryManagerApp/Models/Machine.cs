namespace SatisfactoryManagerApp.Models
{
    public class Machine
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public double PowerConsumption { get; set; } // Consumo de energía en Megavatios (MW)
    }
}