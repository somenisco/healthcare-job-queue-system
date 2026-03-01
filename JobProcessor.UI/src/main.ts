import { provideHttpClient } from '@angular/common/http';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { AllCommunityModule, ModuleRegistry } from 'ag-grid-community';

import { AppComponent } from './app/app.component';
import { provideRouter, Routes } from '@angular/router';

ModuleRegistry.registerModules([AllCommunityModule]);

const routes: Routes = [];

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    provideAnimationsAsync(),
  ],
}).catch((err) => console.error(err));
