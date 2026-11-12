namespace BonusTest.Domain.Entities;

public class Bonus
{
    public long BonusId { get; set; }
    public string DepartmentCode { get; set; } = null!;
    public int EmployeeNumber { get; set; }
    public DateTime Period { get; set; }
    public int BonusTypeId { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? SalaryPercentage { get; set; }
    public string ReasonCode { get; set; } = null!;
    public decimal GrossAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string BonusStateCode { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public BonusType BonusType { get; set; } = null!;
    public BonusReason BonusReason { get; set; } = null!;
    public State BonusState { get; set; } = null!;
}
