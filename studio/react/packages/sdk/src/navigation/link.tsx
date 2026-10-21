
import { useTranslation } from "../localization.js";
import { forwardRef, type ComponentProps } from "react";
import { Link as RouterLink } from "react-router-dom";

/** Preserve anchor semantics, modifiers, targets and refs used by Base UI composition. */
const Link = forwardRef<HTMLAnchorElement, Omit<ComponentProps<typeof RouterLink>, "to"> & { href: string }>(
  function Link({ href, ...props }, ref) {
  useTranslation();
    return <RouterLink ref={ref} to={href} {...props} />;
  },
);

export default Link;
