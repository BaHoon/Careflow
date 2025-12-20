<template>
  <div class="nurse-layout">
    <!-- 固定顶部导航栏 -->
    <header class="layout-header">
      <div class="header-logo">
        <el-icon :size="24" color="#409eff"><Notebook /></el-icon>
        <span class="logo-text">CareFlow | 护士工作台</span>
      </div>
      
      <!-- 导航菜单 -->
      <nav class="header-nav">
        <router-link 
          to="/nurse/dashboard" 
          class="nav-item"
          active-class="active"
        >
          <el-icon><Grid /></el-icon>
          <span>床位概览</span>
        </router-link>
        
        <router-link 
          to="/nurse/tasks" 
          class="nav-item"
          active-class="active"
        >
          <el-icon><List /></el-icon>
          <span>我的任务</span>
        </router-link>
      </nav>
      
      <!-- 用户信息 -->
      <div class="header-user">
        <el-dropdown trigger="click">
          <span class="user-info">
            <el-avatar :size="32" style="background-color: #409eff;">
              {{ userName }}
            </el-avatar>
            <span class="user-name">{{ fullName }}</span>
            <el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item disabled>
                <span style="color: #909399;">{{ roleName }}</span>
              </el-dropdown-item>
              <el-dropdown-item divided @click="handleLogout">
                <el-icon><SwitchButton /></el-icon>
                <span>退出登录</span>
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </header>
    
    <!-- 子页面内容区域 -->
    <main class="layout-content">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { 
  Grid, 
  List, 
  Notebook,
  ArrowDown,
  SwitchButton 
} from '@element-plus/icons-vue'

const router = useRouter()
const userInfo = ref(null)

const userName = computed(() => {
  if (!userInfo.value?.fullName) return 'N'
  return userInfo.value.fullName.charAt(0)
})

const fullName = computed(() => userInfo.value?.fullName || '护士')
const roleName = computed(() => userInfo.value?.role || 'Nurse')

onMounted(() => {
  const stored = localStorage.getItem('userInfo')
  if (stored) {
    try {
      userInfo.value = JSON.parse(stored)
    } catch (error) {
      console.error('解析用户信息失败:', error)
    }
  }
})

const handleLogout = async () => {
  try {
    await ElMessageBox.confirm(
      '确定要退出登录吗？',
      '提示',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    localStorage.removeItem('token')
    localStorage.removeItem('userInfo')
    ElMessage.success('已退出登录')
    router.push('/login')
  } catch (error) {
    // 用户取消
  }
}
</script>

<style scoped>
.nurse-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  background: #f5f7fa;
}

/* ==================== 固定顶部导航栏 ==================== */
.layout-header {
  display: flex;
  align-items: center;
  height: 60px;
  padding: 0 24px;
  background: #ffffff;
  border-bottom: 1px solid #e4e7ed;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  flex-shrink: 0;
  z-index: 1000;
}

.header-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-right: 48px;
}

.logo-text {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
  white-space: nowrap;
}

/* ==================== 导航菜单 ==================== */
.header-nav {
  display: flex;
  gap: 8px;
  flex: 1;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 20px;
  border-radius: 6px;
  color: #606266;
  text-decoration: none;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.3s;
  cursor: pointer;
}

.nav-item:hover {
  background: #f5f7fa;
  color: #409eff;
}

.nav-item.active {
  background: #ecf5ff;
  color: #409eff;
  font-weight: 600;
}

/* ==================== 用户信息 ==================== */
.header-user {
  margin-left: auto;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  padding: 6px 12px;
  border-radius: 6px;
  transition: all 0.3s;
}

.user-info:hover {
  background: #f5f7fa;
}

.user-name {
  font-size: 14px;
  color: #303133;
  font-weight: 500;
}

/* ==================== 内容区域 - 可滚动 ==================== */
.layout-content {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  background: #f5f7fa;
}

/* 滚动条美化 */
.layout-content::-webkit-scrollbar {
  width: 8px;
}

.layout-content::-webkit-scrollbar-track {
  background: #f1f1f1;
}

.layout-content::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 4px;
}

.layout-content::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}

/* ==================== 响应式适配 ==================== */
@media (max-width: 768px) {
  .layout-header {
    padding: 0 16px;
  }
  
  .header-logo {
    margin-right: 20px;
  }
  
  .logo-text {
    font-size: 16px;
  }
  
  .nav-item {
    padding: 6px 12px;
    font-size: 13px;
  }
}
</style>
