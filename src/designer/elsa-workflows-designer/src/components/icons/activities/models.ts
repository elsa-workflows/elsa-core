export enum ActivityIconSize {
  Small,
  Medium,
  Large
}

export interface ActivityIconSettings {
  size?: ActivityIconSize;
}

export function getActivityIconCssClass(settings: ActivityIconSettings) {
  const cssClasses = ['text-white', sizeToClass(settings.size)];
  return cssClasses.join(' ');
}

function sizeToClass(size?: ActivityIconSize) {
  switch (size) {
    case ActivityIconSize.Small:
      return 'h-4 w-4';
    case ActivityIconSize.Medium:
      return 'h-6 w-6';
    case ActivityIconSize.Large:
      return 'h-10 w-10';
    default:
      return 'h-6 w-6';
  }
}
