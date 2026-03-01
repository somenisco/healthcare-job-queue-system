import {
  Component,
  effect,
  HostListener,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { AgGridAngular } from 'ag-grid-angular';
import {
  type ColDef,
  type GridApi,
  type GridReadyEvent,
} from 'ag-grid-community';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { finalize } from 'rxjs';

import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { TestOrderStatusRendererComponent } from './shared/test-order-status-renderer.component';
import { TestOrderListItem } from './shared/test-order.model';
import { TestOrdersService } from './shared/test-orders.service';
import { TestOrderStreamService } from './shared/test-order-stream.service';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';

@Component({
  selector: 'app-test-orders',
  standalone: true,
  imports: [
    CommonModule,
    AgGridAngular,
    MatCardModule,
    MatProgressSpinnerModule,
    MatPaginatorModule,
    MatButtonModule,
    MatBadgeModule,
  ],
  templateUrl: './test-orders.component.html',
  styleUrls: ['./test-orders.component.css'],
})
export class TestOrdersComponent implements OnInit, OnDestroy {
  readonly defaultColDef: ColDef = {
    sortable: true,
    filter: true,
    resizable: true,
  };

  readonly pageSizeOptions = [20, 50, 100];

  readonly columnDefs: ColDef<TestOrderListItem>[] = [
    { field: 'testOrderId', headerName: 'Order #', width: 185 },
    {
      field: 'testOrderType',
      headerName: 'Type',
      minWidth: 130,
      resizable: false,
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 170,
      cellRenderer: TestOrderStatusRendererComponent,
    },
    { field: 'sampleId', headerName: 'Sample #', width: 185 },
    {
      field: 'updatedAt',
      headerName: 'Updated',
      sort: 'desc',
      width: 210,
      valueFormatter: (params) =>
        params.value ? new Date(params.value as string).toLocaleString() : '',
    },
  ];

  readonly noRowsOverlayComponent = EmptyStateComponent;
  readonly noRowsOverlayComponentParams = {
    noRowsMessage: 'No test orders found',
    icon: '🧪',
  };

  getRowId = (params: { data: TestOrderListItem }) => params.data.testOrderId;

  testOrders: TestOrderListItem[] = [];
  isLoading = false;
  errorMessage = '';
  hasNewUpdates = signal(false);
  currentPage = signal(1);
  pageSize = 20;
  totalCount = 0;

  private gridApi: GridApi<TestOrderListItem> | null = null;
  private testOrderStreamService = inject(TestOrderStreamService);

  private realTimeEffect = effect(
    () => {
      const realtimeEvents = this.testOrderStreamService.events();
      const latestTestOrder = realtimeEvents[0];

      if (!latestTestOrder || !this.gridApi) {
        return;
      }

      this.applyRealtimeUpdate(latestTestOrder);
    },
    { allowSignalWrites: true },
  );

  constructor(private readonly testOrdersService: TestOrdersService) {}

  ngOnInit(): void {
    this.loadTestOrders();
    this.testOrderStreamService.connect();
  }

  ngOnDestroy(): void {
    this.testOrderStreamService.disconnect();
  }

  private applyRealtimeUpdate(testOrder: TestOrderListItem): void {
    if (this.currentPage() !== 1) {
      this.hasNewUpdates.set(true);
      return;
    }
    const existingTestOrderNode = this.gridApi!.getRowNode(
      testOrder.testOrderId,
    );

    if (existingTestOrderNode) {
      this.gridApi!.applyTransactionAsync({
        update: [testOrder],
      });
    } else {
      this.gridApi!.applyTransaction({
        add: [testOrder],
        addIndex: 0,
      });
    }
  }

  refreshPage(): void {
    this.loadTestOrders();
    this.hasNewUpdates.set(false);
  }

  onGridReady(event: GridReadyEvent<TestOrderListItem>): void {
    this.gridApi = event.api;
    setTimeout(() => this.sizeColumnsToFit());
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.sizeColumnsToFit();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.currentPage.set(event.pageIndex + 1);

    this.hasNewUpdates.set(false);

    this.loadTestOrders();
  }

  loadTestOrders(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.testOrdersService
      .getTestOrders(this.currentPage(), this.pageSize)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.testOrders = response.items;
          this.totalCount = response.totalCount;
          setTimeout(() => this.sizeColumnsToFit());
        },
        error: () => {
          this.testOrders = [];
          this.totalCount = 0;
          this.errorMessage = 'Unable to load test orders.';
        },
      });
  }

  private sizeColumnsToFit(): void {
    if (!this.gridApi) {
      return;
    }

    this.gridApi.sizeColumnsToFit();
  }
}
