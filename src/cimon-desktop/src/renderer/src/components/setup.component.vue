<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import type {} from '../../../internal-preload/index.d.ts';
import { VForm } from 'vuetify/components';
import { ScreenshotOptions } from '../../../shared/interfaces';
import electron_squirrel_startup from 'electron-squirrel-startup';

const options = await window.electronAPI.getOptions();

const autorun = ref<boolean>(!!options.autoRun);
const screenshotOptions = ref<ScreenshotOptions>(
  options?.screenshots ?? ({ width: 800, height: 600, quality: 80, save: false } as ScreenshotOptions)
);

const baseUrl = ref(options.baseUrl);
const errorText = ref('');
const dialog = ref({
  msg: '',
  show: false,
});
const form = ref<VForm>();

onMounted(() => {
  validate();
});
watch(screenshotOptions, () => {
  validate();
});

function validate(): Promise<{ valid: boolean }> {
  return form.value?.validate() ?? Promise.resolve({ valid: true });
}

async function openFolderDialog() {
  const path = await window.electronAPI.selectFolder(screenshotOptions.value.path);
  if (path) {
    screenshotOptions.value.path = path;
  }
}

function close() {
  window.close();
}

async function connect() {
  if (!(await validate()).valid) {
    return;
  }
  const result = await window.electronAPI.trySetBaseUrl(baseUrl.value);
  dialog.value = {
    msg: result.error ? result.error.message : 'Base url changed',
    show: true,
  };
}

async function save() {
  if (!(await validate()).valid) {
    return;
  }
  const soValue = screenshotOptions.value;
  options.screenshots = {
    width: parseInt(soValue.width.toString()),
    height: parseInt(soValue.height.toString()),
    quality: parseInt(soValue.quality.toString()),
    save: soValue.save,
    path: soValue.path,
  };
  options.autoRun = autorun.value;
  const response = await window.electronAPI.saveOptions(options);
  if (response.error) {
    errorText.value = response.error.message;
    return;
  }
  close();
}

const rules = {
  requiredPath: (value) => screenshotOptions.value.save && (!!value || 'Required.'),
  width: (value) => (value > 200 && value < 4000) || 'width should be > 200 and < 4000',
  height: (value) => (value > 200 && value < 4000) || 'height should be > 200 and < 4000',
  quality: (value) => (value >= 5 && value <= 100) || 'quality should be > 5 and <= 100',
  baseUrl: (value) => {
    if (!URL.canParse(value)){
      return 'enter valid URL';
    }
    if (value.endsWith('/')) {
      return 'do not end value with "/"';
    }
    return true;
  },
};
</script>

<template>
  <v-app>
    <v-main class="ma-4">
      <v-form ref="form">
        <v-card class="pa-2">
          <div class="d-flex">
            <v-text-field
              v-model="baseUrl"
              :rules="[rules.baseUrl]"
              clearable
              label="cimon server url"
              prepend-icon="mdi-server"
            ></v-text-field>
            <v-btn class="ml-4" append-icon="mdi-connection" @click="connect">Connect</v-btn>
          </div>
          <v-switch v-model="autorun" label="Start on login"></v-switch>
          <v-switch v-model="screenshotOptions.save" label="Save screenshots on monitor change"></v-switch>
          <div v-if="screenshotOptions.save">
            <div class="d-flex">
              <v-text-field
                v-model="screenshotOptions.width"
                type="number"
                class="inline-item mr-8"
                :rules="[rules.width]"
                label="Image width"
              ></v-text-field>
              <v-text-field
                v-model="screenshotOptions.height"
                type="number"
                class="inline-item mr-8"
                :rules="[rules.height]"
                label="Image height"
              ></v-text-field>
              <v-text-field
                v-model="screenshotOptions.quality"
                type="number"
                class="inline-item mr-8"
                :rules="[rules.quality]"
                label="Image quality"
              ></v-text-field>
            </div>
            <div class="d-flex">
              <v-text-field
                v-model="screenshotOptions.path"
                :rules="[rules.requiredPath]"
                readonly
                label="Screenshots location"
                prepend-icon="mdi-image"
              ></v-text-field>
              <v-btn class="ml-4" icon="mdi-folder-file-outline" @click="openFolderDialog"></v-btn>
            </div>
          </div>
          <div class="d-flex flex-row-reverse">
            <v-btn class="ml-4" append-icon="mdi-content-save" @click="save">Save</v-btn>
            <v-btn append-icon="mdi-cancel" @click="close">Close</v-btn>
          </div>
        </v-card>
      </v-form>
      <v-snackbar v-if="Boolean(errorText?.length)" multi-line>
        {{ errorText }}
        <template #actions>
          <v-btn color="red" variant="text" @click="errorText = ''"> Close</v-btn>
        </template>
      </v-snackbar>
    </v-main>
  </v-app>
  <v-dialog v-model="dialog.show" max-width="500">
    <v-card title="Info">
      <v-card-text>
        {{ dialog.msg }}
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn text="Close" @click="dialog.show = false"></v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.inline-item {
  max-width: 200px;
}
</style>
