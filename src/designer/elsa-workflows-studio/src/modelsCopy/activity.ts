import {ActivityMessage} from "./activity-message";

export interface Activity {
  id: string
  type: string
  left: number
  top: number
  state: any
  blocking?: boolean
  executed?: boolean
  faulted?: boolean
  message?: ActivityMessage
}

