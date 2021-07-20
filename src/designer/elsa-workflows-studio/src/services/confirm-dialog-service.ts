import {eventBus} from './event-bus';
import {EventTypes} from "../models";

export class ConfirmDialogService {
  show(caption: string, message: string): Promise<boolean> {
    const context = {caption, message, promise: null};
    eventBus.emit(EventTypes.ShowConfirmDialog, this, context);
    
    return context.promise;
  }

  hide() {
    eventBus.emit(EventTypes.HideConfirmDialog, this);
  }
}

export const confirmDialogService = new ConfirmDialogService();
