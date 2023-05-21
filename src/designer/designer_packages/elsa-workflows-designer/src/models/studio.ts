import {Container} from "typedi";
import {PluginRegistry} from "../services";

export interface StudioInitializingContext {
  container: Container;
  pluginRegistry: PluginRegistry;
}
