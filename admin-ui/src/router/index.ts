import { createRouter, createWebHistory } from 'vue-router'

export const RouteNames = {
  ClientList: 'clients',
  PlanList: 'plans',
  ResourceServerList: 'resource-servers',
  ResourceServerCreate: 'resource-server-create',
  ResourceServerDetails: 'resource-server-details',
  ResourceServerEdit: 'resource-server-edit',
  RoleList: 'roles',
  RoleCreate: 'role-create',
  RoleDetails: 'role-details',
  RoleEdit: 'role-edit',
  ScopeList: 'scopes',
  ScopeCreate: 'scope-create',
  ScopeDetails: 'scope-details',
  ScopeEdit: 'scope-edit',
  Subscriptions: 'subscriptions',
  TenantList: 'tenants',
  TenantCreate: 'tenant-create',
  TenantDetails: 'tenant-detail',
  TenantEdit: 'tenant-edit',
  UserList: 'users',
  Buttons: 'buttons',
} as const

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: { name: RouteNames.TenantList },
    },
    {
      path: '/clients',
      name: RouteNames.ClientList,
      component: () => import('@/views/clients/ClientListView.vue'),
    },
    {
      path: '/plans',
      name: RouteNames.PlanList,
      component: () => import('@/views/plans/PlanListView.vue'),
    },
    {
      path: '/resource-servers',
      name: RouteNames.ResourceServerList,
      component: () => import('@/views/resource-servers/ResourceServerListView.vue'),
    },
    {
      path: '/resource-servers/create',
      name: RouteNames.ResourceServerCreate,
      component: () => import('@/views/resource-servers/ResourceServerCreateView.vue'),
    },
    {
      path: '/resource-servers/:id',
      name: RouteNames.ResourceServerDetails,
      component: () => import('@/views/resource-servers/ResourceServerDetailsView.vue'),
      props: true,
    },
    {
      path: '/resource-servers/:id/edit',
      name: RouteNames.ResourceServerEdit,
      component: () => import('@/views/resource-servers/ResourceServerEditView.vue'),
      props: true,
    },
    {
      path: '/roles',
      name: RouteNames.RoleList,
      component: () => import('@/views/roles/RoleListView.vue'),
    },
    {
      path: '/roles/create/:resourceServerId?',
      name: RouteNames.RoleCreate,
      component: () => import('@/views/roles/RoleCreateView.vue'),
      props: true,
    },
    {
      path: '/roles/:id',
      name: RouteNames.RoleDetails,
      component: () => import('@/views/roles/RoleDetailsView.vue'),
      props: true,
    },
    {
      path: '/roles/:id/edit',
      name: RouteNames.RoleEdit,
      component: () => import('@/views/roles/RoleEditView.vue'),
      props: true,
    },
    {
      path: '/scopes',
      name: RouteNames.ScopeList,
      component: () => import('@/views/scopes/ScopeListView.vue'),
    },
    {
      path: '/scopes/create/:resourceServerId?',
      name: RouteNames.ScopeCreate,
      component: () => import('@/views/scopes/ScopeCreateView.vue'),
      props: true,
    },
    {
      path: '/scopes/:id',
      name: RouteNames.ScopeDetails,
      component: () => import('@/views/scopes/ScopeDetailsView.vue'),
      props: true,
    },
    {
      path: '/scopes/:id/edit',
      name: RouteNames.ScopeEdit,
      component: () => import('@/views/scopes/ScopeEditView.vue'),
      props: true,
    },
    {
      path: '/subscriptions',
      name: RouteNames.Subscriptions,
      component: () => import('@/views/subscriptions/SubscriptionListView.vue'),
    },
    {
      path: '/tenants',
      name: RouteNames.TenantList,
      component: () => import('@/views/tenants/TenantListView.vue'),
    },
    {
      path: '/tenants/create',
      name: RouteNames.TenantCreate,
      component: () => import('@/views/tenants/TenantCreateView.vue'),
      props: true,
    },
    {
      path: '/tenant/:id',
      name: RouteNames.TenantDetails,
      component: () => import('@/views/tenants/TenantDetailsView.vue'),
      props: true,
    },
    {
      path: '/tenant/:id/edit',
      name: RouteNames.TenantEdit,
      component: () => import('@/views/tenants/TenantEditView.vue'),
      props: true,
    },
    {
      path: '/users',
      name: RouteNames.UserList,
      component: () => import('@/views/users/UserListView.vue'),
    },
    {
      path: '/mui/buttons',
      name: RouteNames.Buttons,
      component: () => import('@/views/mui/ButtonsView.vue'),
    },
  ],
})

export default router
