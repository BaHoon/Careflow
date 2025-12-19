import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'
import Home from '../views/Home.vue'
import OrderEntry from '../views/OrderEntry.vue'
import Inspection from '../views/Inspection.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/login'
    },
    {
      path: '/login',
      name: 'login',
      component: Login
    },
    {
      path: '/home',
      name: 'home',
      component: Home, 
      meta: { requiresAuth: true }
    },
    {
      path: '/order-entry', // [新增] 医嘱开具路由
      name: 'order-entry',
      component: OrderEntry,
      meta: { requiresAuth: true, role: 'Doctor' } // 限制只有 Doctor 角色进入
    },
    {
      path: '/inspection', // 检查类医嘱管理
      name: 'inspection',
      component: Inspection,
      meta: { requiresAuth: true }
    }
  ]
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  const userInfo = localStorage.getItem('userInfo')
  
  if (to.meta.requiresAuth) {
    if (!token || !userInfo) {
      next('/login')
      return
    }
    
    try {
      const parsedUserInfo = JSON.parse(userInfo)
      if (to.meta.role && parsedUserInfo.role !== to.meta.role) {
        alert(`只有${to.meta.role}可以访问此页面`)
        next('/login')
        return
      }
    } catch (error) {
      console.error('解析用户信息失败:', error)
      next('/login')
      return
    }
  }
  
  next()
})

export default router