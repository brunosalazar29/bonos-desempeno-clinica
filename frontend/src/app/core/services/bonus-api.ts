import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BonusEmployee, BonusFilters, BonusPage, CreateBonusRequest } from '../models/bonus';

@Injectable({ providedIn: 'root' })
export class BonusApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + '/Bonus';

  getPaged(filters: BonusFilters): Observable<BonusPage> {
    let params = new HttpParams()
      .set('pageIndex', filters.pageIndex)
      .set('pageSize', filters.pageSize);

    if (filters.departmentCode) params = params.set('departmentCode', filters.departmentCode);
    if (filters.employeeNumber) params = params.set('employeeNumber', filters.employeeNumber);
    if (filters.period) params = params.set('period', filters.period);
    if (filters.bonusStateCode) params = params.set('bonusStateCode', filters.bonusStateCode);

    return this.http.get<BonusPage>(this.baseUrl, { params });
  }

  findEmployee(departmentCode: string, employeeNumber: number): Observable<BonusEmployee> {
    return this.http.get<BonusEmployee>(
      this.baseUrl + '/employee/' + encodeURIComponent(departmentCode) + '/' + employeeNumber,
    );
  }

  create(request: CreateBonusRequest): Observable<{ bonusId: number }> {
    return this.http.post<{ bonusId: number }>(this.baseUrl, request);
  }

  cancel(bonusId: number): Observable<void> {
    return this.http.put<void>(this.baseUrl + '/' + bonusId + '/cancel', {});
  }

  delete(bonusId: number): Observable<void> {
    return this.http.delete<void>(this.baseUrl + '/' + bonusId);
  }
}
