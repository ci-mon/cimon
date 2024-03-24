import { createApp } from 'vue';

import { createVuetify } from 'vuetify';
import 'vuetify/dist/vuetify.css';
import '@mdi/font/css/materialdesignicons.css';
import { components, directives } from 'vuetify/dist/vuetify';
import App from './App.vue';

import { createRouter, createWebHashHistory } from 'vue-router';
import WarnComponent from './components/warn.component.vue';
import SetupComponent from './components/setup.component.vue';

const vuetify = createVuetify({
  components,
  directives,
});

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    {
      path: '/warn/:messageCode',
      name: 'warnPage',
      component: WarnComponent,
    },
    {
      path: '/setup',
      name: 'setup',
      component: SetupComponent,
    },
  ],
});

createApp(App).use(vuetify).use(router).mount('#app');
