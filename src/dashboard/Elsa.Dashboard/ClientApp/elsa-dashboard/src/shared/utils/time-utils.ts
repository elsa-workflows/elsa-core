import { DateTimeFormatter, LocalTime } from "js-joda";

export const formatTime = (time: LocalTime | string, defaultValue?: () => string): string => {
  return !!time
    ? parseTime(time).format(DateTimeFormatter.ofPattern('HH:mm'))
    : !!defaultValue ? defaultValue() : '';
};

export const parseTime = (time: LocalTime | string): LocalTime => {
  if(time instanceof LocalTime)
    return time;

  return LocalTime.parse(time);
};
