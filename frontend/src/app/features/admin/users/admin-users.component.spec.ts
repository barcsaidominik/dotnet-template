import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../generated/client/services/admin.service';
import { FacilityUsersService } from '../../../generated/client/services/facility-users.service';
import { AuthService } from '../../../core/auth/auth.service';
import { AdminUsersPageComponent } from './admin-users.component';

const { downloadBlobFileMock } = vi.hoisted(() => ({
  downloadBlobFileMock: vi.fn(),
}));

vi.mock('../../../shared/utils/file-download.util', () => ({
  downloadBlobFile: downloadBlobFileMock,
}));

describe('AdminUsersPageComponent', () => {
  let adminApi: {
    apiAdminUsersGet$Json: ReturnType<typeof vi.fn>;
    apiAdminFacilitiesGet$Json: ReturnType<typeof vi.fn>;
    apiAdminUsersUserIdApprovePost: ReturnType<typeof vi.fn>;
    apiAdminUsersExportGet$Response: ReturnType<typeof vi.fn>;
    apiAdminUsersUserIdDelete: ReturnType<typeof vi.fn>;
  };
  let facilityUsersApi: {
    apiFacilitiesFacilityIdUsersPost$Json: ReturnType<typeof vi.fn>;
  };
  let dialog: {
    open: ReturnType<typeof vi.fn>;
  };
  let snackBar: {
    open: ReturnType<typeof vi.fn>;
  };
  let translate: {
    instant: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    downloadBlobFileMock.mockReset();

    adminApi = {
      apiAdminUsersGet$Json: vi.fn(),
      apiAdminFacilitiesGet$Json: vi.fn(),
      apiAdminUsersUserIdApprovePost: vi.fn(),
      apiAdminUsersExportGet$Response: vi.fn(),
      apiAdminUsersUserIdDelete: vi.fn(),
    };
    facilityUsersApi = {
      apiFacilitiesFacilityIdUsersPost$Json: vi.fn(),
    };
    dialog = {
      open: vi.fn(),
    };
    snackBar = {
      open: vi.fn(),
    };
    translate = {
      instant: vi.fn((key: string, params?: Record<string, unknown>) =>
        params ? `${key}:${JSON.stringify(params)}` : key
      ),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        { provide: AdminService, useValue: adminApi },
        { provide: FacilityUsersService, useValue: facilityUsersApi },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
        { provide: AuthService, useValue: { userId: signal('current-admin-id') } },
      ],
    });
  });

  it('loads users and facilities on init', async () => {
    adminApi.apiAdminUsersGet$Json.mockResolvedValue([
      {
        id: 'user-1',
        email: 'admin@test.local',
        facilityId: 'facility-1',
        isApproved: true,
        role: 'SystemAdmin',
      },
    ]);
    adminApi.apiAdminFacilitiesGet$Json.mockResolvedValue([
      {
        id: 'facility-1',
        name: 'Main Facility',
      },
    ]);
    const component = TestBed.runInInjectionContext(() => new AdminUsersPageComponent());

    component.ngOnInit();
    await Promise.resolve();
    await Promise.resolve();

    expect(adminApi.apiAdminUsersGet$Json).toHaveBeenCalled();
    expect(adminApi.apiAdminFacilitiesGet$Json).toHaveBeenCalled();
    expect(component.users()).toHaveLength(1);
    expect(component.facilities()).toHaveLength(1);
    expect(component.isLoading()).toBe(false);
  });

  it('approves a pending user and reloads the data', async () => {
    dialog.open.mockReturnValue({
      afterClosed: () =>
        of({
          facilityId: 'facility-1',
          role: 'FacilityAdmin',
        }),
    });
    adminApi.apiAdminUsersUserIdApprovePost.mockResolvedValue(undefined);
    adminApi.apiAdminUsersGet$Json.mockResolvedValue([]);
    adminApi.apiAdminFacilitiesGet$Json.mockResolvedValue([]);
    const component = TestBed.runInInjectionContext(() => new AdminUsersPageComponent());

    component.openApproveDialog({
      id: 'user-1',
      email: 'pending@test.local',
      facilityId: null,
      isApproved: false,
      role: null,
    });
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(adminApi.apiAdminUsersUserIdApprovePost).toHaveBeenCalledWith({
      userId: 'user-1',
      body: {
        facilityId: 'facility-1',
        role: 'FacilityAdmin',
      },
    });
    expect(snackBar.open).toHaveBeenCalledWith('admin.users.userApproved', 'common.close', {
      duration: 3000,
    });
  });

  it('creates a facility user and opens the setup link dialog', async () => {
    const tokenDialog = {
      componentInstance: {
        setupLink: '',
      },
    };
    dialog.open
      .mockReturnValueOnce({
        afterClosed: () =>
          of({
            email: 'new.facility.user@test.local',
            facilityId: 'facility-1',
            role: 'FacilityEditor',
          }),
      })
      .mockReturnValueOnce(tokenDialog);
    facilityUsersApi.apiFacilitiesFacilityIdUsersPost$Json.mockResolvedValue({
      userId: 'user-2',
      setupToken: 'token-123',
    });
    adminApi.apiAdminUsersGet$Json.mockResolvedValue([]);
    adminApi.apiAdminFacilitiesGet$Json.mockResolvedValue([]);
    const component = TestBed.runInInjectionContext(() => new AdminUsersPageComponent());

    component.openCreateUserDialog();
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(facilityUsersApi.apiFacilitiesFacilityIdUsersPost$Json).toHaveBeenCalledWith({
      facilityId: 'facility-1',
      body: {
        email: 'new.facility.user@test.local',
        role: 'FacilityEditor',
      },
    });
    expect(tokenDialog.componentInstance.setupLink).toContain(
      '/auth/set-password?email=new.facility.user%40test.local&token=token-123'
    );
  });

  it('exports users and delegates file download', async () => {
    const headers = new Headers() as never;
    adminApi.apiAdminUsersExportGet$Response.mockResolvedValue({
      body: new Blob(['xlsx']),
      headers,
    });
    const component = TestBed.runInInjectionContext(() => new AdminUsersPageComponent());

    component.exportUsers();
    await Promise.resolve();
    await Promise.resolve();

    expect(downloadBlobFileMock).toHaveBeenCalledWith(expect.any(Blob), headers, 'users.xlsx');
    expect(component.isExporting()).toBe(false);
  });

  it('deletes a user after confirmation and updates the local list', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    adminApi.apiAdminUsersUserIdDelete.mockResolvedValue(undefined);
    const component = TestBed.runInInjectionContext(() => new AdminUsersPageComponent());
    component.users.set([
      {
        id: 'user-1',
        email: 'pending@test.local',
        facilityId: 'facility-1',
        isApproved: false,
        role: 'FacilityViewer',
      },
    ]);

    component.deleteUser(component.users()[0]!);
    await Promise.resolve();
    await Promise.resolve();

    expect(adminApi.apiAdminUsersUserIdDelete).toHaveBeenCalledWith({ userId: 'user-1' });
    expect(component.users()).toHaveLength(0);
  });
});
