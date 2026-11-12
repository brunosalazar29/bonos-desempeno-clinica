export interface BonusEmployee {
  departmentCode: string;
  employeeNumber: number;
  fullName: string;
  hireDate: string;
  monthlySalary: number;
  taxRate: number;
}

export interface BonusRow {
  bonusId: number;
  bonusStateCode: string;
  bonusStateDescription: string;
  period: string;
  departmentCode: string;
  departmentName: string;
  employeeNumber: number;
  employeeName: string;
  bonusTypeId: number;
  bonusTypeDescription: string;
  fixedAmount: number | null;
  salaryPercentage: number | null;
  reasonCode: string;
  reasonDescription: string;
  grossAmount: number;
  taxAmount: number;
  netAmount: number;
}

export interface BonusPage {
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  data: BonusRow[];
}

export interface BonusFilters {
  departmentCode?: string;
  employeeNumber?: number;
  period?: string;
  bonusStateCode?: string;
  pageIndex: number;
  pageSize: number;
}

export interface CreateBonusRequest {
  departmentCode: string;
  employeeNumber: number;
  period: string;
  bonusTypeId: number;
  value: number;
  reasonCode: string;
}
