"use client";

import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export interface ConfirmOptions {
  title: ReactNode;
  description?: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  /** Optional third button between Cancel and Confirm (e.g. "Discard"); resolves with `"alternate"` in `useConfirmChoice`. */
  alternateLabel?: string;
  alternateDestructive?: boolean;
}

/** Outcome of a three-way prompt: the primary action, the alternate action, or cancel. */
export type ConfirmChoice = "confirm" | "alternate" | "cancel";

type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;
type ChoiceFn = (options: ConfirmOptions) => Promise<ConfirmChoice>;

const ConfirmContext = createContext<ChoiceFn | null>(null);

/** Promise-based confirmation: `if (await confirm({ title: "Delete?" })) …`. */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<ConfirmOptions | null>(null);
  const resolver = useRef<(v: ConfirmChoice) => void>(null);

  const choose = useCallback<ChoiceFn>((options) => {
    setState(options);
    return new Promise<ConfirmChoice>((resolve) => {
      resolver.current = resolve;
    });
  }, []);

  const finish = (value: ConfirmChoice) => {
    resolver.current?.(value);
    resolver.current = null;
    setState(null);
  };

  return (
    <ConfirmContext.Provider value={choose}>
      {children}
      <AlertDialog open={!!state} onOpenChange={(open) => !open && finish("cancel")}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{state?.title}</AlertDialogTitle>
            {state?.description && <AlertDialogDescription>{state.description}</AlertDialogDescription>}
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={() => finish("cancel")}>{state?.cancelLabel ?? "Cancel"}</AlertDialogCancel>
            {state?.alternateLabel && (
              <AlertDialogAction className={cn(buttonVariants({ variant: state.alternateDestructive ? "destructive" : "outline" }))} onClick={() => finish("alternate")}>
                {state.alternateLabel}
              </AlertDialogAction>
            )}
            <AlertDialogAction
              className={cn(state?.destructive && buttonVariants({ variant: "destructive" }))}
              onClick={() => finish("confirm")}
            >
              {state?.confirmLabel ?? "Confirm"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </ConfirmContext.Provider>
  );
}

/** Three-way prompt (Confirm / alternate / Cancel) sharing the same dialog as `useConfirm`. */
export function useConfirmChoice(): ChoiceFn {
  const ctx = useContext(ConfirmContext);
  if (!ctx) throw new Error("useConfirmChoice must be used inside <ConfirmProvider>");
  return ctx;
}

export function useConfirm(): ConfirmFn {
  const choose = useConfirmChoice();
  return useCallback<ConfirmFn>(async (options) => (await choose(options)) === "confirm", [choose]);
}
