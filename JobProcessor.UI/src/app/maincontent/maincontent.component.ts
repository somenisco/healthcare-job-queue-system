import { Component } from '@angular/core';
import { ActivityLogComponent } from '../features/activity-log/activity-log.component';
import { PatientsComponent } from '../features/patients/patients.component';
import { TestOrdersComponent } from '../features/test-orders/test-orders.component';

@Component({
  selector: 'app-maincontent',
  standalone: true,
  imports: [TestOrdersComponent, PatientsComponent, ActivityLogComponent],
  templateUrl: './maincontent.component.html',
  styleUrls: ['./maincontent.component.css'],
})
export class MaincontentComponent {}
