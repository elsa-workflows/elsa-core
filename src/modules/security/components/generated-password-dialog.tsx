"use client";

import { KeyRound } from "lucide-react";
import { CopyButton } from "@/components/common/copy-button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";

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
  return (
    <Dialog open={!!password} onOpenChange={(open) => !open && onClose()}>
      <DialogContent showCloseButton={false}>
        <DialogHeader>
          <DialogTitle>Password for {userName}</DialogTitle>
          <DialogDescription>The server generated a password for this user.</DialogDescription>
        </DialogHeader>
        <div className="flex items-center gap-2 rounded-md border bg-muted/40 px-3 py-2 font-mono text-sm break-all">
          <span className="flex-1">{password}</span>
          <CopyButton value={password ?? ""} label="Copy password" />
        </div>
        <Alert>
          <KeyRound />
          <AlertTitle>Shown only once</AlertTitle>
          <AlertDescription>Copy it now. It will not be displayed again; you can set a new one by editing the user.</AlertDescription>
        </Alert>
        <DialogFooter>
          <Button onClick={onClose}>Done</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
