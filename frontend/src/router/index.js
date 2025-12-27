import { createRouter, createWebHistory } from 'vue-router'

// 基础页面
import Login from '../views/Login.vue'
import Home from '../views/Home.vue'

// 布局组件
import NurseLayout from '../layouts/NurseLayout.vue'
import DoctorLayout from '../layouts/DoctorLayout.vue'

// 护士子页面
import NurseDashboard from '../views/NurseDashboard.vue'
import NurseTaskList from '../views/NurseTaskList.vue'
import OrderAcknowledgement from '../views/OrderAcknowledgement.vue'
import NurseOrderQueryView from '../views/NurseOrderQueryView.vue'
import OrderApplication from '../views/OrderApplication.vue'
import NursingRecord from '../views/NursingRecord.vue'
import TaskScan from '../views/TaskScan.vue'
import PatientManagementView from '../views/PatientManagementView.vue'
import NurseScheduleView from '../views/NurseScheduleView.vue'

// 医生子页面
import OrderEntry from '../views/OrderEntry.vue'
import DoctorOrderView from '../views/DoctorOrderView.vue'

// 管理员子页面
import OrderStatusHistoryView from '../views/Admin/OrderStatusHistoryView.vue'
import StaffManagementView from '../views/Admin/StaffManagementView.vue'

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
    
    // ========== 护士工作台（嵌套路由） ==========
    {
      path: '/nurse',
      component: NurseLayout, // 使用布局组件作为父路由
      meta: { requiresAuth: true, role: 'Nurse' },
      redirect: '/nurse/dashboard', // 默认重定向到床位概览
      children: [
        {
          path: 'dashboard', // 相对路径，实际路径为 /nurse/dashboard
          name: 'nurse-dashboard',
          component: NurseDashboard,
          meta: { title: '床位概览' }
        },
        {
          path: 'tasks', // 相对路径，实际路径为 /nurse/tasks
          name: 'nurse-tasks',
          component: NurseTaskList,
          meta: { title: '我的任务' }
        },
        {
          path: 'acknowledgement', // 相对路径，实际路径为 /nurse/acknowledgement
          name: 'order-acknowledgement',
          component: OrderAcknowledgement,
          meta: { title: '医嘱签收' }
        },
        {
          path: 'order-query', // 相对路径，实际路径为 /nurse/order-query
          name: 'nurse-order-query',
          component: NurseOrderQueryView,
          meta: { title: '医嘱查询' }
        },
        {
          path: 'application', // 相对路径，实际路径为 /nurse/application
          name: 'order-application',
          component: OrderApplication,
          meta: { title: '医嘱申请' }
        },
        {
          path: 'nursing-record', // 相对路径，实际路径为 /nurse/nursing-record
          name: 'nursing-record',
          component: NursingRecord,
          meta: { title: '护理记录' }
        },
        {
          path: 'task-scan', // 相对路径，实际路径为 /nurse/task-scan
          name: 'task-scan',
          component: TaskScan,
          meta: { title: '任务扫码执行' }
        },
        {
          path: 'schedule', // 相对路径，实际路径为 /nurse/schedule
          name: 'nurse-schedule',
          component: NurseScheduleView,
          meta: { title: '排班查看' }
        }
      ]
    },
    
    // ========== 医生工作台（嵌套路由） ==========
    {
      path: '/doctor',
      component: DoctorLayout, // 使用布局组件作为父路由
      meta: { requiresAuth: true, role: 'Doctor' },
      redirect: '/doctor/order-entry', // 默认重定向到医嘱开具
      children: [
        {
          path: 'order-entry', // 相对路径，实际路径为 /doctor/order-entry
          name: 'order-entry',
          component: OrderEntry,
          meta: { title: '医嘱开具' }
        },
        {
          path: 'order-query', // 相对路径，实际路径为 /doctor/order-query
          name: 'doctor-order-query',
          component: DoctorOrderView,
          meta: { title: '医嘱查询' }
        },
        {
          path: 'patient-management', // 相对路径，实际路径为 /doctor/patient-management
          name: 'doctor-patient-management',
          component: PatientManagementView,
          props: { userRole: 'Doctor' },
          meta: { title: '患者管理' }
        }
      ]
    },

    // ========== 管理员工作台 ==========
    {
      path: '/admin',
      redirect: '/admin/order-history',
      meta: { requiresAuth: true, role: 'Admin' }
    },
    {
      path: '/admin/order-history',
      name: 'admin-order-history',
      component: OrderStatusHistoryView,
      meta: { requiresAuth: true, role: 'Admin', title: '医嘱状态流转记录' }
    },
    {
      path: '/staff-management',
      name: 'staff-management',
      component: StaffManagementView,
      meta: { requiresAuth: true, role: 'Admin', title: '人员管理' }
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