import vuetify from '@/plugins/vuetify.ts'
import { createApp } from 'vue'
import 'vuetify/styles'
import App from './App.vue'
import router from './router'
import './styles/index.scss'

const app = createApp(App)
app.use(router)
app.use(vuetify)
app.mount('#app')
