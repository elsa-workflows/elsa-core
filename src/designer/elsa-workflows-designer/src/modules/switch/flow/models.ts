import {Activity, Expression} from "../../../models";

export interface FlowSwitchCase {
  label: string;
  condition: Expression;
}

export interface FlowSwitchActivity extends Activity {
  cases: Array<FlowSwitchCase>;
}
