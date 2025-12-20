<template>
  <div class="home-container">
    <main class="content">
      <h1>工作台 Dashboard</h1>
      <p class="dept-info">当前科室：{{ currentDeptName }}</p>

      <div class="cards">
        <template v-if="currentUser.role === 'Doctor'">
          <div class="card clickable" @click="router.push('/order-entry')">
            <h3>✍️ 开具新医嘱</h3>
            <p>为所管辖患者下达长期或临时医嘱</p>
            <div class="card-badge doctor">医生权限</div>
          </div>
          <div class="card clickable" @click="router.push('/my-patients')">
            <h3>👥 我的患者</h3>
            <p>查看负责的病床列表及临床概况</p>
          </div>
        </template>

        <template v-else-if="currentUser.role === 'Nurse'">
          <div class="card clickable" @click="router.push('/nurse/dashboard')">
            <h3>🏥 床位概览</h3>
            <p>查看病区床位状态及患者概况</p>
            <div class="card-badge nurse">护士权限</div>
          </div>
          <div class="card clickable" @click="router.push('/nurse/tasks')">
            <h3>📋 我的任务</h3>
            <p>查看今日待执行的护理任务</p>
            <div class="card-badge nurse">护士权限</div>
          </div>
        </template>

        <template v-else-if="currentUser.role === 'Admin'">
          <div class="card clickable" @click="router.push('/staff-management')">
            <h3>⚙️ 人员管理</h3>
            <p>管理医护人员账号、权限及科室分配</p>
            <div class="card-badge admin">管理权限</div>
          </div>
        </template>
      </div>
    </main>
  </div>
</template>

<style scoped>
/* 增加不同角色的视觉区分 */
.card-badge.doctor { background: #409eff; }
.card-badge.nurse { background: #67c23a; }
.card-badge.admin { background: #f56c6c; }

.dept-info { color: #909399; font-size: 0.9rem; margin-bottom: 20px; }
</style>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const currentUser = ref({ fullName: '', role: '', deptCode: '' });

// 科室代码到名称的映射
const deptNameMap = {
  'IM': '内科',
  'SUR': '外科',
  'PED': '儿科',
  'OB': '妇产科',
  'ICU': '重症医学科',
  'ER': '急诊科'
};

// 计算当前科室名称
const currentDeptName = computed(() => {
  if (!currentUser.value.deptCode) {
    return '未分配';
  }
  return deptNameMap[currentUser.value.deptCode] || currentUser.value.deptCode;
});

onMounted(() => {
  // 从 LocalStorage 读取用户信息
  const userStr = localStorage.getItem('userInfo'); // 修正为 userInfo
  const token = localStorage.getItem('token');

  if (!token || !userStr) {
    router.push('/login'); // 无 Token 强制回登录页
    return;
  }

  currentUser.value = JSON.parse(userStr);
});

const logout = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('userInfo'); // 修正为 userInfo
  router.push('/login');
};
</script>

<style scoped>
.navbar {
  display: flex;
  justify-content: space-between;
  padding: 1rem 2rem;
  background-color: #2c3e50;
  color: white;
  align-items: center;
}

.logout-btn {
  margin-left: 15px;
  padding: 5px 15px;
  background-color: #f56c6c;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.content {
  padding: 40px;
}

.cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
  margin-top: 20px;
}

.card {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 12px 0 rgba(0,0,0,0.1);
  border-left: 5px solid #409eff;
  position: relative;
  transition: all 0.3s ease;
}

.card.clickable {
  cursor: pointer;
  border-left-color: #27ae60;
}

.card.clickable:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 25px 0 rgba(0,0,0,0.15);
  border-left-color: #2ecc71;
}

.card-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  background: #27ae60;
  color: white;
  padding: 3px 8px;
  border-radius: 12px;
  font-size: 0.7em;
  font-weight: 600;
}

.card h3 {
  margin: 0 0 10px 0;
  color: #2c3e50;
}

.card p {
  margin: 0;
  color: #7f8c8d;
  line-height: 1.5;
}
</style>