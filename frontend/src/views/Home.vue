<template>
  <div class="home-container">
    <nav class="navbar">
      <div class="logo">CareFlow 系统</div>
      <div class="user-info">
        <span>欢迎, {{ currentUser.fullName }} ({{ currentUser.role }})</span>
        <button @click="logout" class="logout-btn">退出</button>
      </div>
    </nav>

    <main class="content">
      <h1>工作台 Dashboard</h1>
      <div class="cards">
        <div class="card">
          <h3>我的患者</h3>
          <p>查看当前负责的患者列表</p>
        </div>
        <div class="card">
          <h3>待办医嘱</h3>
          <p>今日待处理的医嘱任务</p>
        </div>
        <div class="card clickable" v-if="currentUser.role === 'Nurse'" @click="goToBarcodeMatching">
          <h3>🔍 条形码匹配验证</h3>
          <p>扫描条形码验证执行任务与患者匹配</p>
          <div class="card-badge">护士专用</div>
        </div>
        <div class="card" v-if="currentUser.role === 'Admin'">
          <h3>人员管理</h3>
          <p>导入与管理医护账号</p>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const currentUser = ref({ fullName: '', role: '' });

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

function goToBarcodeMatching() {
  router.push('/barcode-matching');
}

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