import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class FacilityService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getUsers(facilityId: string) {
    return this.http.get<User[]>(`${this.apiUrl}/facilities/${facilityId}/users`);
  }

  createUser(facilityId: string, email: string, role: string) {
    return this.http.post<{ userId: string; setupToken: string }>(
      `${this.apiUrl}/facilities/${facilityId}/users`,
      { email, role }
    );
  }

  removeUser(facilityId: string, userId: string) {
    return this.http.delete(`${this.apiUrl}/facilities/${facilityId}/users/${userId}`);
  }

  updateUserRole(facilityId: string, userId: string, role: string) {
    return this.http.put(`${this.apiUrl}/facilities/${facilityId}/users/${userId}/role`, { role });
  }
}
