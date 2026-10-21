import { useMemo } from "react";
import { useLocation, useNavigate, useSearchParams as useRouterSearchParams } from "react-router-dom";

/** Stable navigation shared by the shell and modules; BrowserRouter owns History events. */
export function useRouter() {
  const navigate = useNavigate();
  return useMemo(() => ({
    push: (to: string, options?: { scroll?: boolean }) => {
      void navigate(to);
      if (options?.scroll !== false) window.scrollTo(0, 0);
    },
    replace: (to: string, options?: { scroll?: boolean }) => {
      void navigate(to, { replace: true });
      if (options?.scroll !== false) window.scrollTo(0, 0);
    },
  }), [navigate]);
}

export function usePathname() {
  return useLocation().pathname;
}

export function useSearchParams() {
  return useRouterSearchParams()[0];
}
