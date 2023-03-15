import {RuntimeSelectListApi} from "./runtime-select-list-api";
import {AxiosInstance} from "axios";

export class DesignerApi {
  runtimeSelectListApi: RuntimeSelectListApi;
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.runtimeSelectListApi = new RuntimeSelectListApi(httpClient);
  }

}
