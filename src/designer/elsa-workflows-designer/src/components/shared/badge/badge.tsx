import {FunctionalComponent, h} from "@stencil/core";
import { TinyColor } from '@ctrl/tinycolor';

export interface BadgeProps {
  text?: string;
  color?: string;
}

export const Badge: FunctionalComponent<BadgeProps> = ({text, color}) => {
  const foreColor = color;
  const backColor = new TinyColor(color).lighten(55).toHexString();

  const style = {
    color: foreColor,
    backgroundColor: backColor
  };

  return <span class="inline-flex items-center px-3 py-0.5 rounded-full text-sm font-medium" style={style}>{text}</span>;
};
