import { Injectable, NgZone, signal } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { ActivityLogEvent } from './activity-log.model';

@Injectable({ providedIn: 'root' })
export class ActivityLogService {
  private eventSource: EventSource | null = null;

  private _logs = signal<ActivityLogEvent[]>([]);
  private _connected = signal(false);

  logs = this._logs.asReadonly();
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
      this.eventSource = new EventSource('/activity/stream');

      this.eventSource.onopen = () => {
        this.ngZone.run(() => {
          this._connected.set(true);
          this.reconnectAttempts = 0;
        });
      };

      this.eventSource.onmessage = (event: MessageEvent) => {
        this.ngZone.run(() => {
          try {
            const data = JSON.parse(event.data);

            const newLog: ActivityLogEvent = {
              message: data.message,
              timestamp: new Date(data.timestamp),
            };

            this._logs.update((logs) => [newLog, ...logs.slice(0, 99)]);
          } catch (error) {
            console.error('Failed to parse activity message:', error);
          }
        });
      };

      this.eventSource.onerror = (error: Event) => {
        this.ngZone.run(() => {
          console.error('SSE connection error:', error);
          this._connected.set(false);
        });

        this.eventSource?.close();
        this.eventSource = null;
        this.scheduleReconnect();
      };
    } catch (error) {
      console.error('Failed to establish SSE connection:', error);
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
      console.error('Max reconnect attempts reached');
      return;
    }

    this.reconnectAttempts++;

    const delay = Math.min(
      this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1),
      this.maxReconnectDelay,
    );

    console.log(
      `Reconnecting in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`,
    );

    this.reconnectTimeout = setTimeout(() => {
      this.connect();
    }, delay);
  }
}
