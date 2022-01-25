import {Activity, ActivityInput, Expression} from "../../models";

export interface SwitchCase {
  label: string;
  condition: ActivityInput
}

export interface SwitchActivity extends Activity {
  cases: Array<SwitchCase>;
}
