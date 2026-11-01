import { useEffect, useState } from "react";
import { auth } from "./auth";
import { decodeJwt } from "./jwt";

type Tenant = { id: string; name: string };

export default function TenantSelect() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [currentTenantId, setCurrentTenantId] = useState("");

  useEffect(() => {
    auth.getUser().then((user) => {
      if (!user) return;
      const claims = decodeJwt(user.access_token);
      setCurrentTenantId(claims.tenant_id ?? "");
      setTenants((user.profile.tenants as Tenant[] | undefined) ?? []);
    });
  }, []);

  const switchTenant = (tenantId: string) => {
    if (tenantId && tenantId !== currentTenantId) {
      auth.signinRedirect({ extraQueryParams: { tenant: tenantId } });
    }
  };

  return (
    <label>
      Yritys:{" "}
      <select value={currentTenantId} onChange={(e) => switchTenant(e.target.value)}>
        {tenants.map((t) => (
          <option key={t.id} value={t.id}>
            {t.name}
          </option>
        ))}
      </select>
    </label>
  );
}
