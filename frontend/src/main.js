import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import api from './utils/api'
import axios from 'axios'

const app = createApp(App)

// 将自定义api实例设置为全局默认
axios.defaults = { ...axios.defaults, ...api.defaults }
axios.interceptors.request = api.interceptors.request
axios.interceptors.response = api.interceptors.response

// 也可以将api实例挂载到全局属性上
app.config.globalProperties.$api = api

app.use(router)

app.mount('#app')
