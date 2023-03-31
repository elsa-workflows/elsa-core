export interface Plugin {
  initialize(): Promise<void>;
}
