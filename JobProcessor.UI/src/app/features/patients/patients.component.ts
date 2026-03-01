import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AgGridAngular } from 'ag-grid-angular';
import { type ColDef } from 'ag-grid-community';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';

import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { PatientListItem } from './shared/patient.model';
import { PatientsService } from './shared/patients.service';

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [
    CommonModule,
    AgGridAngular,
    MatCardModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './patients.component.html',
  styleUrls: ['./patients.component.css'],
})
export class PatientsComponent implements OnInit {
  readonly defaultColDef: ColDef = {
    sortable: true,
    resizable: true,
  };

  readonly columnDefs: ColDef<PatientListItem>[] = [
    { field: 'patientId', headerName: 'Patient #', width: 185 },
    { field: 'name', headerName: 'Name', flex: 1, minWidth: 140 },
    {
      field: 'dateOfBirth',
      headerName: 'DOB',
      width: 150,
      valueFormatter: (params) =>
        params.value
          ? new Date(params.value as string).toLocaleDateString()
          : '',
    },
  ];

  readonly noRowsOverlayComponent = EmptyStateComponent;
  readonly noRowsOverlayComponentParams = {
    noRowsMessage: 'No patients found',
    icon: '👤',
  };

  patients: PatientListItem[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private readonly patientsService: PatientsService) {}

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.patientsService
      .getAll()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.patients = response;
        },
        error: () => {
          this.errorMessage = 'Unable to load patients.';
        },
      });
  }
}
