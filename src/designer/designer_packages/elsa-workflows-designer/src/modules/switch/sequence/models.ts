import {Activity, ActivityInput, Expression} from "../../../models";

export interface SwitchCase {
  label: string;
  condition: Expression;
  activity?: Activity;
}

export interface SwitchActivity extends Activity {
  cases: Array<SwitchCase>;
  default?: Activity;
}
