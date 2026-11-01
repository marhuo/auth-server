<template>
  <h1>Buttons</h1>
  <div style="display: flex; flex-direction: column; gap: 30px">
    <div style="display: flex; flex-direction: row; gap: 10px">
      <v-btn variant="elevated" color="primary">Elevated</v-btn>
      <!-- varjolla, oletus -->
      <v-btn variant="flat" color="primary">Flat</v-btn>
      <v-btn variant="flat" color="info">Flat</v-btn>
      <!-- täytetty, ei varjoa -->
      <v-btn variant="tonal" color="primary">Tonal</v-btn>
      <!-- vaalea, läpikuultava tausta -->
      <v-btn variant="outlined" color="primary">Outlined</v-btn>
      <!-- reunaviiva -->
      <v-btn variant="text" color="primary">Text</v-btn>
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
      <v-btn variant="elevated" color="tertiary">Elevated</v-btn>
      <!-- varjolla, oletus -->
      <v-btn variant="flat" color="tertiary">Flat</v-btn>
      <!-- täytetty, ei varjoa -->
      <v-btn variant="tonal" color="tertiary">Tonal</v-btn>
      <!-- vaalea, läpikuultava tausta -->
      <v-btn variant="outlined" color="tertiary">Outlined</v-btn>
      <!-- reunaviiva -->
      <v-btn variant="text" color="tertiary">Text</v-btn>
      <!-- pelkkä teksti, hover-tausta -->
      <v-btn variant="plain" color="tertiary">Plain</v-btn>
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
    <v-text-field label="Label"></v-text-field>
    <v-card title="Acme Corporation" subtitle="acme-corporation" max-width="400">
      <v-card-text> Tenant on aktiivinen ja sillä on 12 käyttäjää. </v-card-text>
      <v-card-actions>
        <v-btn color="secondary" variant="flat">Peruuta</v-btn>
        <v-btn color="primary" variant="flat">Avaa</v-btn>
      </v-card-actions>
    </v-card>
  </div>
</template>
