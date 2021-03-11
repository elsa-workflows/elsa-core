export interface DownloadOptions {
  fileName?: string;
  contentType?: string;
}

export function downloadFromUrl(url: string, options: DownloadOptions) {
  const anchorElement = document.createElement('a');
  anchorElement.href = url;
  anchorElement.download = options.fileName !== null && options.fileName !== void 0 ? options.fileName : '';
  anchorElement.click();
  anchorElement.remove();
}

export function downloadFromBytes(content: Uint8Array, options: DownloadOptions) {
  const contentType = options.contentType || 'application/octet-stream';
  const base64String = btoa(String.fromCharCode(...content));
  const url = `data:${contentType}";base64,${base64String}`;
  downloadFromUrl(url, {fileName: options.fileName});
}

export function downloadFromText(content: string, options: DownloadOptions) {
  const utf8Encode = new TextEncoder();
  const bytes = utf8Encode.encode(content);
  downloadFromBytes(bytes, options)
}

export function downloadFromBlob(content: Blob, options: DownloadOptions) {
  const url = window.URL.createObjectURL(content);
  downloadFromUrl(url, options)
}
