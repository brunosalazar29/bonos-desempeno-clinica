using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

public sealed class BonusService(
    IBonusRepository repository,
    TimeProvider timeProvider,
    ILogger<BonusService> logger)
{
    public async Task<PagedResponse<BonusListItemDto>> GetPagedAsync(
        BonusFilterRequest request,
        CancellationToken cancellationToken)
    {
        DateTime? period = null;
        if (!string.IsNullOrWhiteSpace(request.Period))
            period = ParsePeriod(request.Period);

        request.DepartmentCode = string.IsNullOrWhiteSpace(request.DepartmentCode)
            ? null
            : request.DepartmentCode.Trim();
        request.BonusStateCode = string.IsNullOrWhiteSpace(request.BonusStateCode)
            ? null
            : request.BonusStateCode.Trim();

        return await repository.GetPagedAsync(request, period, cancellationToken);
    }

    public async Task<BonusEmployeeDto> FindEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken)
    {
        var employee = await GetEligibleEmployeeAsync(departmentCode, employeeNumber, cancellationToken);
        return new BonusEmployeeDto(
            employee.DepartmentCode,
            employee.EmployeeNumber,
            employee.FullName,
            employee.HireDate,
            employee.MonthlySalary,
            employee.TaxRate);
    }

    public async Task<long> CreateAsync(
        CreateBonusRequest request,
        string? userCode,
        CancellationToken cancellationToken)
    {
        var createdBy = RequireUserCode(userCode);
        var departmentCode = request.DepartmentCode?.Trim() ?? string.Empty;
        var reasonCode = request.ReasonCode?.Trim() ?? string.Empty;
        if (departmentCode.Length == 0 || request.EmployeeNumber <= 0)
            throw new BusinessException("EMPLOYEE_KEY_REQUIRED", "Indica un área y un número de empleado válidos.");

        var period = ParsePeriod(request.Period);
        if (period > GetPeruCurrentMonthStart())
            throw new BusinessException("PERIOD_IN_FUTURE", "El periodo no puede ser posterior al mes actual.");

        var employee = await GetEligibleEmployeeAsync(departmentCode, request.EmployeeNumber, cancellationToken);
        if (!await repository.IsBonusTypeActiveAsync(request.BonusTypeId, cancellationToken))
            throw new BusinessException("BONUS_TYPE_INVALID", "El tipo de bono indicado no existe.");
        if (reasonCode.Length == 0 || !await repository.IsReasonActiveAsync(reasonCode, cancellationToken))
            throw new BusinessException("BONUS_REASON_INACTIVE", "Selecciona un motivo activo.");

        var normalizedValue = NormalizeValue(request.BonusTypeId, request.Value, employee.MonthlySalary);
        if (await repository.HasActiveBonusAsync(departmentCode, request.EmployeeNumber, period, cancellationToken))
            throw new BusinessException("BONUS_ALREADY_EXISTS", "El empleado ya tiene un bono activo para ese periodo.");

        var amounts = BonusCalculator.Calculate(
            request.BonusTypeId,
            normalizedValue,
            employee.MonthlySalary,
            employee.TaxRate);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var bonus = new Bonus
        {
            DepartmentCode = departmentCode,
            EmployeeNumber = request.EmployeeNumber,
            Period = period,
            BonusTypeId = request.BonusTypeId,
            FixedAmount = request.BonusTypeId == BonusTypes.FixedAmount ? amounts.GrossAmount : null,
            SalaryPercentage = request.BonusTypeId == BonusTypes.SalaryPercentage ? normalizedValue : null,
            ReasonCode = reasonCode,
            GrossAmount = amounts.GrossAmount,
            TaxAmount = amounts.TaxAmount,
            NetAmount = amounts.NetAmount,
            BonusStateCode = BonusStates.Registered,
            IsDeleted = false,
            CreatedBy = createdBy,
            CreatedAt = now
        };

        try
        {
            await repository.AddAsync(bonus, cancellationToken);
            return bonus.BonusId;
        }
        catch (DbUpdateException exception) when (IsActiveBonusUniqueViolation(exception))
        {
            logger.LogInformation(
                "Se rechazó una alta concurrente de bono para {DepartmentCode}-{EmployeeNumber} en {Period}.",
                departmentCode,
                request.EmployeeNumber,
                period);
            throw new BusinessException("BONUS_ALREADY_EXISTS", "El empleado ya tiene un bono activo para ese periodo.");
        }
    }

    public async Task CancelAsync(long bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var actor = RequireUserCode(userCode);
        var bonus = await repository.GetByIdAsync(bonusId, cancellationToken)
            ?? throw new BusinessException("BONUS_NOT_FOUND", "No se encontró el bono indicado.");
        if (bonus.BonusStateCode != BonusStates.Registered || bonus.IsDeleted)
            throw new BusinessException("BONUS_CANNOT_CANCEL", "Solo se puede anular un bono Registrado.");

        bonus.BonusStateCode = BonusStates.Cancelled;
        bonus.ModifiedBy = actor;
        bonus.ModifiedAt = timeProvider.GetUtcNow().UtcDateTime;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var actor = RequireUserCode(userCode);
        var bonus = await repository.GetByIdAsync(bonusId, cancellationToken)
            ?? throw new BusinessException("BONUS_NOT_FOUND", "No se encontró el bono indicado.");
        if (bonus.BonusStateCode != BonusStates.Registered || bonus.IsDeleted)
            throw new BusinessException("BONUS_CANNOT_DELETE", "Solo se puede eliminar un bono Registrado.");

        bonus.IsDeleted = true;
        bonus.ModifiedBy = actor;
        bonus.ModifiedAt = timeProvider.GetUtcNow().UtcDateTime;
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Employee> GetEligibleEmployeeAsync(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(departmentCode) || employeeNumber <= 0)
            throw new BusinessException("EMPLOYEE_KEY_REQUIRED", "Indica un área y un número de empleado válidos.");

        var employee = await repository.GetEmployeeAsync(
            departmentCode.Trim(),
            employeeNumber,
            cancellationToken);
        if (employee is null)
            throw new BusinessException("EMPLOYEE_NOT_FOUND", "No se encontró al empleado indicado.");
        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_NOT_ACTIVE", "Solo se pueden registrar bonos para empleados Activos.");
        return employee;
    }

    private static decimal NormalizeValue(int bonusTypeId, decimal value, decimal monthlySalary)
    {
        if (value <= 0)
            throw new BusinessException("BONUS_VALUE_INVALID", "El valor del bono debe ser mayor que cero.");

        switch (bonusTypeId)
        {
            case BonusTypes.FixedAmount:
            {
                if (value > monthlySalary)
                    throw new BusinessException("FIXED_AMOUNT_INVALID", "El monto fijo debe ser mayor que cero y no superar el sueldo mensual.");

                var amount = BonusCalculator.RoundMoney(value);
                if (amount <= 0)
                    throw new BusinessException("FIXED_AMOUNT_INVALID", "El monto fijo debe ser mayor que cero y no superar el sueldo mensual.");
                return amount;
            }
            case BonusTypes.SalaryPercentage:
            {
                if (value > 100)
                    throw new BusinessException("PERCENTAGE_INVALID", "El porcentaje debe ser mayor que cero y no superar 100.");
                var percentage = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
                if (percentage <= 0 || percentage > 100)
                    throw new BusinessException("PERCENTAGE_INVALID", "El porcentaje debe ser mayor que cero y no superar 100.");
                return percentage;
            }
            default:
                throw new BusinessException("BONUS_TYPE_INVALID", "El tipo de bono indicado no existe.");
        }
    }

    private static DateTime ParsePeriod(string? value)
    {
        if (!DateTime.TryParseExact(
                value?.Trim(),
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var period))
            throw new BusinessException("PERIOD_INVALID", "El periodo debe tener el formato YYYY-MM.");

        return new DateTime(period.Year, period.Month, 1);
    }

    private DateTime GetPeruCurrentMonthStart()
    {
        var timeZoneId = OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var peruNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        return new DateTime(peruNow.Year, peruNow.Month, 1);
    }

    private static string RequireUserCode(string? userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode))
            throw new BusinessException("USER_CODE_REQUIRED", "No se recibió el código del usuario que opera.");

        var normalized = userCode.Trim();
        if (normalized.Length > 100)
            throw new BusinessException("USER_CODE_INVALID", "El código del usuario supera el máximo permitido.");
        return normalized;
    }

    private static bool IsActiveBonusUniqueViolation(DbUpdateException exception)
    {
        var message = exception.GetBaseException().Message;
        return message.Contains("UX_Bonus_Active_Employee_Period", StringComparison.OrdinalIgnoreCase)
            || message.Contains("2601", StringComparison.OrdinalIgnoreCase)
            || message.Contains("2627", StringComparison.OrdinalIgnoreCase);
    }
}
