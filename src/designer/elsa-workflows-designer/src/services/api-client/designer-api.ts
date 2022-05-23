import {RuntimeSelectListApi, RuntimeSelectListApiImpl} from "./runtime-select-list-api";
import {AxiosInstance} from "axios";

export interface DesignerApi {
  runtimeSelectListApi: RuntimeSelectListApi;
}

export class DesignerApiImpl implements DesignerApi {
  runtimeSelectListApi: RuntimeSelectListApi;
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
    this.runtimeSelectListApi = new RuntimeSelectListApiImpl(httpClient);
  }

}
