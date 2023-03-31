import {Service} from "typedi";
import store from "../data/server-store";

@Service()
export class ServerSettings {
  get baseAddress(): string {
    return store.serverAddress;
  }

  set baseAddress(value: string) {
    store.serverAddress = value;
  }
}
