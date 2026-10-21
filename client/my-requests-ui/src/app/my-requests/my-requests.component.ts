import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap, catchError, of, map, startWith } from 'rxjs';
import { ApiError, ApiErrorKind } from '../core/api-error';
import { MyRequest } from './my-request.model';
import { MyRequestsApi } from './my-requests.api';
import { buildActionUrl, REQUEST_ACTION_URL_TEMPLATE } from './request-action-url';

/** One state at a time — impossible to be "loading" and "error" together. */
type ViewState =
  | { readonly status: 'loading' }
  | { readonly status: 'error'; readonly kind: ApiErrorKind }
  | { readonly status: 'loaded'; readonly requests: readonly MyRequest[] };

/**
 * "הבקשות שלי" — standalone, OnPush, Signals for view state, RxJS for the HTTP pipeline.
 * Every state change is a signal write, so OnPush re-renders without any manual detectChanges().
 */
@Component({
  selector: 'app-my-requests',
  templateUrl: './my-requests.component.html',
  styleUrl: './my-requests.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyRequestsComponent {
  private readonly api = inject(MyRequestsApi);
  private readonly actionUrlTemplate = inject(REQUEST_ACTION_URL_TEMPLATE);

  /** Each emission = "load now". switchMap cancels a load still in flight (e.g. double-click on retry). */
  private readonly load$ = new Subject<void>();

  protected readonly state = signal<ViewState>({ status: 'loading' });

  protected readonly requests = computed(() => {
    const state = this.state();
    return state.status === 'loaded' ? state.requests : [];
  });
  protected readonly isEmpty = computed(
    () => this.state().status === 'loaded' && this.requests().length === 0,
  );
  protected readonly actionRequiredCount = computed(
    () => this.requests().filter((r) => r.actionRequired).length,
  );
  protected readonly errorMessage = computed(() => {
    const state = this.state();
    if (state.status !== 'error') return '';
    return state.kind === 'unauthorized'
      ? 'פג תוקף ההתחברות. יש להתחבר מחדש לאזור האישי.'
      : 'לא הצלחנו לטעון את הבקשות שלך כרגע.';
  });
  protected readonly canRetry = computed(() => {
    const state = this.state();
    return state.status === 'error' && state.kind !== 'unauthorized';
  });

  constructor() {
    this.load$
      .pipe(
        switchMap(() =>
          this.api.getMyRequests().pipe(
            map((response): ViewState => ({ status: 'loaded', requests: response.requests })),
            catchError((error: unknown) =>
              of<ViewState>({
                status: 'error',
                kind: error instanceof ApiError ? error.kind : 'unexpected',
              }),
            ),
            startWith<ViewState>({ status: 'loading' }),
          ),
        ),
        takeUntilDestroyed(inject(DestroyRef)), // no manual unsubscribe, no leak
      )
      .subscribe((viewState) => this.state.set(viewState));

    this.load$.next();
  }

  protected retry(): void {
    this.load$.next();
  }

  protected actionUrl(request: MyRequest): string {
    return buildActionUrl(this.actionUrlTemplate, request.requestId);
  }
}
