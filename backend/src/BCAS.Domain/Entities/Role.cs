namespace BCAS.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string Editor = "Editor";

    public const string AdministratorOrEditor = Administrator + "," + Editor;
}
