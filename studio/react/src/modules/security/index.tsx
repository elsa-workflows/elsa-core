import { ShieldCheck, UserCog, Users } from "lucide-react";
import { defineModule, NavGroups } from "@/lib/modules/types";
import { Resources, Verbs } from "@/lib/permissions";
import { RolesPage } from "./pages/roles-page";
import { UsersPage } from "./pages/users-page";

export const SecurityRoutes = {
  users: "/security/users",
  roles: "/security/roles",
} as const;

export const securityModule = defineModule({
  id: "security",
  name: "Security",
  description: "Users and roles managed by the Elsa Identity module.",
  requiresServerFeatures: ["Elsa.Identity"],
  routes: [
    { path: SecurityRoutes.users, component: UsersPage, title: "Users", permissions: [{ resource: Resources.users, verb: Verbs.view }] },
    { path: SecurityRoutes.roles, component: RolesPage, title: "Roles", permissions: [{ resource: Resources.roles, verb: Verbs.view }] },
  ],
  nav: [
    {
      id: "identity",
      label: "Identity & access",
      icon: ShieldCheck,
      group: NavGroups.administration.id,
      order: 100,
      children: [
        { id: "security-users", label: "Users", href: SecurityRoutes.users, icon: Users, order: 0, permissions: [{ resource: Resources.users, verb: Verbs.view }] },
        { id: "security-roles", label: "Roles", href: SecurityRoutes.roles, icon: UserCog, order: 10, permissions: [{ resource: Resources.roles, verb: Verbs.view }] },
      ],
    },
  ],
});
