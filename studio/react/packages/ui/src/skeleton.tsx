
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { cn } from "cn"

function Skeleton({ className, ...props }: React.ComponentProps<"div">) {
  useTranslation();
  return (
    <div
      data-slot="skeleton"
      className={cn("animate-pulse rounded-md bg-muted", className)}
      {...props}
    />
  )
}

export { Skeleton }
