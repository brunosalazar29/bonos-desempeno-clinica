using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    Task<Employee?> GetEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken);
    Task<bool> IsBonusTypeActiveAsync(int bonusTypeId, CancellationToken cancellationToken);
    Task<bool> IsReasonActiveAsync(string reasonCode, CancellationToken cancellationToken);
    Task<bool> HasActiveBonusAsync(string departmentCode, int employeeNumber, DateTime period, CancellationToken cancellationToken);
    Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest request, DateTime? period, CancellationToken cancellationToken);
    Task<Bonus?> GetByIdAsync(long bonusId, CancellationToken cancellationToken);
    Task AddAsync(Bonus bonus, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
