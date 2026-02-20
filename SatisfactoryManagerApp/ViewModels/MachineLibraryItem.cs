namespace SatisfactoryManagerApp.ViewModels
{
    /// <summary>
    /// Represents one draggable entry in the machine library sidebar.
    /// MachineName is shown as the label; PowerMW is used when creating
    /// the MachineNode on drop.
    /// </summary>
    public class MachineLibraryItem
    {
        public string MachineName { get; init; } = string.Empty;
        public double PowerMW { get; init; }

        public override string ToString() => MachineName;
    }
}
