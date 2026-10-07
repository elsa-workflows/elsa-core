// Synthetic in-memory protocol contracts only. No native requests or processes.
import assert from 'node:assert/strict';
import { EventEmitter } from 'node:events';
import { ProbeControl, validateProbeControlDescriptor } from './private-probe-control.js';

const nonce = 'a'.repeat(64);
class Channel extends EventEmitter {
  writes: string[] = [];
  destroyed = false;
  write(value: string, callback: (error?: Error | null) => void): void { this.writes.push(value); callback(); }
  destroy(): void { this.destroyed = true; }
  acknowledge(update: Record<string, unknown> = {}): void {
    this.emit('data', Buffer.from(JSON.stringify({ ...JSON.parse(this.writes.at(-1)!), ack: true, ...update }) + '\n'));
  }
}
const create = (disconnect = false, timeout = 1000) => {
  const channel = new Channel();
  return { channel, control: new ProbeControl(channel, nonce, disconnect, timeout) };
};
assert.deepEqual(validateProbeControlDescriptor({ fd: 3, nonce }), { fd: 3, nonce });
for (const value of [{ fd: 1, nonce }, { fd: 3.5, nonce }, { fd: 65536, nonce }, { fd: 3, nonce: 'bad' },
  { fd: 3, nonce, token: 'private' }, null, []]) assert.throws(() => validateProbeControlDescriptor(value), /private_probe_control_failed/);
{
  const { channel, control } = create();
  const done = control.beginNativeAction();
  assert.deepEqual(JSON.parse(channel.writes[0]), { schema: 1, nonce, seq: 1, event: 'begin-native-action' });
  const ack = Buffer.from(JSON.stringify({ schema: 1, nonce, seq: 1, event: 'begin-native-action', ack: true }) + '\n');
  channel.emit('data', ack.subarray(0, 9)); channel.emit('data', ack.subarray(9));
  await done;
  control.close();
  assert.equal(channel.destroyed, true);
  assert.equal(channel.listenerCount('data'), 0);
  assert.doesNotThrow(() => channel.emit('error', new Error('synthetic late write failure')));
}
{
  const { channel, control } = create(true);
  let done = control.disconnectReady(); channel.acknowledge(); await done;
  done = control.beginNativeAction(); channel.acknowledge(); await done;
  assert.equal(JSON.parse(channel.writes[1]).seq, 2);
  control.close();
}
for (const update of [{ nonce: 'b'.repeat(64) }, { seq: 2 }, { event: 'disconnect-ready' }, { ack: false },
  { schema: 2 }, { extra: 'unsafe' }]) {
  const { channel, control } = create();
  const done = control.beginNativeAction(); channel.acknowledge(update);
  await assert.rejects(done, /private_probe_control_failed/);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
const canonicalAck = JSON.stringify({ schema: 1, nonce, seq: 1, event: 'begin-native-action', ack: true });
for (const line of [canonicalAck.replace('"ack":true', '"ack":true,"ack":true'),
  canonicalAck.replace('"schema":1', '"schema":1,"schema":1'),
  canonicalAck.replace('"nonce":', '"nonce":"' + nonce + '","nonce":'),
  ' ' + canonicalAck, canonicalAck + '\r', canonicalAck.replace('"schema":1,', '"schema":1, ')]) {
  const { channel, control } = create();
  const done = control.beginNativeAction(); channel.emit('data', Buffer.from(line + '\n'));
  await assert.rejects(done, /private_probe_control_failed/);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
for (const event of ['end', 'close', 'error']) {
  const { channel, control } = create();
  const done = control.beginNativeAction(); channel.emit(event);
  await assert.rejects(done, /private_probe_control_failed/);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
for (const value of [Buffer.from('bad\n'), Buffer.alloc(513), Buffer.from('{}\n{}\n'), Buffer.from('{}\ntrailing'),
  Buffer.from([0xff, 10])]) {
  const { channel, control } = create();
  const done = control.beginNativeAction(); channel.emit('data', value);
  await assert.rejects(done, /private_probe_control_failed/);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
{
  const { channel, control } = create(true);
  await assert.rejects(control.beginNativeAction(), /private_probe_control_failed/);
  assert.equal(channel.writes.length, 0);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
{
  const { channel, control } = create();
  const first = control.beginNativeAction();
  const firstRejected = assert.rejects(first, /private_probe_control_failed/);
  await assert.rejects(control.beginNativeAction(), /private_probe_control_failed/);
  await firstRejected;
  assert.equal(channel.destroyed, true);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
{
  const { control } = create(false, 5);
  await assert.rejects(control.beginNativeAction(), /private_probe_control_failed/);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
for (const throws of [false, true]) {
  const { channel, control } = create();
  channel.write = (_value, callback) => {
    if (throws) throw new Error('synthetic private write failure');
    callback(new Error('synthetic private write failure'));
  };
  await assert.rejects(control.beginNativeAction(), /private_probe_control_failed/);
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
{
  const { control } = create();
  const done = control.beginNativeAction();
  const rejected = assert.rejects(done, /private_probe_control_failed/);
  assert.throws(() => control.close(), /private_probe_control_failed/);
  await rejected;
}
{
  const { channel, control } = create();
  const done = control.beginNativeAction(); channel.acknowledge(); await done;
  channel.emit('data', Buffer.from('{}\n'));
  assert.throws(() => control.close(), /private_probe_control_failed/);
}
process.stdout.write('private optional probe control contracts passed\n');
