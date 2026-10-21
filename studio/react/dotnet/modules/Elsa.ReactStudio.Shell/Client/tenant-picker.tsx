
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useTenant } from "@elsa-workflows/react-studio-sdk/tenancy";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";

export function TenantPicker() {
  const _t = useTranslation();
  const tenant = useTenant();
  if (tenant.tenantId === undefined && tenant.mode !== "selection") return null;
  return <div className="flex items-center gap-1">
    {tenant.mode === "selection" ? <SimpleSelect aria-label={_t("Switch tenant")} value={tenant.tenantId === undefined ? undefined : JSON.stringify(tenant.tenantId)}
      options={tenant.tenants.map(item => ({ value: JSON.stringify(item.id), label: item.name }))} onChange={value => {
        const selected = tenant.tenants.find(item => JSON.stringify(item.id) === value);
        if (selected) tenant.selectTenant(selected.id);
      }} />
      : <span className="text-xs">{_t("Tenant:")}{" "}{tenant.tenants.find(item => item.id === tenant.tenantId)?.name ?? (tenant.tenantId || _t("Default tenant"))}</span>}
    {tenant.error && <span role="alert">{tenant.error}</span>}
  </div>;
}
