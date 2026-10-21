import { useTenant } from "@elsa-workflows/react-studio-sdk/tenancy";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";

export function TenantPicker() {
  const tenant = useTenant();
  if (tenant.tenantId === undefined && tenant.mode !== "selection") return null;
  return <div className="flex items-center gap-1">
    {tenant.mode === "selection" ? <SimpleSelect aria-label="Switch tenant" value={tenant.tenantId === undefined ? undefined : JSON.stringify(tenant.tenantId)}
      options={tenant.tenants.map(item => ({ value: JSON.stringify(item.id), label: item.name }))} onChange={value => {
        const selected = tenant.tenants.find(item => JSON.stringify(item.id) === value);
        if (selected) tenant.selectTenant(selected.id);
      }} />
      : <span className="text-xs">Tenant: {tenant.tenants.find(item => item.id === tenant.tenantId)?.name ?? (tenant.tenantId || "Default tenant")}</span>}
    {tenant.error && <span role="alert">{tenant.error}</span>}
  </div>;
}
