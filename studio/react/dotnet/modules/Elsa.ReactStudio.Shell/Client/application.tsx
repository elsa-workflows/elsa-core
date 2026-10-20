import { useLocation } from "react-router-dom";
import { LoginPage } from "./login-page";
import { AuthCallback } from "./auth-callback";
import { StudioRouter } from "./studio-router";
export function Application() {
 const {pathname} = useLocation();
 if(pathname === "/login") return <LoginPage />;
 if(pathname === "/auth/callback") return <AuthCallback />;
 return <StudioRouter path={pathname} />;
}
