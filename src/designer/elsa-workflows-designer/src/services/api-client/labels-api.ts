import {AxiosInstance} from "axios";

export interface LabelsApi {

}

export class LabelsApiImpl implements LabelsApi {
  private httpClient: AxiosInstance;

  constructor(httpClient: AxiosInstance) {
    this.httpClient = httpClient;
  }
}
