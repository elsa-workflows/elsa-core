import {Activity, ActivityInput} from "../../../models";

export interface FlowSendHttpRequest extends Activity {
  expectedStatusCodes: ActivityInput;
}
