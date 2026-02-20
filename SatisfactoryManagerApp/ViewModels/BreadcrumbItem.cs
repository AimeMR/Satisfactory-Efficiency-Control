using SatisfactoryManagerApp.Graph.Nodes;

namespace SatisfactoryManagerApp.ViewModels
{
    /// <summary>
    /// Represents one segment in the breadcrumb navigation bar.
    /// Label  = display text (e.g. "Fábrica principal" or a group name).
    /// Group  = the FactoryGroupNode this segment points to;
    ///          null means root level.
    /// </summary>
    public record BreadcrumbItem(string Label, FactoryGroupNode? Group);
}
