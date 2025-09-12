# Cloudflare Cache Purge Integration

This feature automatically purges Cloudflare cache for the `.well-known/configuration` endpoints when Companies or System Messages are modified.

## Configuration

The Cloudflare integration requires two environment variables to be configured in Azure Key Vault:

### Azure Key Vault Keys

1. **ConnectionStrings--CloudflareApiKey**: Your Cloudflare API token with cache purge permissions
2. **ConnectionStrings--CloudflareZoneId**: The Zone ID of your Cloudflare domain

### Obtaining Cloudflare Credentials

1. **API Key**: 
   - Go to [Cloudflare Dashboard](https://dash.cloudflare.com/profile/api-tokens)
   - Create a new API token with `Zone:Cache Purge` permissions for your domain

2. **Zone ID**:
   - Go to your domain overview in Cloudflare Dashboard
   - Copy the Zone ID from the right sidebar

## Functionality

### Automatic Cache Purge Triggers

Cache is automatically purged when:

#### Company Operations
- Company is created (`POST /api/companies`)
- Company daily limit is updated (`PUT /api/companies/{id}/limit`)
- Company is deleted (`DELETE /api/companies/{id}`)

#### System Message Operations
- System message is created (`POST /api/systemmessages`)
- System message is updated (`PUT /api/systemmessages/{id}`)
- System message is deleted (`DELETE /api/systemmessages/{id}`)

### Purged URLs

The following URLs are purged from Cloudflare cache:
- `https://mimosonk.hu/api/.well-known/configuration`
- `https://www.mimosonk.hu/api/.well-known/configuration`

## Error Handling

- If Cloudflare credentials are not configured, the service logs a warning and continues without purging
- If Cloudflare API returns an error, it's logged but doesn't affect the main operation
- All exceptions are caught and logged to prevent disruption of CRUD operations

## Implementation Details

### Components

1. **ICloudflareService**: Interface defining cache purge operations
2. **CloudflareService**: Implementation that calls Cloudflare's purge cache API
3. **CarWashConfiguration**: Extended to include Cloudflare settings
4. **Controller Integration**: Injected into CompanyController and SystemMessagesController

### Testing

Comprehensive unit tests verify:
- Cloudflare API calls are made with correct parameters
- Cache purge is called after each CRUD operation
- Proper error handling when credentials are missing
- HTTP client behavior and request formation