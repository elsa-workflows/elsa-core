import {AxiosInstance} from "axios";
import {JavaScriptApi} from "./javascript-api";

export class ScriptingApi {
  javaScriptApi: JavaScriptApi;

  constructor(httpClient: AxiosInstance) {
    this.javaScriptApi = new JavaScriptApi(httpClient);
  }

}
