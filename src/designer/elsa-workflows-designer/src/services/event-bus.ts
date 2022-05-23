import {Service} from 'typedi';
import CustomEventBus, {EventCallback} from './custom-event-bus';

@Service()
export class EventBus {
  private eventBus = new CustomEventBus();

  /**
   * Emit the event asynchronously.
   * @param {string} eventName - name of the event.
   * @param context
   * @param args
   */
  async emit<C = null>(eventName: string, context?: C, ...args: any[]): Promise<void> {
    await this.eventBus.emit<C>(eventName, context, ...args);
  }

  /**
   * Attach a callback to an event.
   * @param {string} eventName - name of the event.
   * @param {function} callback - callback executed when this event is triggered
   */
  on(eventName: string, callback: EventCallback): void {
    this.eventBus.on(eventName, callback);
  }

  /**
   * Kill an event with all it's callbacks.
   * @param {string} eventName - name of the event.
   */
  off(eventName: string): void {
    this.eventBus.off(eventName);
  }

  /**
   * Remove the callback for the given event.
   * @param {string} eventName - name of the event.
   * @param {callback} callback - the callback to remove (undefined to remove all of them).
   */
  detach(eventName: string, callback?: EventCallback): void {
    this.eventBus.off(eventName);
  }
}
