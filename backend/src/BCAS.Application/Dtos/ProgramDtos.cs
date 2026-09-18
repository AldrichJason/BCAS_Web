using System.ComponentModel.DataAnnotations;

namespace BCAS.Application.Dtos;

public class ProgramResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DegreeLevel { get; set; } = string.Empty;
    public int DurationYears { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public class ProgramRequest
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DegreeLevel { get; set; } = string.Empty;

    [Range(1, 10)]
    public int DurationYears { get; set; } = 4;

    public bool IsActive { get; set; } = true;

    [Range(0, 1000)]
    public int DisplayOrder { get; set; }
}
