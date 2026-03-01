import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import type { ICellRendererAngularComp } from 'ag-grid-angular';
import type { ICellRendererParams } from 'ag-grid-community';

@Component({
  selector: 'app-test-order-status-renderer',
  standalone: true,
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="statusClass()">
      <mat-icon class="status-icon">{{ statusIcon() }}</mat-icon>
      <span>{{ value() }}</span>
    </span>
  `,
  styles: [
    `
      .status-pill {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        height: 24px;
        padding: 0 8px;
        border-radius: 999px;
        font-size: 12px;
        font-weight: 500;
        white-space: nowrap;
      }

      .status-icon {
        width: 14px;
        height: 14px;
        font-size: 14px;
        line-height: 14px;
      }

      .status-created {
        background: #e0f2fe;
        color: #075985;
      }

      .status-queued {
        background: #ede9fe;
        color: #5b21b6;
      }

      .status-running {
        background: #fef3c7;
        color: #92400e;
      }

      .status-success {
        background: #dcfce7;
        color: #166534;
      }

      .status-dead {
        background: #fee2e2;
        color: #991b1b;
      }

      .status-unknown {
        background: #e5e7eb;
        color: #374151;
      }
    `,
  ],
})
export class TestOrderStatusRendererComponent implements ICellRendererAngularComp {
  value = signal('Unknown');
  statusClass = signal('status-pill status-unknown');
  statusIcon = signal('help_outline');

  agInit(params: ICellRendererParams): void {
    this.refresh(params);
  }

  refresh(params: ICellRendererParams): boolean {
    const status = this.normalizeStatus(params.value);
    this.value.set(status);

    switch (status) {
      case 'Created':
        this.statusClass.set('status-pill status-created');
        this.statusIcon.set('fiber_manual_record');
        break;
      case 'Queued':
        this.statusClass.set('status-pill status-queued');
        this.statusIcon.set('schedule');
        break;
      case 'Running':
        this.statusClass.set('status-pill status-running');
        this.statusIcon.set('hourglass_top');
        break;
      case 'Success':
        this.statusClass.set('status-pill status-success');
        this.statusIcon.set('check_circle');
        break;
      case 'Dead':
        this.statusClass.set('status-pill status-dead');
        this.statusIcon.set('cancel');
        break;
      default:
        this.statusClass.set('status-pill status-unknown');
        this.statusIcon.set('help_outline');
        break;
    }

    return true;
  }

  private normalizeStatus(value: unknown): string {
    if (typeof value === 'string' && value.trim().length > 0) {
      return value;
    }

    if (typeof value === 'number') {
      switch (value) {
        case 0:
          return 'Created';
        case 1:
          return 'Queued';
        case 2:
          return 'Running';
        case 3:
          return 'Success';
        case 4:
          return 'Dead';
        default:
          return 'Unknown';
      }
    }

    return 'Unknown';
  }
}
