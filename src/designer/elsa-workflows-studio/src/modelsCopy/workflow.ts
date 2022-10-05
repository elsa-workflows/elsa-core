import {Activity} from "./activity";
import {Connection} from "./connection";

export type Workflow = {
  activities: Array<Activity>
  connections: Array<Connection>

}
