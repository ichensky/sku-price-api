# Frontend and BFF Design

This document describes the frontend and middleware portion. It is a design proposal; no frontend or BFF code is included.

## Request Flow

```text
Browser UI  ->  BFF route  ->  GET /products/{sku}/price  ->  SKU Price API
	 ^               |                                         |
	 |               +---------- forwards response ------------+
	 +--------------------- renders result or error ------------+
```

The UI submits a SKU to a same-origin BFF route such as `GET /api/products/{sku}/price`. The BFF URL-encodes the SKU and calls the configured API base URL at `GET /products/{sku}/price`. The API base URL remains server-side configuration; the browser does not call or need to know about the ERP service.

On success, the BFF returns the API response without changing its meaning:

```json
{
	"price": 19.99,
	"currency": "USD",
	"lastUpdated": "2026-10-21T12:00:00Z"
}
```

The UI formats the price using the returned currency and displays the update time in the user's local time zone. It should not assume a currency or timestamp format before receiving the response.

## UI Behavior

- **Idle:** Show a labeled SKU input and a lookup action.
- **Invalid input:** Trim the value and provide immediate feedback if it is empty. The API remains authoritative for SKU format rules; client-side validation is for usability only.
- **Loading:** Disable duplicate submissions, expose a visible progress state, and announce the status through an `aria-live` region.
- **Success:** Show the price, currency, and last-updated time. A new lookup replaces the previous result.
- **Not found:** Explain that no price was found for the submitted SKU and allow another lookup.
- **Failure:** Show a concise, recoverable message and a retry action. Keep technical details out of the UI.

Use a real `<form>` so Enter submits the SKU. Associate a visible label with the input, move focus to an error summary when appropriate, and do not communicate errors through color alone.

## BFF Responsibilities

- Accept the browser request and call the SKU Price API using a server-configured base URL.
- Pass through the requested SKU as a path segment using correct URL encoding.
- Preserve the HTTP status and Problem Details format, but replace or sanitize `detail` before returning it to the browser. Never expose upstream credentials or internal exception details.
- Apply a bounded outbound timeout and cancel the API request when the browser disconnects.
- Avoid automatic retries for invalid input or not-found responses. Any retry policy for transient failures should be bounded and consistent with the API's resilience policy.

The BFF keeps browser/API communication same-origin, centralizes the API URL, and provides a place for authentication or request policies if the application needs them later. For a small public demo, the UI could call the API directly instead, provided CORS is configured; the BFF is the preferred option when serving a separate frontend.

## Error Mapping

The API returns Problem Details with `status`, `title`, and `detail`. The BFF should preserve the status and response content type, and sanitize the detail as described above. The UI should map status codes to user-facing messages rather than displaying raw exception text.

| API status | Meaning | Suggested UI behavior |
| --- | --- | --- |
| `400` | Invalid SKU or another domain validation error | Identify the SKU as invalid and let the user correct it. |
| `404` | No price exists for the SKU | Show a not-found state; do not retry automatically. |
| `502` | Upstream ERP request or response failed | Explain that price lookup is temporarily unavailable and offer retry. |
| `503` | ERP circuit is open/unavailable | Show a temporary availability message and allow retry. |
| `504` | ERP request timed out | Explain the lookup timed out and allow retry. |
| `500` | Unexpected API error | Show a generic error and offer retry. |

If the browser request itself is cancelled, stop the loading indicator without presenting it as an ERP failure. If the BFF cannot reach the API, it should return a generic gateway/service error in the same Problem Details format.

## Acceptance Checks

- A known SKU displays its price, currency, and update time.
- An unknown SKU shows a not-found state.
- An invalid SKU shows a validation message.
- An ERP timeout or failure shows a recoverable error, not a blank result or raw exception.
- Loading, success, and error states remain usable with keyboard navigation and assistive technology.
