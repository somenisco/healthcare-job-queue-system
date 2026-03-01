import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { INoRowsOverlayAngularComp } from 'ag-grid-angular';
import { INoRowsOverlayParams } from 'ag-grid-community';

type CustomNoRowsOverlayParams = INoRowsOverlayParams & {
  noRowsMessage?: string;
  icon?: string;
};
@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.css'],
})
export class EmptyStateComponent implements INoRowsOverlayAngularComp {
  noRowsMessage = signal('No data to display');
  icon = signal('📄');

  agInit(params: CustomNoRowsOverlayParams): void {
    this.refresh(params);
  }

  refresh(params: CustomNoRowsOverlayParams): void {
    this.noRowsMessage.set(params.noRowsMessage || 'No data to display');
    this.icon.set(params.icon || '📄');
  }
}
