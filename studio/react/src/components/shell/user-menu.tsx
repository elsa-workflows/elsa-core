"use client";

import { LogOut, User } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@/lib/environments/active-server";

export function UserMenu() {
  const { user, strategy, logout } = useAuth();
  const server = useActiveServer();
  const name = user?.name ?? "Anonymous";
  const initials = name
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((s) => s[0]?.toUpperCase())
    .join("");

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button variant="ghost" size="icon-sm" aria-label="User menu" />
        }
      >
        <Avatar className="size-7">
          <AvatarFallback className="text-xs">
            {initials || <User className="size-3.5" />}
          </AvatarFallback>
        </Avatar>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuGroup>
          <DropdownMenuLabel className="font-normal">
            <div className="text-sm font-medium truncate">{name}</div>
            <div className="text-xs text-muted-foreground truncate">
              {server.url}
            </div>
            {user?.tenantId && (
              <div className="text-xs text-muted-foreground">
                Tenant: {user.tenantId}
              </div>
            )}
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        {strategy.interactive && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={() => void logout()}>
              <LogOut />
              Sign out
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
