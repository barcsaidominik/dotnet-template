import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../generated/client/services/admin.service';
import { AdminFacilitiesPageComponent } from './admin-facilities.component';

const { downloadBlobFileMock } = vi.hoisted(() => ({
  downloadBlobFileMock: vi.fn(),
}));

vi.mock('../../../shared/utils/file-download.util', () => ({
  downloadBlobFile: downloadBlobFileMock,
}));

describe('AdminFacilitiesPageComponent', () => {
  let adminApi: {
    apiAdminFacilitiesGet$Json: ReturnType<typeof vi.fn>;
    apiAdminFacilitiesPost$Json: ReturnType<typeof vi.fn>;
    apiAdminFacilitiesFacilityIdPut: ReturnType<typeof vi.fn>;
    apiAdminFacilitiesFacilityIdDelete: ReturnType<typeof vi.fn>;
    apiAdminFacilitiesExportGet$Response: ReturnType<typeof vi.fn>;
    apiAdminUsersGet$Json: ReturnType<typeof vi.fn>;
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
      apiAdminFacilitiesGet$Json: vi.fn(),
      apiAdminFacilitiesPost$Json: vi.fn(),
      apiAdminFacilitiesFacilityIdPut: vi.fn(),
      apiAdminFacilitiesFacilityIdDelete: vi.fn(),
      apiAdminFacilitiesExportGet$Response: vi.fn(),
      apiAdminUsersGet$Json: vi.fn().mockResolvedValue([]),
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
        { provide: AdminService, useValue: adminApi },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
      ],
    });
  });

  it('loads facilities on init', async () => {
    adminApi.apiAdminFacilitiesGet$Json.mockResolvedValue([
      {
        id: 'facility-1',
        name: 'Main Facility',
        employeeCount: 5,
        rowVersion: 1,
      },
    ]);
    const component = TestBed.runInInjectionContext(() => new AdminFacilitiesPageComponent());

    component.ngOnInit();
    await Promise.resolve();
    await Promise.resolve();

    expect(adminApi.apiAdminFacilitiesGet$Json).toHaveBeenCalled();
    expect(component.facilities()).toHaveLength(1);
    expect(component.isLoading()).toBe(false);
  });

  it('creates a facility from dialog result and reloads the list', async () => {
    dialog.open.mockReturnValue({
      afterClosed: () => of('Created Facility'),
    });
    adminApi.apiAdminFacilitiesPost$Json.mockResolvedValue('facility-2');
    adminApi.apiAdminFacilitiesGet$Json.mockResolvedValue([]);
    const component = TestBed.runInInjectionContext(() => new AdminFacilitiesPageComponent());

    component.openCreateDialog();
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(adminApi.apiAdminFacilitiesPost$Json).toHaveBeenCalledWith({
      body: { name: 'Created Facility' },
    });
    expect(snackBar.open).toHaveBeenCalledWith('admin.facilities.facilityCreated', 'common.close', {
      duration: 3000,
    });
  });

  it('updates a facility from dialog result and patches local state', async () => {
    dialog.open.mockReturnValue({
      afterClosed: () => of('Updated Facility'),
    });
    adminApi.apiAdminFacilitiesFacilityIdPut.mockResolvedValue(undefined);
    const component = TestBed.runInInjectionContext(() => new AdminFacilitiesPageComponent());
    component.facilities.set([
      {
        id: 'facility-1',
        name: 'Old Facility',
        employeeCount: 3,
        rowVersion: 42,
      },
    ]);

    component.openEditDialog(component.facilities()[0]!);
    await Promise.resolve();
    await Promise.resolve();

    expect(adminApi.apiAdminFacilitiesFacilityIdPut).toHaveBeenCalledWith({
      facilityId: 'facility-1',
      body: { name: 'Updated Facility', rowVersion: 42 },
    });
    expect(component.facilities()[0]?.name).toBe('Updated Facility');
  });

  it('deletes a facility after confirmation and updates the local list', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    adminApi.apiAdminFacilitiesFacilityIdDelete.mockResolvedValue(undefined);
    const component = TestBed.runInInjectionContext(() => new AdminFacilitiesPageComponent());
    component.facilities.set([
      {
        id: 'facility-1',
        name: 'Main Facility',
        employeeCount: 0,
        rowVersion: 1,
      },
    ]);

    component.deleteFacility(component.facilities()[0]!);
    await Promise.resolve();
    await Promise.resolve();

    expect(adminApi.apiAdminFacilitiesFacilityIdDelete).toHaveBeenCalledWith({
      facilityId: 'facility-1',
    });
    expect(component.facilities()).toHaveLength(0);
  });

  it('shows generic error when deletion fails', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    adminApi.apiAdminFacilitiesFacilityIdDelete.mockRejectedValue({
      error: {
        detail: 'Facility cannot be deleted while products still reference it.',
      },
    });
    const component = TestBed.runInInjectionContext(() => new AdminFacilitiesPageComponent());
    component.facilities.set([
      {
        id: 'facility-1',
        name: 'Main Facility',
        employeeCount: 0,
        rowVersion: 1,
      },
    ]);

    component.deleteFacility(component.facilities()[0]!);
    await Promise.resolve();
    await Promise.resolve();

    expect(consoleSpy).toHaveBeenCalledWith('Failed to delete facility:', expect.any(Object));
    expect(snackBar.open).toHaveBeenCalledWith(
      'errors.Error.Unexpected',
      'common.close',
      { duration: 4000 }
    );
    consoleSpy.mockRestore();
  });

  it('exports facilities and delegates file download', async () => {
    const headers = new Headers() as never;
    adminApi.apiAdminFacilitiesExportGet$Response.mockResolvedValue({
      body: new Blob(['xlsx']),
      headers,
    });
    const component = TestBed.runInInjectionContext(() => new AdminFacilitiesPageComponent());

    component.exportFacilities();
    await Promise.resolve();
    await Promise.resolve();

    expect(downloadBlobFileMock).toHaveBeenCalledWith(expect.any(Blob), headers, 'facilities.xlsx');
    expect(component.isExporting()).toBe(false);
  });
});
