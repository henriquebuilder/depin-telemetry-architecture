import { Injectable, signal, computed } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface NodeAlert {
  deviceId: string;
  deviceType: string;
  location: string;
  healthStatus: string;
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  timestamp: string;
}

export interface NodeDto {
  id: string;
  deviceId: string;
  deviceType: string;
  location: string;
  healthStatus: string;
  lastTelemetryReceived: string | null;
  registeredAt: string;
  isActive: boolean;
  consecutiveUnhealthyChecks: number;
}

@Injectable({
  providedIn: 'root'
})
export class TelemetryService {
  private hubConnection?: signalR.HubConnection;
  private readonly hubUrl = 'http://localhost:5000/hubs/nodehealth';

  private readonly alerts = signal<NodeAlert[]>([]);
  private readonly nodes = signal<NodeDto[]>([]);
  private readonly connectionState = signal<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);
  private readonly totalIngested = signal(0);

  readonly alerts$ = this.alerts.asReadonly();
  readonly nodes$ = this.nodes.asReadonly();
  readonly connectionState$ = this.connectionState.asReadonly();
  readonly totalIngested$ = this.totalIngested.asReadonly();

  readonly totalNodes = computed(() => this.nodes().length);
  readonly unhealthyNodes = computed(() => this.nodes().filter(n => n.healthStatus === 'Unhealthy' || n.consecutiveUnhealthyChecks >= 3));
  readonly totalUnhealthy = computed(() => this.unhealthyNodes().length);
  readonly healthyNodes = computed(() => this.nodes().filter(n => n.healthStatus === 'Healthy'));
  readonly totalHealthy = computed(() => this.healthyNodes().length);
  readonly degradedNodes = computed(() => this.nodes().filter(n => n.healthStatus === 'Degraded'));
  readonly totalDegraded = computed(() => this.degradedNodes().length);

  async startConnection(): Promise<void> {
    try {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(this.hubUrl)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.hubConnection.onreconnecting(() => {
        this.connectionState.set(signalR.HubConnectionState.Reconnecting);
      });

      this.hubConnection.onreconnected(() => {
        this.connectionState.set(signalR.HubConnectionState.Connected);
      });

      this.hubConnection.onclose(() => {
        this.connectionState.set(signalR.HubConnectionState.Disconnected);
      });

      this.hubConnection.on('NodeHealthAlert', (alert: NodeAlert) => {
        this.alerts.update(current => [alert, ...current].slice(0, 50));
      });

      this.hubConnection.on('UnhealthyNodeDetected', (alert: NodeAlert) => {
        this.alerts.update(current => [alert, ...current].slice(0, 50));
      });

      await this.hubConnection.start();
      this.connectionState.set(signalR.HubConnectionState.Connected);

      await this.fetchUnhealthyNodes();
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
      throw error;
    }
  }

  async stopConnection(): Promise<void> {
    try {
      if (this.hubConnection) {
        await this.hubConnection.stop();
        this.connectionState.set(signalR.HubConnectionState.Disconnected);
      }
    } catch (error) {
      console.error('Error stopping SignalR connection:', error);
    }
  }

  async joinNodeGroup(deviceId: string): Promise<void> {
    if (this.hubConnection && this.connectionState() === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('JoinNodeGroup', deviceId);
      } catch (error) {
        console.error('Error joining node group:', error);
      }
    }
  }

  async leaveNodeGroup(deviceId: string): Promise<void> {
    if (this.hubConnection && this.connectionState() === signalR.HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('LeaveNodeGroup', deviceId);
      } catch (error) {
        console.error('Error leaving node group:', error);
      }
    }
  }

  private async fetchUnhealthyNodes(): Promise<void> {
    try {
      const response = await fetch('http://localhost:5000/api/nodes/unhealthy');
      if (response.ok) {
        const data: NodeDto[] = await response.json();
        this.nodes.set(data);
      }
    } catch (error) {
      console.error('Error fetching unhealthy nodes:', error);
    }
  }

  async refreshNodes(): Promise<void> {
    await this.fetchUnhealthyNodes();
  }

  clearAlerts(): void {
    this.alerts.set([]);
  }
}
