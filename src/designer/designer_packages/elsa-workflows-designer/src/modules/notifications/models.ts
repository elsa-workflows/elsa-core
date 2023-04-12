import {Moment} from "moment";

export interface NotificationType {
  id?: number | any;
  title: string;
  text: string | any;
  type?: NotificationDisplayType;
  timestamp?: Moment;
  showToast?: boolean;
}

export enum NotificationDisplayType {
  Success,
  Warning,
  Error,
  InProgress,
}
