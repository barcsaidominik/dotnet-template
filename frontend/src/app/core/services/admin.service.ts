import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';
import { Facility } from '../models/facility.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getUsers() {
    return this.http.get<User[]>(`${this.apiUrl}/admin/users`);
  }

  approveUser(id: string, facilityId: string, role: string) {
    return this.http.post(`${this.apiUrl}/admin/users/${id}/approve`, { facilityId, role });
  }

  deleteUser(id: string) {
    return this.http.delete(`${this.apiUrl}/admin/users/${id}`);
  }

  getFacilities() {
    return this.http.get<Facility[]>(`${this.apiUrl}/admin/facilities`);
  }

  createFacility(name: string) {
    return this.http.post<{ id: string }>(`${this.apiUrl}/admin/facilities`, { name });
  }

  deleteFacility(id: string) {
    return this.http.delete(`${this.apiUrl}/admin/facilities/${id}`);
  }
}
