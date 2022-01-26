import {Activity, ActivityInput, Expression} from "../../models";

export interface SwitchCase {
  label: string;
  condition: Expression;
}

export interface SwitchActivity extends Activity {
  cases: ActivityInput;
}
