import {Container} from "typedi";
import {PluginRegistry} from "../services";

export interface ShellInitializingContext {
  container: Container;
  pluginRegistry: PluginRegistry;
}
