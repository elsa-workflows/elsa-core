"use client";

import { Search, X } from "lucide-react";
import { useEffect, useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

/** Debounced search box; `onChange` fires after the user pauses typing. */
export function SearchInput({
  value,
  onChange,
  placeholder = "Search…",
  delay = 400,
  className,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  delay?: number;
  className?: string;
}) {
  const [text, setText] = useState(value);
  const [lastValue, setLastValue] = useState(value);
  if (lastValue !== value) {
    // Prop changed from outside (URL navigation): adopt it without an extra effect pass.
    setLastValue(value);
    setText(value);
  }

  useEffect(() => {
    if (text === value) return;
    const t = setTimeout(() => onChange(text), delay);
    return () => clearTimeout(t);
  }, [text, value, delay, onChange]);

  return (
    <div className={cn("relative", className)}>
      <Search className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
      <Input value={text} onChange={(e) => setText(e.target.value)} placeholder={placeholder} className="pl-8 pr-8" />
      {text && (
        <Button
          variant="ghost"
          size="icon-xs"
          className="absolute right-1 top-1/2 -translate-y-1/2"
          onClick={() => {
            setText("");
            onChange("");
          }}
          aria-label="Clear search"
        >
          <X />
        </Button>
      )}
    </div>
  );
}
