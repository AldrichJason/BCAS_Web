using System.ComponentModel.DataAnnotations;

namespace BCAS.Application.Dtos;

public class FaqResponse
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class FaqRequest
{
    [Required]
    [MaxLength(300)]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Keywords { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Category { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class ChatRequest
{
    public Guid? SessionId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public Guid SessionId { get; set; }
    public string Reply { get; set; } = string.Empty;
    public int? MatchedFaqId { get; set; }
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
}
