<script setup lang="ts">
import {ref} from "vue";
import {useRoute} from "vue-router";

const route = useRoute();

enum WarnCodes {
  Unavailable = 'unavailable',
  Refused = 'ERR_CONNECTION_REFUSED',
}

interface Alert {
  message: string;
  type?: "error" | "success" | "warning" | "info";
}

let message: string;
switch (route.params.messageCode) {
  case WarnCodes.Unavailable:
  case WarnCodes.Refused:
    message = "Application unavailable";
    break;
  default:
    message = route.params.messageCode as string
    break;
}
const alert = ref<Alert>({
  message: message,
  type: "warning"
});

</script>

<template>
  <v-alert :type="alert.type" icon="mdi-alert" :text="alert.message"></v-alert>
</template>

<style scoped>

</style>
