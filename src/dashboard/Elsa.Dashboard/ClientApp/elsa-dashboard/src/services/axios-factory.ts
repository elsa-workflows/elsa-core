import axios, { AxiosInstance} from 'axios';

export class AxiosFactory {

  static createDefaultClient = (): AxiosInstance => axios.create();
}
