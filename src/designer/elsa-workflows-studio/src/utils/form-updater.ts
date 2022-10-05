import {Activity} from "../modelsCopy";

export class FormUpdater {
  public static updateEditor(activity: Activity, formData: FormData): Activity {
    const newState = {...activity.state};

    formData.forEach((value, key) => {
      newState[key] = value
    });

    return {...activity, state: newState};
  }
}
