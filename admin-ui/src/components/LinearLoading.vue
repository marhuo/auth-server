<script setup lang="ts">
defineProps<{ loading: boolean }>()
</script>

<template>
  <div style="height: 4px">
    <v-progress-linear :active="loading" indeterminate color="tertiary" height="4" />
  </div>
</template>
