import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, of } from 'rxjs';
import { BonusEmployee, BonusPage, BonusRow, CreateBonusRequest } from '../../core/models/bonus';
import { BonusReason, BonusStates, BonusType, BonusTypes, Department, MasterStateCodes, State } from '../../core/models/catalog';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';
import { Notification } from '../../core/services/notification';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-bonuses',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly catalogApi = inject(CatalogApi);
  private readonly bonusApi = inject(BonusApi);
  private readonly notification = inject(Notification);

  protected readonly BonusStates = BonusStates;
  protected readonly BonusTypes = BonusTypes;
  protected readonly pageSize = PAGE_SIZE;
  protected readonly currentMonthValue = this.currentMonth();
  protected readonly departments = signal<Department[]>([]);
  protected readonly bonusStates = signal<State[]>([]);
  protected readonly bonusTypes = signal<BonusType[]>([]);
  protected readonly bonusReasons = signal<BonusReason[]>([]);
  protected readonly rows = signal<BonusRow[]>([]);
  protected readonly employee = signal<BonusEmployee | null>(null);
  protected readonly loading = signal(false);
  protected readonly searchingEmployee = signal(false);
  protected readonly saving = signal(false);
  protected readonly modalOpen = signal(false);
  protected readonly pageIndex = signal(1);
  protected readonly totalRecords = signal(0);
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalRecords() / PAGE_SIZE)));
  protected readonly firstRow = computed(() =>
    this.totalRecords() === 0 ? 0 : (this.pageIndex() - 1) * PAGE_SIZE + 1,
  );
  protected readonly lastRow = computed(() => Math.min(this.pageIndex() * PAGE_SIZE, this.totalRecords()));
  protected readonly previewInput = signal({ bonusTypeId: BonusTypes.FixedAmount, value: 0 });
  protected readonly preview = computed(() => {
    const currentEmployee = this.employee();
    if (!currentEmployee) return null;

    const { bonusTypeId, value } = this.previewInput();
    const rawGross =
      bonusTypeId === BonusTypes.FixedAmount
        ? value
        : (currentEmployee.monthlySalary * value) / 100;
    const gross = this.roundMoney(rawGross);
    const tax = this.roundMoney((gross * currentEmployee.taxRate) / 100);
    return { gross, tax, net: this.roundMoney(gross - tax) };
  });

  protected readonly filtersForm = this.fb.nonNullable.group({
    departmentCode: [''],
    employeeNumber: [''],
    period: [''],
    bonusStateCode: [''],
  });

  protected readonly employeeSearchForm = this.fb.nonNullable.group({
    departmentCode: ['', Validators.required],
    employeeNumber: ['', [Validators.required, Validators.pattern(/^[1-9]\d*$/)]],
  });

  protected readonly bonusForm = this.fb.nonNullable.group({
    period: [
      this.currentMonthValue,
      [
        Validators.required,
        Validators.pattern(/^\d{4}-(0[1-9]|1[0-2])$/),
        (control) => control.value && control.value > this.currentMonthValue ? { futurePeriod: true } : null,
      ],
    ],
    bonusTypeId: [BonusTypes.FixedAmount, Validators.required],
    value: [0, [Validators.required, Validators.min(0.01)]],
    reasonCode: ['', Validators.required],
  });

  ngOnInit(): void {
    this.catalogApi.getDepartments().pipe(catchError(() => of([]))).subscribe((items) => this.departments.set(items));
    this.catalogApi.getStates(MasterStateCodes.Bonus).pipe(catchError(() => of([]))).subscribe((items) => this.bonusStates.set(items));
    this.catalogApi.getBonusTypes().pipe(catchError(() => of([]))).subscribe((items) => this.bonusTypes.set(items));
    this.catalogApi.getBonusReasons().pipe(catchError(() => of([]))).subscribe((items) => this.bonusReasons.set(items));

    this.bonusForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) =>
        this.previewInput.set({
          bonusTypeId: Number(value.bonusTypeId ?? BonusTypes.FixedAmount),
          value: Number(value.value ?? 0),
        }),
      );

    this.loadPage(1);
  }

  protected search(): void {
    this.loadPage(1);
  }

  protected clearFilters(): void {
    this.filtersForm.reset({ departmentCode: '', employeeNumber: '', period: '', bonusStateCode: '' });
    this.loadPage(1);
  }

  protected changePage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.pageIndex()) return;
    this.loadPage(page);
  }

  protected openCreate(): void {
    this.employee.set(null);
    this.employeeSearchForm.reset({ departmentCode: '', employeeNumber: '' });
    this.bonusForm.reset({
      period: this.currentMonthValue,
      bonusTypeId: BonusTypes.FixedAmount,
      value: 0,
      reasonCode: '',
    });
    this.updateValueValidators();
    this.modalOpen.set(true);
  }

  protected closeModal(): void {
    if (this.saving()) return;
    this.modalOpen.set(false);
  }

  protected findEmployee(): void {
    if (this.employeeSearchForm.invalid) {
      this.employeeSearchForm.markAllAsTouched();
      return;
    }

    const { departmentCode, employeeNumber } = this.employeeSearchForm.getRawValue();
    this.searchingEmployee.set(true);
    this.employee.set(null);
    this.bonusApi.findEmployee(departmentCode, Number(employeeNumber)).subscribe({
      next: (employee) => {
        this.employee.set(employee);
        this.updateValueValidators();
        this.searchingEmployee.set(false);
      },
      error: () => this.searchingEmployee.set(false),
    });
  }

  protected onBonusTypeChange(): void {
    this.updateValueValidators();
  }

  protected valueLabel(): string {
    return this.bonusForm.controls.bonusTypeId.value === BonusTypes.FixedAmount
      ? 'Monto fijo'
      : 'Porcentaje del sueldo';
  }

  protected valueMax(): number | null {
    if (this.bonusForm.controls.bonusTypeId.value === BonusTypes.SalaryPercentage) return 100;
    return this.employee()?.monthlySalary ?? null;
  }

  protected saveBonus(): void {
    if (!this.employee() || this.bonusForm.invalid) {
      this.bonusForm.markAllAsTouched();
      return;
    }

    const form = this.bonusForm.getRawValue();
    const request: CreateBonusRequest = {
      departmentCode: this.employee()!.departmentCode,
      employeeNumber: this.employee()!.employeeNumber,
      period: form.period,
      bonusTypeId: Number(form.bonusTypeId),
      value: Number(form.value),
      reasonCode: form.reasonCode,
    };

    this.saving.set(true);
    this.bonusApi.create(request).subscribe({
      next: () => {
        this.saving.set(false);
        this.modalOpen.set(false);
        this.notification.show('success', 'El bono se registró correctamente.');
        this.loadPage(1);
      },
      error: () => this.saving.set(false),
    });
  }

  protected cancelBonus(row: BonusRow): void {
    if (!window.confirm('¿Confirma la anulación del bono de ' + row.employeeName + ' para ' + row.period.slice(0, 7) + '?'))
      return;

    this.bonusApi.cancel(row.bonusId).subscribe({
      next: () => {
        this.notification.show('success', 'El bono se anuló correctamente.');
        this.loadPage(1);
      },
      error: () => undefined,
    });
  }

  protected deleteBonus(row: BonusRow): void {
    if (!window.confirm('¿Confirma la eliminación lógica del bono de ' + row.employeeName + ' para ' + row.period.slice(0, 7) + '?'))
      return;

    this.bonusApi.delete(row.bonusId).subscribe({
      next: () => {
        this.notification.show('success', 'El bono se eliminó correctamente.');
        this.loadPage(1);
      },
      error: () => undefined,
    });
  }

  private loadPage(pageIndex: number): void {
    const filters = this.filtersForm.getRawValue();
    const request = {
      departmentCode: filters.departmentCode || undefined,
      employeeNumber: filters.employeeNumber ? Number(filters.employeeNumber) : undefined,
      period: filters.period || undefined,
      bonusStateCode: filters.bonusStateCode || undefined,
      pageIndex,
      pageSize: PAGE_SIZE,
    };

    this.loading.set(true);
    this.bonusApi.getPaged(request).subscribe({
      next: (page: BonusPage) => {
        this.rows.set(page.data);
        this.totalRecords.set(page.totalRecords);
        this.pageIndex.set(page.pageNumber);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private updateValueValidators(): void {
    const valueControl = this.bonusForm.controls.value;
    const validators =
      this.bonusForm.controls.bonusTypeId.value === BonusTypes.SalaryPercentage
        ? [Validators.required, Validators.min(0.01), Validators.max(100)]
        : [
            Validators.required,
            Validators.min(0.01),
            ...(this.employee() ? [Validators.max(this.employee()!.monthlySalary)] : []),
          ];
    valueControl.setValidators(validators);
    valueControl.updateValueAndValidity({ emitEvent: false });
  }

  private currentMonth(): string {
    const now = new Date();
    return now.getFullYear().toString() + '-' + String(now.getMonth() + 1).padStart(2, '0');
  }

  private roundMoney(value: number): number {
    return Math.round((value + Number.EPSILON) * 100) / 100;
  }
}
