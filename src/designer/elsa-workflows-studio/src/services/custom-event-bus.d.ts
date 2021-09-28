declare type EventCallback = (...args: any[]) => void;
export default class EventBus{
  /**
  * Attach a callback to an event
  * @param {string} eventName - name of the event.
  * @param {function} callback - callback executed when this event is triggered
  */
  on(eventName: string, callback: EventCallback): void
  /**
   * Attach a callback to an event. This callback will not be executed more than once if the event is trigger mutiple times
   * @param {string} eventName - name of the event.
   * @param {function} callback - callback executed when this event is triggered
   */
  once(eventName: string, callback: EventCallback): void
  /**
  * Attach a callback to an event. This callback will be executed will not be executed more than the number if the event is trigger mutiple times
  * @param {number} number - max number of executions
  * @param {string} eventName - name of the event.
  * @param {function} callback - callback executed when this event is triggered
  */
  exactly(number: number, eventName: string, callback: EventCallback): void
  /**
   * Kill an event with all it's callbacks
   * @param {string} eventName - name of the event.
   */
  die(eventName: string): void
  /**
   * Kill an event with all it's callbacks
   * @param {string} eventName - name of the event.
   */
  off(eventName: string): void
  /**
  * Remove the callback for the given event
  * @param {string} eventName - name of the event.
  * @param {callback} callback - the callback to remove (undefined to remove all of them).
  */
  detach(eventName: string, callback?: EventCallback): boolean
  /**
  * Remove all the events
  */
  detachAll(): void
  /**
   * Emit the event
   * @param {string} eventName - name of the event.
   */
  emit<C = null>(eventName: string, context?: C, ...args: any[]): Promise<void>
  /**
   * Emit the event asynchronously
   * @param {string} eventName - name of the event.
   */
}
