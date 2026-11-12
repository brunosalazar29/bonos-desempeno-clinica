using BonusTest.Application.Common;

namespace BonusTest.Application.Bonuses;

public sealed record BonusEmployeeDto(
    string DepartmentCode,
    int EmployeeNumber,
    string FullName,
    DateTime HireDate,
    decimal MonthlySalary,
    decimal TaxRate);

public sealed record BonusListItemDto(
    long BonusId,
    string BonusStateCode,
    string BonusStateDescription,
    DateTime Period,
    string DepartmentCode,
    string DepartmentName,
    int EmployeeNumber,
    string EmployeeName,
    int BonusTypeId,
    string BonusTypeDescription,
    decimal? FixedAmount,
    decimal? SalaryPercentage,
    string ReasonCode,
    string ReasonDescription,
    decimal GrossAmount,
    decimal TaxAmount,
    decimal NetAmount);

public sealed record CreateBonusRequest(
    string DepartmentCode,
    int EmployeeNumber,
    string Period,
    int BonusTypeId,
    decimal Value,
    string ReasonCode);

public sealed class BonusFilterRequest : PaginationRequest
{
    public string? DepartmentCode { get; set; }
    public int? EmployeeNumber { get; set; }
    public string? Period { get; set; }
    public string? BonusStateCode { get; set; }
}

public sealed record BonusCalculationResult(decimal GrossAmount, decimal TaxAmount, decimal NetAmount);
