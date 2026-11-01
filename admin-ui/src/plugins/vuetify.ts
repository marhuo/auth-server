import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'
import 'vuetify/styles'

export default createVuetify({
  theme: {
    defaultTheme: 'system',
    themes: {
      light: {
        dark: false,
        colors: {
          background: '#FAFAFA',
          surface: '#f4f4f4',
          primary: '#181818',
          secondary: '#ececec',
          tertiary: '#C1AA7B',
          'on-tertiary': '#181818',
          error: '#B00020',
          info: '#2196F3',
          success: '#4CAF50',
          warning: '#FB8C00',
        },
      },
      dark: {
        dark: true,
        colors: {
          background: '#181818',
          surface: '#1F1F1F',
          'on-surface': '#f3f3f3',
          primary: '#d3d3d3',
          secondary: '#3a3a3a',
          'on-secondary': '#e6e6e6',
          tertiary: '#C1AA7B',
          'on-tertiary': '#181818',
          error: '#c0324c',
          info: '#64B5F6',
          success: '#81C784',
          warning: '#FFB74D',
        },
      },
    },
  },
  defaults: {
    VBreadcrumbs: {
      class: 'pt-0 mb-4 mt-0',
    },
    VPagination: {
      density: 'comfortable',
    },
    VList: {
      rounded: true,
    },
    VSheet: {
      rounded: true,
    },
  },
})
