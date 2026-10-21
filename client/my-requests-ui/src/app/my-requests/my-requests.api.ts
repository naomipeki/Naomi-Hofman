import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MyRequestsResponse } from './my-request.model';

/** Typed access to the backend. The browser talks ONLY to our service — never to the Forms System or MongoDB. */
@Injectable({ providedIn: 'root' })
export class MyRequestsApi {
  private readonly http = inject(HttpClient);

  /** No identity in the URL: the server reads it from the token. Sorting is done by the server (exam 4.6). */
  getMyRequests(): Observable<MyRequestsResponse> {
    return this.http.get<MyRequestsResponse>('/api/my-requests');
  }
}
