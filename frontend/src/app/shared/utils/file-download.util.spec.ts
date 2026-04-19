import { HttpHeaders } from '@angular/common/http';
import { downloadBlobFile } from './file-download.util';

describe('downloadBlobFile', () => {
  beforeEach(() => {
    vi.useFakeTimers();

    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      writable: true,
      value: vi.fn(),
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      writable: true,
      value: vi.fn(),
    });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('uses utf-8 filename from content-disposition and revokes the object url', () => {
    const blob = new Blob(['pdf-content'], { type: 'application/pdf' });
    const headers = new HttpHeaders({
      'content-disposition': "attachment; filename*=UTF-8''report%20%C3%A9kezet.pdf",
    });
    const appendSpy = vi.spyOn(document.body, 'append');
    const createObjectUrlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test-url');
    const revokeObjectUrlSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => undefined);

    downloadBlobFile(blob, headers, 'fallback.pdf');

    expect(createObjectUrlSpy).toHaveBeenCalledWith(blob);
    expect(clickSpy).toHaveBeenCalledOnce();
    expect(appendSpy).toHaveBeenCalledOnce();

    const anchor = appendSpy.mock.calls[0]?.[0] as HTMLAnchorElement;
    expect(anchor.download).toBe('report ékezet.pdf');
    expect(anchor.href).toBe('blob:test-url');

    vi.runAllTimers();

    expect(revokeObjectUrlSpy).toHaveBeenCalledWith('blob:test-url');
  });

  it('falls back to the provided file name when headers do not include one', () => {
    const blob = new Blob(['xlsx-content'], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    });
    const appendSpy = vi.spyOn(document.body, 'append');
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:fallback');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    downloadBlobFile(blob, new HttpHeaders(), 'fallback.xlsx');

    const anchor = appendSpy.mock.calls[0]?.[0] as HTMLAnchorElement;
    expect(anchor.download).toBe('fallback.xlsx');
  });
});
