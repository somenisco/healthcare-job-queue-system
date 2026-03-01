import { Injectable, NgZone, signal } from '@angular/core';
import { TestOrderListItem } from './test-order.model';

type IncomingTestOrder = Partial<TestOrderListItem> & {
  TestOrderId?: string;
  SampleId?: string;
  TestOrderType?: string;
  Status?: string;
  RetryCount?: number;
  MaxRetries?: number;
  CreatedAt?: string;
  UpdatedAt?: string;
};

@Injectable({ providedIn: 'root' })
export class TestOrderStreamService {
  private eventSource: EventSource | null = null;

  private _events = signal<TestOrderListItem[]>([]);
  private _connected = signal(false);

  events = this._events.asReadonly();
  isConnected = this._connected.asReadonly();

  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private reconnectDelay = 1000;
  private maxReconnectDelay = 30000;
  private reconnectTimeout: any;

  constructor(private ngZone: NgZone) {}

  connect(): void {
    if (this.eventSource) {
      return;
    }

    try {
      this.eventSource = new EventSource('/tests/stream');

      this.eventSource.onopen = () => {
        this.ngZone.run(() => {
          this._connected.set(true);
          this.reconnectAttempts = 0;
        });
      };

      this.eventSource.onmessage = (event: MessageEvent) => {
        this.ngZone.run(() => {
          try {
            const testOrder = this.normalizePayload(
              JSON.parse(event.data) as IncomingTestOrder,
            );
            if (!testOrder?.testOrderId) {
              return;
            }
            this._events.update((events) => [
              testOrder,
              ...events.slice(0, 199),
            ]);
          } catch (error) {
            console.error('Error parsing SSE message:', error);
          }
        });
      };

      this.eventSource.onerror = () => {
        this.ngZone.run(() => {
          this._connected.set(false);
        });

        this.eventSource?.close();
        this.eventSource = null;
        this.scheduleReconnect();
      };
    } catch (error) {
      console.error('Error connecting to SSE:', error);
      this.scheduleReconnect();
    }
  }

  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }

    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
    }

    this._connected.set(false);
  }

  private scheduleReconnect(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      return;
    }

    this.reconnectAttempts++;

    const delay = Math.min(
      this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1),
      this.maxReconnectDelay,
    );

    this.reconnectTimeout = setTimeout(() => {
      this.connect();
    }, delay);
  }

  private normalizePayload(payload: IncomingTestOrder): TestOrderListItem {
    return {
      testOrderId: payload.testOrderId ?? payload.TestOrderId ?? '',
      sampleId: payload.sampleId ?? payload.SampleId ?? '',
      testOrderType: payload.testOrderType ?? payload.TestOrderType ?? '',
      status: payload.status ?? payload.Status ?? '',
      retryCount: payload.retryCount ?? payload.RetryCount ?? 0,
      maxRetries: payload.maxRetries ?? payload.MaxRetries ?? 0,
      createdAt: payload.createdAt ?? payload.CreatedAt ?? '',
      updatedAt: payload.updatedAt ?? payload.UpdatedAt ?? '',
    };
  }
}
