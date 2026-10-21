
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { Button } from "./button.js";
import { Tooltip, TooltipTrigger, TooltipContent } from "./tooltip.js";
export function IconButton({ label, children, ...props }: React.ComponentProps<typeof Button> & { label: string }) {
  useTranslation();
  return (
    <Tooltip>
      <TooltipTrigger render={<Button variant="ghost" size="icon-sm" aria-label={label} {...props} />}>{children}</TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}
