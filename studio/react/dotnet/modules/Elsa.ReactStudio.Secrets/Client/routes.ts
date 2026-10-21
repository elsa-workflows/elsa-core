/** Route patterns owned by this module. Kept apart from `index.tsx` so components can import them without a cycle. */
export const SecretRoutes = {
  list: "/security/secrets",
  detail: "/security/secrets/:name",
} as const;
