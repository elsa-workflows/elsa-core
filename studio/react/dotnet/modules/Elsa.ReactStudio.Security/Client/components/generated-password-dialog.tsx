"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { KeyRound } from "lucide-react";
import { CopyButton } from "@elsa-workflows/react-studio-ui/common/copy-button";
import { Alert, AlertDescription, AlertTitle } from "@elsa-workflows/react-studio-ui/alert";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";

/** Shows a server-generated password exactly once; it cannot be retrieved after closing. */
export function GeneratedPasswordDialog({
  userName,
  password,
  onClose,
}: {
  userName: string;
  password: string | undefined;
  onClose: () => void;
}) {
  const _t = useTranslation();
  return (
    <Dialog open={!!password} onOpenChange={(open) => !open && onClose()}>
      <DialogContent showCloseButton={false}>
        <DialogHeader>
          <DialogTitle>{_t("Password for")}{" "}{userName}</DialogTitle>
          <DialogDescription>{_t("The server generated a password for this user.")}</DialogDescription>
        </DialogHeader>
        <div className="flex items-center gap-2 rounded-md border bg-muted/40 px-3 py-2 font-mono text-sm break-all">
          <span className="flex-1">{password}</span>
          <CopyButton value={password ?? ""} label={_t("Copy password")} />
        </div>
        <Alert>
          <KeyRound />
          <AlertTitle>{_t("Shown only once")}</AlertTitle>
          <AlertDescription>{_t("Copy it now. It will not be displayed again; you can set a new one by editing the user.")}</AlertDescription>
        </Alert>
        <DialogFooter>
          <Button onClick={onClose}>{_t("Done")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
