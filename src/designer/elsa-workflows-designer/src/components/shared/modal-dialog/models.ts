import {ModalType} from "../../../components/shared/modal-dialog";

export interface ModalDialogInstance {
  content: () => any;
  actions: Array<ActionDefinition>;
  modalType: ModalType;
}

export type ActionClickArgs = (e: Event, action: ActionDefinition) => void;

export interface ActionDefinition {
  text: string;
  name?: string;
  isPrimary?: boolean;
  isDangerous?: boolean;
  type?: ActionType;
  onClick?: ActionClickArgs;
  display?: (button: ActionDefinition) => any;
}

export interface ActionInvokedArgs {
  action: ActionDefinition;
}

export enum ActionType {
  Button,
  Submit,
  Cancel
}

export class DefaultActions {

  public static Cancel = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Cancel',
    name: 'Cancel',
    type: ActionType.Cancel,
    onClick: handler
  });

  public static Close = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Close',
    name: 'Close',
    type: ActionType.Cancel,
    onClick: handler
  });

  public static Save = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Save',
    name: 'Save',
    type: ActionType.Submit,
    isPrimary: true,
    onClick: handler
  });

  public static Delete = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'Delete',
    name: 'Delete',
    type: ActionType.Button,
    isDangerous: true,
    onClick: handler
  });

  public static New = (handler?: ActionClickArgs): ActionDefinition => ({
    text: 'New',
    name: 'New',
    type: ActionType.Button,
    isPrimary: true,
    onClick: handler
  });
}

// export class DefaultContents {
//   public static Delete = (message: string): any => {
//     return(
//       <div class="relative p-4 w-full max-w-md h-full md:h-auto">
//         <div class="relative bg-white rounded-lg shadow dark:bg-gray-700">
//             <button type="button" class="absolute top-3 right-2.5 text-gray-400 bg-transparent hover:bg-gray-200 hover:text-gray-900 rounded-lg text-sm p-1.5 ml-auto inline-flex items-center dark:hover:bg-gray-800 dark:hover:text-white" data-modal-toggle="popup-modal">
//                 <svg aria-hidden="true" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path></svg>
//                 <span class="sr-only">Close modal</span>
//             </button>
//             <div class="p-6 text-center">
//                 <svg aria-hidden="true" class="mx-auto mb-4 w-14 h-14 text-gray-400 dark:text-gray-200" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
//                 <h3 class="mb-5 text-lg font-normal text-gray-500 dark:text-gray-400">{message}</h3>
//                 <button data-modal-toggle="popup-modal" type="button" class="text-white bg-red-600 hover:bg-red-800 focus:ring-4 focus:outline-none focus:ring-red-300 dark:focus:ring-red-800 font-medium rounded-lg text-sm inline-flex items-center px-5 py-2.5 text-center mr-2">
//                     Yes, I'm sure
//                 </button>
//                 <button data-modal-toggle="popup-modal" type="button" class="text-gray-500 bg-white hover:bg-gray-100 focus:ring-4 focus:outline-none focus:ring-gray-200 rounded-lg border border-gray-200 text-sm font-medium px-5 py-2.5 hover:text-gray-900 focus:z-10 dark:bg-gray-700 dark:text-gray-300 dark:border-gray-500 dark:hover:text-white dark:hover:bg-gray-600 dark:focus:ring-gray-600">No, cancel</button>
//             </div>
//         </div>
//       </div>
//     )
//   };
// }