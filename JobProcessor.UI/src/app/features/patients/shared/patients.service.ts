import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PatientListItem } from './patient.model';

@Injectable({
  providedIn: 'root',
})
export class PatientsService {
  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<PatientListItem[]> {
    return this.http.get<PatientListItem[]>('/patients');
  }
}
