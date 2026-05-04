/// <reference types="vitest/globals" />
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { FacilityUsersService } from '../../../generated/client/services/facility-users.service';
import { AuthService } from '../../../core/auth/auth.service';
import { FacilityUsersPageComponent } from './facility-users.component';

describe('FacilityUsersPageComponent', () => {
  let facilityUsersApi: {
    apiFacilitiesFacilityIdUsersGet$Json: ReturnType<typeof vi.fn>;
    apiFacilitiesFacilityIdUsersPost$Json: ReturnType<typeof vi.fn>;
    apiFacilitiesFacilityIdUsersUserIdRolePut: ReturnType<typeof vi.fn>;
    apiFacilitiesFacilityIdUsersUserIdDelete: ReturnType<typeof vi.fn>;
  };
  let auth: {
    facilityId: ReturnType<typeof signal>;
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
    facilityUsersApi = {
      apiFacilitiesFacilityIdUsersGet$Json: vi.fn(),
      apiFacilitiesFacilityIdUsersPost$Json: vi.fn(),
      apiFacilitiesFacilityIdUsersUserIdRolePut: vi.fn(),
      apiFacilitiesFacilityIdUsersUserIdDelete: vi.fn(),
    };
    auth = {
      facilityId: signal<string | null>('facility-1'),
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
        { provide: FacilityUsersService, useValue: facilityUsersApi },
        { provide: AuthService, useValue: auth },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
      ],
    });
  });

  it('loads users on init when the current session has a facility id', async () => {
    facilityUsersApi.apiFacilitiesFacilityIdUsersGet$Json.mockResolvedValue([
      {
        id: 'user-1',
        email: 'viewer@test.local',
        facilityId: 'facility-1',
        isApproved: true,
        role: 'FacilityViewer',
      },
    ]);
    const component = TestBed.runInInjectionContext(() => new FacilityUsersPageComponent());

    component.ngOnInit();
    await Promise.resolve();
    await Promise.resolve();

    expect(facilityUsersApi.apiFacilitiesFacilityIdUsersGet$Json).toHaveBeenCalledWith({
      facilityId: 'facility-1',
    });
    expect(component.users()).toHaveLength(1);
    expect(component.isLoading()).toBe(false);
  });

  it('skips loading and stops the spinner when no facility id is available', () => {
    auth.facilityId.set(null);
    const component = TestBed.runInInjectionContext(() => new FacilityUsersPageComponent());

    component.ngOnInit();

    expect(facilityUsersApi.apiFacilitiesFacilityIdUsersGet$Json).not.toHaveBeenCalled();
    expect(component.isLoading()).toBe(false);
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
            email: 'new.user@test.local',
            role: 'FacilityEditor',
          }),
      })
      .mockReturnValueOnce(tokenDialog);
    facilityUsersApi.apiFacilitiesFacilityIdUsersPost$Json.mockResolvedValue({
      userId: 'user-2',
      setupToken: 'token-123',
    });
    facilityUsersApi.apiFacilitiesFacilityIdUsersGet$Json.mockResolvedValue([]);
    const component = TestBed.runInInjectionContext(() => new FacilityUsersPageComponent());

    component.openCreateDialog();
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(facilityUsersApi.apiFacilitiesFacilityIdUsersPost$Json).toHaveBeenCalledWith({
      facilityId: 'facility-1',
      body: {
        email: 'new.user@test.local',
        role: 'FacilityEditor',
      },
    });
    expect(tokenDialog.componentInstance.setupLink).toContain(
      '/auth/set-password?email=new.user%40test.local&token=token-123'
    );
  });

  it('updates the local user role after a successful role change', async () => {
    facilityUsersApi.apiFacilitiesFacilityIdUsersUserIdRolePut.mockResolvedValue(undefined);
    const component = TestBed.runInInjectionContext(() => new FacilityUsersPageComponent());
    component.users.set([
      {
        id: 'user-1',
        email: 'viewer@test.local',
        facilityId: 'facility-1',
        isApproved: true,
        role: 'FacilityViewer',
      },
    ]);

    component.changeRole(component.users()[0]!, 'FacilityEditor');
    await Promise.resolve();
    await Promise.resolve();

    expect(facilityUsersApi.apiFacilitiesFacilityIdUsersUserIdRolePut).toHaveBeenCalledWith({
      facilityId: 'facility-1',
      userId: 'user-1',
      body: { role: 'FacilityEditor' },
    });
    expect(component.users()[0]?.role).toBe('FacilityEditor');
  });

  it('removes a user after confirmation and successful deletion', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    facilityUsersApi.apiFacilitiesFacilityIdUsersUserIdDelete.mockResolvedValue(undefined);
    const component = TestBed.runInInjectionContext(() => new FacilityUsersPageComponent());
    component.users.set([
      {
        id: 'user-1',
        email: 'viewer@test.local',
        facilityId: 'facility-1',
        isApproved: true,
        role: 'FacilityViewer',
      },
    ]);

    component.removeUser(component.users()[0]!);
    await Promise.resolve();
    await Promise.resolve();

    expect(facilityUsersApi.apiFacilitiesFacilityIdUsersUserIdDelete).toHaveBeenCalledWith({
      facilityId: 'facility-1',
      userId: 'user-1',
    });
    expect(component.users()).toHaveLength(0);
  });
});
