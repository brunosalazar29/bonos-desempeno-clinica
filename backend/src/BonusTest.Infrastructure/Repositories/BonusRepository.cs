using Microsoft.EntityFrameworkCore;
using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Persistence;

namespace BonusTest.Infrastructure.Repositories;

public sealed class BonusRepository(AppDbContext context) : IBonusRepository
{
    public Task<Employee?> GetEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken) =>
        context.Employees.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.DepartmentCode == departmentCode && x.EmployeeNumber == employeeNumber,
                cancellationToken);

    public Task<bool> IsBonusTypeActiveAsync(int bonusTypeId, CancellationToken cancellationToken) =>
        context.BonusTypes.AsNoTracking().AnyAsync(x => x.BonusTypeId == bonusTypeId, cancellationToken);

    public Task<bool> IsReasonActiveAsync(string reasonCode, CancellationToken cancellationToken) =>
        context.BonusReasons.AsNoTracking().AnyAsync(
            x => x.ReasonCode == reasonCode && x.State,
            cancellationToken);

    public Task<bool> HasActiveBonusAsync(
        string departmentCode,
        int employeeNumber,
        DateTime period,
        CancellationToken cancellationToken) =>
        context.Bonuses.AsNoTracking().AnyAsync(
            x => !x.IsDeleted
                && x.BonusStateCode != "0203"
                && x.DepartmentCode == departmentCode
                && x.EmployeeNumber == employeeNumber
                && x.Period == period,
            cancellationToken);

    public async Task<PagedResponse<BonusListItemDto>> GetPagedAsync(
        BonusFilterRequest request,
        DateTime? period,
        CancellationToken cancellationToken)
    {
        var query = context.Bonuses.AsNoTracking().Where(x => !x.IsDeleted);

        if (request.DepartmentCode is not null)
            query = query.Where(x => x.DepartmentCode == request.DepartmentCode);
        if (request.EmployeeNumber.HasValue)
            query = query.Where(x => x.EmployeeNumber == request.EmployeeNumber.Value);
        if (period.HasValue)
            query = query.Where(x => x.Period == period.Value);
        if (request.BonusStateCode is not null)
            query = query.Where(x => x.BonusStateCode == request.BonusStateCode);

        var totalRecords = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderByDescending(x => x.Period)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.BonusId)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new BonusListItemDto(
                x.BonusId,
                x.BonusStateCode,
                x.BonusState.StateDescription,
                x.Period,
                x.DepartmentCode,
                x.Employee.Department.DepartmentName,
                x.EmployeeNumber,
                x.Employee.FullName,
                x.BonusTypeId,
                x.BonusType.BonusTypeDescription,
                x.FixedAmount,
                x.SalaryPercentage,
                x.ReasonCode,
                x.BonusReason.ReasonDescription,
                x.GrossAmount,
                x.TaxAmount,
                x.NetAmount))
            .ToListAsync(cancellationToken);

        return PagedResponse<BonusListItemDto>.Create(
            data,
            request.PageIndex,
            request.PageSize,
            totalRecords);
    }

    public Task<Bonus?> GetByIdAsync(long bonusId, CancellationToken cancellationToken) =>
        context.Bonuses.SingleOrDefaultAsync(x => x.BonusId == bonusId, cancellationToken);

    public async Task AddAsync(Bonus bonus, CancellationToken cancellationToken)
    {
        await context.Bonuses.AddAsync(bonus, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
