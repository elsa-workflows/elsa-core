import { closeSync, fstatSync } from 'node:fs';
import { Socket } from 'node:net';

export type ProbeControlDescriptor = { fd: number; nonce: string };
export type ProbeControlEvent = 'begin-native-action' | 'disconnect-ready';
export interface ProbeControlChannel {
  on(event: string, listener: (...args: any[]) => void): unknown;
  off(event: string, listener: (...args: any[]) => void): unknown;
  write(value: string, callback: (error?: Error | null) => void): unknown;
  destroy(): unknown;
}
const failure = () => new Error('private_probe_control_failed');

export function validateProbeControlDescriptor(value: unknown): ProbeControlDescriptor {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw failure();
  const descriptor = value as Record<string, unknown>;
  if (Object.keys(descriptor).sort().join(',') !== 'fd,nonce' || !Number.isSafeInteger(descriptor.fd) ||
      (descriptor.fd as number) < 3 || (descriptor.fd as number) > 65535 ||
      typeof descriptor.nonce !== 'string' || !/^[0-9a-f]{64}$/.test(descriptor.nonce)) throw failure();
  return { fd: descriptor.fd as number, nonce: descriptor.nonce };
}

/** One private inherited Unix socket, one pending exchange, no stdout protocol. */
export class ProbeControl {
  private index = 0;
  private buffer = Buffer.alloc(0);
  private failed = false;
  private closed = false;
  private pending?: { event: ProbeControlEvent; resolve(): void; reject(error: Error): void; timer: ReturnType<typeof setTimeout> };
  private readonly events: ProbeControlEvent[];
  private readonly ended = () => { this.fail(); };
  private readonly data = (chunk: Buffer) => {
    if (this.closed) return;
    if (!this.pending || !Buffer.isBuffer(chunk) || this.buffer.length + chunk.length > 512) { this.fail(); return; }
    this.buffer = Buffer.concat([this.buffer, chunk]);
    const newline = this.buffer.indexOf(10);
    if (newline < 0) return;
    // Exactly one line: no unsolicited second acknowledgement or trailing bytes.
    if (newline === 0 || newline !== this.buffer.length - 1) { this.fail(); return; }
    try {
      const ack = JSON.parse(new TextDecoder('utf8', { fatal: true }).decode(this.buffer.subarray(0, newline)));
      if (!ack || typeof ack !== 'object' || Array.isArray(ack) ||
          Object.keys(ack).sort().join(',') !== 'ack,event,nonce,schema,seq' || ack.schema !== 1 || ack.ack !== true ||
          ack.nonce !== this.nonce || ack.seq !== this.index + 1 || ack.event !== this.pending.event) throw failure();
      const pending = this.pending;
      clearTimeout(pending.timer);
      this.pending = undefined;
      this.buffer = Buffer.alloc(0);
      this.index++;
      pending.resolve();
    } catch { this.fail(); }
  };

  constructor(private channel: ProbeControlChannel, private nonce: string, disconnect: boolean, private timeoutMs = 60_000) {
    if (!/^[0-9a-f]{64}$/.test(nonce) || !Number.isSafeInteger(timeoutMs) || timeoutMs < 1 || timeoutMs > 60_000) {
      channel.destroy();
      throw failure();
    }
    this.events = disconnect ? ['disconnect-ready', 'begin-native-action'] : ['begin-native-action'];
    channel.on('data', this.data);
    channel.on('end', this.ended); channel.on('close', this.ended); channel.on('error', this.ended);
  }

  private fail(): void {
    if (this.failed || this.closed) return;
    this.failed = true;
    if (this.pending) {
      clearTimeout(this.pending.timer);
      const pending = this.pending;
      this.pending = undefined;
      pending.reject(failure());
    }
    this.channel.destroy();
  }

  private async exchange(event: ProbeControlEvent): Promise<void> {
    if (this.closed || this.failed || this.pending || this.events[this.index] !== event) {
      this.fail();
      throw failure();
    }
    await new Promise<void>((resolve, reject) => {
      this.pending = { event, resolve, reject, timer: setTimeout(() => this.fail(), this.timeoutMs) };
      try {
        this.channel.write(JSON.stringify({ schema: 1, nonce: this.nonce, seq: this.index + 1, event }) + '\n',
          error => { if (error) this.fail(); });
      } catch { this.fail(); }
    });
    if (this.failed || this.closed) throw failure();
  }

  beginNativeAction(): Promise<void> { return this.exchange('begin-native-action'); }
  disconnectReady(): Promise<void> { return this.exchange('disconnect-ready'); }

  close(): void {
    const complete = !this.failed && !this.pending && this.index === this.events.length;
    this.closed = true;
    if (this.pending) {
      clearTimeout(this.pending.timer);
      this.pending.reject(failure());
      this.pending = undefined;
    }
    this.channel.off('data', this.data);
    this.channel.off('end', this.ended); this.channel.off('close', this.ended); this.channel.off('error', this.ended);
    // A delayed write error after destroy must not become an unhandled EventEmitter error.
    this.channel.on('error', () => {});
    this.channel.destroy();
    if (!complete) throw failure();
  }
}

export function openProbeControl(value: unknown, disconnect: boolean): ProbeControl {
  let descriptor: ProbeControlDescriptor | undefined;
  let socket: Socket | undefined;
  try {
    descriptor = validateProbeControlDescriptor(value);
    if (!fstatSync(descriptor.fd).isSocket()) throw failure();
    socket = new Socket({ fd: descriptor.fd, readable: true, writable: true });
    return new ProbeControl(socket, descriptor.nonce, disconnect);
  } catch {
    if (socket) socket.destroy();
    else {
      // Close only a nonstdio socket explicitly supplied through the private descriptor.
      const fd = descriptor?.fd ?? (value && typeof value === 'object' ? (value as Record<string, unknown>).fd : undefined);
      if (Number.isSafeInteger(fd) && (fd as number) >= 3 && (fd as number) <= 65535) {
        try { if (fstatSync(fd as number).isSocket()) closeSync(fd as number); } catch { /* Already closed or invalid. */ }
      }
    }
    throw failure();
  }
}
