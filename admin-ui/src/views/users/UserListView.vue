<script setup lang="ts"></script>
<template>
  <h1>Users</h1>
  <v-container>
    <v-row>
      <v-col :style="{ display: 'flex', flexDirection: 'column', gap: '12px' }">
        <h1>User List</h1>
        <div style="display: flex; flex-direction: row; gap: 10px">
          <v-btn variant="elevated" color="primary">Elevated</v-btn>
          <!-- varjolla, oletus -->
          <v-btn variant="flat" color="primary">Flat</v-btn>
          <!-- täytetty, ei varjoa -->
          <v-btn variant="tonal" color="primary">Tonal</v-btn>
          <!-- vaalea, läpikuultava tausta -->
          <v-btn variant="outlined" color="primary">Outlined</v-btn>
          <!-- reunaviiva -->
          <v-btn variant="text" color="primary">Text</v-btn>
          <div>
            <v-btn variant="text" color="primary" prepend-icon="mdi-logout" size="small"
              >Kirjaudu ulos</v-btn
            >
          </div>
          <!-- pelkkä teksti, hover-tausta -->
          <v-btn variant="plain" color="primary">Plain</v-btn>
          <!-- pelkkä teksti, himmeä -->
        </div>
        <div style="display: flex; flex-direction: row; gap: 10px">
          <v-btn variant="elevated" color="secondary">Elevated</v-btn>
          <!-- varjolla, oletus -->
          <v-btn variant="flat" color="secondary">Flat</v-btn>
          <!-- täytetty, ei varjoa -->
          <v-btn variant="tonal" color="secondary">Tonal</v-btn>
          <!-- vaalea, läpikuultava tausta -->
          <v-btn variant="outlined" color="secondary">Outlined</v-btn>
          <!-- reunaviiva -->
          <v-btn variant="text" color="secondary">Text</v-btn>
          <!-- pelkkä teksti, hover-tausta -->
          <v-btn variant="plain" color="secondary">Plain</v-btn>
          <!-- pelkkä teksti, himmeä -->
        </div>
        <div style="display: flex; flex-direction: row; gap: 10px">
          <v-btn variant="elevated">Elevated</v-btn>
          <!-- varjolla, oletus -->
          <v-btn variant="flat">Flat</v-btn>
          <!-- täytetty, ei varjoa -->
          <v-btn variant="tonal">Tonal</v-btn>
          <!-- vaalea, läpikuultava tausta -->
          <v-btn variant="outlined">Outlined</v-btn>
          <!-- reunaviiva -->
          <v-btn variant="text">Text</v-btn>
          <!-- pelkkä teksti, hover-tausta -->
          <v-btn variant="plain">Plain</v-btn>
          <!-- pelkkä teksti, himmeä -->
        </div>
        <v-card>
          ssss
          <v-btn variant="elevated">Flat</v-btn>
        </v-card>
      </v-col>
    </v-row>
  </v-container>

  <v-container>
    <v-row class="align-center justify-center">
      <v-col cols="auto">
        <v-btn size="x-small">Extra small Button</v-btn>
      </v-col>

      <v-col cols="auto">
        <v-btn size="small">Small Button</v-btn>
      </v-col>

      <v-col cols="auto">
        <v-btn>Regular Button</v-btn>
      </v-col>

      <v-col cols="auto">
        <v-btn size="large">Large Button</v-btn>
      </v-col>

      <v-col cols="auto">
        <v-btn size="x-large">X-Large Button</v-btn>
      </v-col>
    </v-row>
  </v-container>
</template>
