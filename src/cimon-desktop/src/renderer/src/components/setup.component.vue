<script setup lang="ts">
import { ref } from 'vue';

const showScreenshots = ref(false);
const screenshotLocation = ref('');
const openFolderDialog = () => {
  ipcRenderer.invoke('open-folder-dialog').then(result => {
    if (!result.canceled && result.filePaths.length > 0) {
      screenshotLocation.value = result.filePaths[0];
    }
  });
};
</script>

<template>
  <v-app>
    <v-main class="ma-4">
      <v-card class="pa-2">
        <div class="d-flex">
          <v-text-field clearable label="cimon server url" prepend-icon="mdi-server"></v-text-field>
          <v-btn class="ml-4" append-icon="mdi-connection">Connect</v-btn>
        </div>
        <v-switch label="Start on login"></v-switch>
        <v-switch v-model="showScreenshots" label="Save screenshots on monitor change"></v-switch>
        <div v-if="showScreenshots" class="d-flex">
          <v-text-field label="screenshots location" prepend-icon="mdi-image"></v-text-field>
          <v-btn class="ml-4" icon="mdi-folder-file-outline"></v-btn>
        </div>
        <div class="d-flex flex-row-reverse">
          <v-btn class="ml-4" append-icon="mdi-content-save">Save</v-btn>
          <v-btn append-icon="mdi-cancel">Cancel</v-btn>
        </div>

      </v-card>
    </v-main>
  </v-app>
</template>

<style scoped></style>
