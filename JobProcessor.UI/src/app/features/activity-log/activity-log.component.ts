import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ActivityLogService } from './Shared/activity-log.service';

@Component({
  selector: 'app-activity-log',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity-log.component.html',
  styleUrls: ['./activity-log.component.css'],
})
export class ActivityLogComponent implements OnInit, OnDestroy {
  private activityLogService = inject(ActivityLogService);

  activities = this.activityLogService.logs;
  isConnected = this.activityLogService.isConnected;
  isLoading = true;

  ngOnInit(): void {
    this.activityLogService.connect();
    this.isLoading = false;
  }

  ngOnDestroy(): void {
    this.activityLogService.disconnect();
  }
}
