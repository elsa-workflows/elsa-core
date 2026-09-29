import {Activity} from "./activity";

export interface Container extends Activity {
    activities: Array<Activity>;
}