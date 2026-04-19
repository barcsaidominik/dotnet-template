import type { HttpHeaders } from '@angular/common/http';

export function downloadBlobFile(blob: Blob, headers: HttpHeaders, fallbackFileName: string): void {
  const fileName = getFileName(headers.get('content-disposition')) ?? fallbackFileName;
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement('a');

  anchor.href = objectUrl;
  anchor.download = fileName;
  anchor.style.display = 'none';

  document.body.append(anchor);
  anchor.click();
  anchor.remove();

  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 0);
}

function getFileName(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null;
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);

  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const asciiMatch = /filename="?([^"]+)"?/i.exec(contentDisposition);

  return asciiMatch?.[1] ?? null;
}
