# RegistrationApp — Infrastructure & Setup

## 1. Architecture

```
				  ┌───────────────────────────────┐
 Razorpay ───────▶│  App Service  (.NET 10)       │
 webhooks         │  Razor Pages + reconciliation │
				  │  background worker            │
				  └───────────────┬───────────────┘
								  │ Managed Identity
	  ┌───────────────┬───────────┼───────────┬───────────────┐
	  ▼               ▼           ▼           ▼               ▼
┌───────────┐ ┌─────────────┐ ┌────────┐ ┌──────────┐ ┌─────────────┐
│ Azure SQL │ │    Blob     │ │  Key   │ │   App    │ │  Razorpay   │
│reg-app-db │ │registrations│ │ Vault  │ │ Insights │ │  REST API   │
└───────────┘ └─────────────┘ └────────┘ └──────────┘ └─────────────┘
```

| Service          | Purpose                                            |
|------------------|----------------------------------------------------|
| App Service      | Hosts app + daily reconciliation worker            |
| Azure SQL        | Registrations, payments					        |
| Blob Storage     | Photo / Aadhar uploads (container `registrations`) |
| Key Vault        | All secrets						                |
| App Insights     | Logs, telemetry									|
| Managed Identity | Auth to Vault + Blob, no credentials				|

> Redis is in `.csproj` but unused in `Program.cs` — **do not provision**.

---

## 2. Key Vault secrets

Secret names use `--` where config keys use `:`.

```
Razorpay--KeySecret   ──▶   Razorpay:KeySecret
```

| Secret name							  | Notes								  |
|-----------------------------------------|---------------------------------------|
| `ConnectionStrings--DefaultConnection`  | Azure SQL							  |
| `Razorpay--KeyId`						  | `rzp_live_…`						  |
| `Razorpay--KeySecret`					  |										  |
| `Razorpay--WebhookSecret`				  | Must match Razorpay dashboard exactly |
| `ApplicationInsights--ConnectionString` |										  |
| `DashboardAuth--Username`				  |										  |
| `DashboardAuth--Password`				  |										  |

Stay in `appsettings.Production.json` (not secret):

| Key										| Value					   |
|-------------------------------------------|--------------------------|
| `PaymentReconciliation:Enabled`			| `true`				   |
| `PaymentReconciliation:IntervalMinutes`   | `1440` (daily)		   |
| `PaymentReconciliation:MinimumAgeMinutes` | `15`					   |
| `PaymentReconciliation:LookbackDays`		| `15`					   |
| `PaymentReconciliation:BatchSize`			| `300`					   |
| `AzureStorage:AccountName`				| `registrationappstorage` |
| `AzureStorage:ContainerName`				| `registrations`		   |

> Omit `AzureStorage--ConnectionString` in production — `Program.cs` falls back to
> Managed Identity when only `AccountName` is set.

---

## 3. Provision

```bash
RG=reg-app-rg; LOC=centralus; APP=reg-app-rd; PLAN=reg-app-plan
SQLSRV=reg-app-db-rd; SQLDB=reg-app-db; STORAGE=registrationappstorage
KV=reg-app-kv; AI=reg-app-insights

az group create -n $RG -l $LOC

az sql server create -n $SQLSRV -g $RG -l $LOC -u sqladmin -p '<PASSWORD>'
az sql db create -g $RG -s $SQLSRV -n $SQLDB --service-objective S0
az sql server firewall-rule create -g $RG -s $SQLSRV -n AllowAzure \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

az storage account create -n $STORAGE -g $RG -l $LOC --sku Standard_LRS
az storage container create -n registrations --account-name $STORAGE

az monitor app-insights component create --app $AI -g $RG -l $LOC --application-type web
az keyvault create -n $KV -g $RG -l $LOC --enable-rbac-authorization true

az appservice plan create -n $PLAN -g $RG --sku B1 --is-linux
az webapp create -g $RG -p $PLAN -n $APP --runtime "DOTNETCORE:10.0"
az webapp identity assign -g $RG -n $APP
```

### RBAC

```
App Service Managed Identity
   ├── Key Vault Secrets User          ──▶ Key Vault
   └── Storage Blob Data Contributor   ──▶ Storage Account
```

```bash
PRINCIPAL=$(az webapp identity show -g $RG -n $APP --query principalId -o tsv)
SUB=$(az account show --query id -o tsv)

az role assignment create --assignee $PRINCIPAL --role "Key Vault Secrets User" \
  --scope "/subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.KeyVault/vaults/$KV"

az role assignment create --assignee $PRINCIPAL --role "Storage Blob Data Contributor" \
  --scope "/subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.Storage/storageAccounts/$STORAGE"
```

### Secrets + app settings

```bash
az keyvault secret set --vault-name $KV -n "ConnectionStrings--DefaultConnection" \
  --value "Server=tcp:$SQLSRV.database.windows.net,1433;Initial Catalog=$SQLDB;User ID=sqladmin;Password=<PASSWORD>;Encrypt=True;Connection Timeout=30;"
az keyvault secret set --vault-name $KV -n "Razorpay--KeyId"         --value "<rzp_live_xxx>"
az keyvault secret set --vault-name $KV -n "Razorpay--KeySecret"     --value "<secret>"
az keyvault secret set --vault-name $KV -n "Razorpay--WebhookSecret" --value "<webhook_secret>"
az keyvault secret set --vault-name $KV -n "DashboardAuth--Username" --value "<admin>"
az keyvault secret set --vault-name $KV -n "DashboardAuth--Password" --value "<strong_password>"
az keyvault secret set --vault-name $KV -n "ApplicationInsights--ConnectionString" \
  --value "$(az monitor app-insights component show --app $AI -g $RG --query connectionString -o tsv)"

az webapp config appsettings set -g $RG -n $APP --settings \
  KeyVault__Uri="https://$KV.vault.azure.net/" \
  AzureStorage__AccountName="$STORAGE" \
  AzureStorage__ContainerName="registrations" \
  ASPNETCORE_ENVIRONMENT="Production"
```

---

## 4. Required code change — Key Vault is NOT wired up

Packages are referenced, but `Program.cs` has no `AddAzureKeyVault` call. Secrets
currently resolve from App Service env vars only.

```
appsettings.json  ──▶  App Service env vars  ──▶  Key Vault   (last wins)
												  ▲
												  └── missing today
```

Add after `var builder = WebApplication.CreateBuilder(args);`:

```csharp
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var keyVaultUri = builder.Configuration["KeyVault:Uri"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
	builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}
```

---

## 5. Database

```bash
dotnet ef database update --connection "<azure_sql_connection_string>"
```

---

## 6. Razorpay webhook

| Field | Value |
|---|---|
| URL | `https://<app>.azurewebsites.net/api/webhooks/razorpay` |
| Secret | = `Razorpay--WebhookSecret` |
| Events | `payment.captured`, `payment.failed`, `payment.authorized`, `order.paid` |

Health check: `GET /api/webhooks/razorpay/test`

---

## 7. Reconciliation (daily + dashboard button)

```
Pass 1  local pending rows ──▶ GET /v1/orders/{id}/payments
		catches: missed webhook on the original order

Pass 2  GET /v1/payments ──▶ match via notes.registration_id ──▶ local row
		catches: retry paid on a DIFFERENT order, rows already out of pending
```

Pass 2 acts only when `status == "captured"` **and** amount matches; it updates status
only and never overwrites `RazorpayOrderId`.

---

## 8. Security — rotate now

Live credentials are committed in plaintext:

| File | Line | Credential |
|---|---|---|
| `appsettings.{Development,Production}.json` | 11 | SQL password |
| `appsettings.Development.json` | 17–19 | Razorpay key / secret / webhook secret |
| `appsettings.{Development,Production}.json` | 29–30 | Storage access key |
| `appsettings.{Development,Production}.json` | 32–34 | Dashboard login — **`rd` / `rd`** |

`rd`/`rd` guards the admin dashboard exposing every registrant's name, phone, and Aadhar links.

1. Rotate SQL password, Razorpay keys, storage keys.
2. Set a strong `DashboardAuth` password.
3. Move values to Key Vault; blank them in `appsettings.Production.json`.
4. Local dev: `dotnet user-secrets set "Razorpay:KeySecret" "<value>"`.
5. Purge from git history — committed secrets stay recoverable.

---

## 9. Deploy

```bash
dotnet publish -c Release -o ./publish
cd publish && zip -r ../app.zip . && cd ..
az webapp deploy -g $RG -n $APP --src-path app.zip --type zip
```

**Verify:** `/api/webhooks/razorpay/test` responds → `/dashboard` cards populate →
reconciliation button reports a completed run → App Insights shows
`Payment reconciliation schedule started`.
