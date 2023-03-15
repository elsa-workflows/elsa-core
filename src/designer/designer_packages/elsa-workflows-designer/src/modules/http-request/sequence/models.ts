import {Activity} from "../../../models";

export interface SendHttpRequest extends Activity {
  expectedStatusCodes: Array<HttpStatusCodeCase>;
  unmatchedStatusCode?: Activity;
}

export interface HttpStatusCodeCase {
  statusCode: number;
  activity?: Activity;
}
