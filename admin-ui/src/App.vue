<script setup lang="ts">
import { RouteNames } from '@/router'
import { ref } from 'vue'
import { useRoute } from 'vue-router'
const route = useRoute()
const sideBarOpen = ref<boolean | null>(null)
</script>

<template>
  <v-app>
    <v-app-bar>
      <v-app-bar-nav-icon color="primary" @click="sideBarOpen = !sideBarOpen" />
      <img src="@/assets/logo-32x32.png" alt="" height="28" class="ml-8" />
      <v-app-bar-title>Auth server</v-app-bar-title>
      <v-spacer />
      <v-btn variant="text" color="primary" prepend-icon="mdi-logout" size="small"
        >Kirjaudu ulos</v-btn
      >
    </v-app-bar>

    <v-navigation-drawer v-model="sideBarOpen" :mobile-breakpoint="800">
      <v-list slim>
        <v-list-subheader>Asiakkuudet</v-list-subheader>
        <v-list-item
          title="Asiakkaat"
          :to="{ name: RouteNames.TenantList }"
          :active="route.path.startsWith('/tenant')"
        >
          <template #prepend>
            <v-icon icon="mdi-domain" />
          </template>
        </v-list-item>

        <v-list-item
          title="Tilausmallit"
          :to="{ name: RouteNames.PlanList }"
          :active="route.path.startsWith('/plan')"
        >
          <template #prepend>
            <v-icon icon="mdi-package-variant-closed" />
          </template>
        </v-list-item>

        <v-list-item
          title="Tilaukset"
          :to="{ name: RouteNames.Subscriptions }"
          :active="route.path.startsWith('/subscription')"
        >
          <template #prepend>
            <v-icon icon="mdi-calendar-sync" />
          </template>
        </v-list-item>

        <v-list-subheader>Valtuutus</v-list-subheader>

        <v-list-item
          title="Sovellukset"
          :to="{ name: RouteNames.ClientList }"
          :active="route.path.startsWith('/client')"
        >
          <template #prepend>
            <v-icon icon="mdi-application-outline" />
          </template>
        </v-list-item>
        <v-list-item
          title="Resurssipalveliment"
          :to="{ name: RouteNames.ResourceServerList }"
          :active="route.path.startsWith('/resource-server')"
        >
          <template #prepend>
            <v-icon icon="mdi-server-security" />
          </template>
        </v-list-item>
        <v-list-item
          title="Scopet"
          :to="{ name: RouteNames.ScopeList }"
          :active="route.path.startsWith('/scope')"
        >
          <template #prepend>
            <v-icon icon="mdi-key-variant" />
          </template>
        </v-list-item>
        <v-list-item
          title="Käyttäjät"
          :to="{ name: RouteNames.UserList }"
          :active="route.path.startsWith('/user')"
        >
          <template #prepend>
            <v-icon icon="mdi-account-multiple" />
          </template>
        </v-list-item>
        <v-list-item
          title="Roolit"
          :to="{ name: RouteNames.RoleList }"
          :active="route.path.startsWith('/role')"
        >
          <template #prepend>
            <v-icon icon="mdi-shield-account" />
          </template>
        </v-list-item>

        <v-list-subheader>Lokit</v-list-subheader>
      </v-list>
    </v-navigation-drawer>

    <v-main>
      <v-container class="page">
        <router-view />
      </v-container>
    </v-main>

    <v-footer app>© 2026</v-footer>
  </v-app>
</template>
