import { Component } from '@angular/core';
import { NodeMonitorComponent } from './components/node-monitor/node-monitor.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NodeMonitorComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'admin-dashboard-angular';
}
