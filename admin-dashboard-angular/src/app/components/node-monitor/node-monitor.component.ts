import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TelemetryService, NodeDto, NodeAlert } from '../../services/telemetry.service';

@Component({
  selector: 'app-node-monitor',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './node-monitor.component.html',
  styleUrl: './node-monitor.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NodeMonitorComponent implements OnInit, OnDestroy {
  private telemetryService = inject(TelemetryService);

  readonly connectionState = this.telemetryService.connectionState$;
  readonly totalNodes = this.telemetryService.totalNodes;
  readonly totalUnhealthy = this.telemetryService.totalUnhealthy;
  readonly totalHealthy = this.telemetryService.totalHealthy;
  readonly totalDegraded = this.telemetryService.totalDegraded;
  readonly nodes = this.telemetryService.nodes$;
  readonly alerts = this.telemetryService.alerts$;

  readonly isConnecting = signal(false);
  readonly isConnected = computed(() => this.connectionState() === 'Connected');
  readonly isReconnecting = computed(() => this.connectionState() === 'Reconnecting');
  readonly isDisconnected = computed(() => this.connectionState() === 'Disconnected');

  readonly unhealthyPercentage = computed(() => {
    const total = this.totalNodes();
    const unhealthy = this.totalUnhealthy();
    return total > 0 ? Math.round((unhealthy / total) * 100) : 0;
  });

  readonly recentAlerts = computed(() => this.alerts().slice(0, 5));

  ngOnInit(): void {
    this.connect();
  }

  ngOnDestroy(): void {
    this.telemetryService.stopConnection();
  }

  async connect(): Promise<void> {
    this.isConnecting.set(true);
    try {
      await this.telemetryService.startConnection();
    } catch (error) {
      console.error('Failed to connect:', error);
    } finally {
      this.isConnecting.set(false);
    }
  }

  async refresh(): Promise<void> {
    await this.telemetryService.refreshNodes();
  }

  clearAlerts(): void {
    this.telemetryService.clearAlerts();
  }

  getHealthStatusBadgeClass(healthStatus: string): string {
    switch (healthStatus) {
      case 'Healthy':
        return 'badge-success';
      case 'Unhealthy':
        return 'badge-danger';
      case 'Degraded':
        return 'badge-warning';
      default:
        return 'badge-neutral';
    }
  }

  getHealthStatusIcon(healthStatus: string): string {
    switch (healthStatus) {
      case 'Healthy':
        return '●';
      case 'Unhealthy':
        return '●';
      case 'Degraded':
        return '●';
      default:
        return '○';
    }
  }

  formatTimestamp(timestamp: string | null): string {
    if (!timestamp) return 'Never';
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ${diffMins % 60}m ago`;
    return `${diffDays}d ${diffHours % 24}h ago`;
  }
}
