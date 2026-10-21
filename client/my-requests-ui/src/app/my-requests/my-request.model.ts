/** Mirrors the API contract of GET /api/my-requests (exam p.5). No ID number exists on the client side at all. */
export interface MyRequest {
  readonly requestId: string;
  readonly serviceName: string;
  readonly authorityName: string;
  readonly status: string;
  readonly actionRequired: boolean;
  readonly isOverdue: boolean;
  /** Already formatted by the server as dd/MM/yyyy (Israel calendar day). */
  readonly submittedAt: string;
  /** Already formatted by the server as dd/MM/yyyy (Israel calendar day). */
  readonly lastUpdate: string;
}

export interface MyRequestsResponse {
  readonly requests: readonly MyRequest[];
}
