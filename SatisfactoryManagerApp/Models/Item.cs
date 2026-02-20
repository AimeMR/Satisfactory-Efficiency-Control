namespace SatisfactoryManagerApp.Models
{
    public class Item
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool IsFluid { get; set; } // Para saber si se transporta en cinta o en tubería
    }
}