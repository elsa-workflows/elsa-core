
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useLocation } from "react-router-dom";
import { LoginPage } from "./login-page";
import { AuthCallback } from "./auth-callback";
import { StudioRouter } from "./studio-router";
export function Application() {
  useTranslation();
 const {pathname} = useLocation();
 if(pathname === "/login") return <LoginPage />;
 if(pathname === "/auth/callback") return <AuthCallback />;
 return <StudioRouter path={pathname} />;
}
