import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PagedResponse, TestOrderListItem } from './test-order.model';

@Injectable({
  providedIn: 'root',
})
export class TestOrdersService {
  constructor(private readonly http: HttpClient) {}

  getTestOrders(
    page: number,
    pageSize: number,
    status?: string,
  ): Observable<PagedResponse<TestOrderListItem>> {
    let params = new HttpParams().set('Page', page).set('PageSize', pageSize);

    if (status) {
      params = params.set('TestOrderStatus', status);
    }

    return this.http.get<PagedResponse<TestOrderListItem>>('/tests', {
      params,
    });
  }
}
