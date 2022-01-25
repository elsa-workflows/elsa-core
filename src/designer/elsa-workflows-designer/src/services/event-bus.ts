import {Service} from 'typedi';
import CustomEventBus, {EventCallback} from './custom-event-bus';

@Service()
export class EventBus {
  private eventBus = new CustomEventBus();

  async emit<C = null>(eventName: string, context?: C, ...args: any[]): Promise<void> {
    await this.eventBus.emit<C>(eventName, context, ...args);
  }

  on(eventName: string, callback: EventCallback): void {
    this.eventBus.on(eventName, callback);
  }

  off(eventName: string): void {
    this.eventBus.off(eventName);
  }
}
