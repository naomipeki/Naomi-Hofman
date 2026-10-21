import { InjectionToken } from '@angular/core';

/**
 * URL template for the "complete the action" button. The exam does not define the destination,
 * so it is configuration (assumption to confirm with the Personal Area / Forms teams).
 * "{requestId}" is replaced with the URL-encoded request id.
 */
export const REQUEST_ACTION_URL_TEMPLATE = new InjectionToken<string>('REQUEST_ACTION_URL_TEMPLATE', {
  providedIn: 'root',
  factory: () => '/requests/{requestId}',
});

export function buildActionUrl(template: string, requestId: string): string {
  return template.replace('{requestId}', encodeURIComponent(requestId));
}
