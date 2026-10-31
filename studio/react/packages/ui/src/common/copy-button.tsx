"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Check, Copy } from "lucide-react";
import { useState } from "react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@elsa-workflows/react-studio-ui/tooltip";

export function CopyButton({ value, label = "Copy" }: { value: string; label?: string }) {
  const _t = useTranslation();
  const [copied, setCopied] = useState(false);
  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <Button
            variant="ghost"
            size="icon-xs"
            aria-label={label}
            onClick={async () => {
              await navigator.clipboard.writeText(value);
              setCopied(true);
              setTimeout(() => setCopied(false), 1500);
            }}
          />
        }
      >
        {copied ? <Check className="text-emerald-600" /> : <Copy />}
      </TooltipTrigger>
      <TooltipContent>{copied ? _t("Copied") : label}</TooltipContent>
    </Tooltip>
  );
}
