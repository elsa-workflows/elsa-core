import { Component, h } from '@stencil/core';

@Component({
  tag: 'app-sidebar',
  styleUrl: 'app-sidebar.scss',
  shadow: false
})
export class AppSidebar {

  render() {
    return (
      <aside class="left-sidebar bg-sidebar">
        <div id="sidebar" class="sidebar sidebar-with-footer">

          <div class="app-brand">
            <stencil-route-link url="/elsa-dashboard">
              <span class="brand-name">Elsa</span>
            </stencil-route-link>
          </div>

          <div class="sidebar-scrollbar">


            <ul class="nav sidebar-inner" id="sidebar-menu">

              <li class="active">
                <stencil-route-link class="sidenav-item-link" url="/elsa-dashboard/workflow-definitions">
                  <i class="fas fa-project-diagram"/>
                  <span class="nav-text">Workflows</span>
                </stencil-route-link>
              </li>
              <li>
                <a class="sidenav-item-link" href="#">
                  <i class="far fa-circle"/>
                  <span class="nav-text">Activities</span>
                </a>
              </li>

            </ul>

          </div>

          <hr class="separator" />

          <div class="sidebar-footer">
            <div class="sidebar-footer-content">
            </div>
          </div>
        </div>
      </aside>
    );
  }
}
