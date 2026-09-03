namespace OrderManagement.DTOs;

public class GetOrdersRequestDto
{
    public int? UserId { get; set; }
    public string? Status { get; set; }
    public DateTime? Cursor { get; set; }
    public int? PageSize { get; set; } = 20;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Direction { get; set; } = "next";
    public string SortDirection { get; set; } = "desc";
}
