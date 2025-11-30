<template>
  <div class="login-container">
    <div class="login-box">
      <div class="header">
        <h2>🏥 CareFlow 病房管理系统</h2>
        <p>请使用工号登录</p>
      </div>
      
      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label>工号</label>
          <input 
            v-model="form.employeeNumber" 
            type="text" 
            placeholder="例如: admin001, doc001" 
            required 
          />
        </div>
        
        <div class="form-group">
          <label>密码</label>
          <input 
            v-model="form.password" 
            type="password" 
            placeholder="默认密码: 123456" 
            required 
          />
        </div>

        <div v-if="errorMsg" class="error-msg">{{ errorMsg }}</div>

        <button type="submit" :disabled="isLoading">
          {{ isLoading ? '登录中...' : '立即登录' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import axios from 'axios';
import { useRouter } from 'vue-router';

const router = useRouter();
const isLoading = ref(false);
const errorMsg = ref('');

const form = ref({
  employeeNumber: '',
  password: ''
});

const handleLogin = async () => {
  isLoading.value = true;
  errorMsg.value = '';

  try {
    // 假设后端运行在 localhost:5000 (根据你的 launchSettings.json 调整端口)
    // 注意：如果你配置了 axios baseURL，这里可以直接写 '/api/auth/login'
    const res = await axios.post('http://localhost:5181/api/auth/login', form.value);
    
    const { token, fullName, role } = res.data;

    // 1. 存储 Token 和用户信息
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify({ fullName, role }));

    // 2. 跳转到主页
    router.push('/home');
  } catch (err) {
    console.error(err);
    errorMsg.value = err.response?.data?.message || '登录失败，请检查网络或账号';
  } finally {
    isLoading.value = false;
  }
};
</script>

<style scoped>
.login-container {
  height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #f0f2f5;
  background-image: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}

.login-box {
  background: white;
  padding: 40px;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
}

.header {
  text-align: center;
  margin-bottom: 30px;
}

.header h2 {
  color: #2c3e50;
  margin-bottom: 10px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  color: #606266;
}

input {
  width: 100%;
  padding: 12px;
  border: 1px solid #dcdfe6;
  border-radius: 6px;
  box-sizing: border-box; /* 关键 */
  transition: border-color 0.3s;
}

input:focus {
  border-color: #409eff;
  outline: none;
}

button {
  width: 100%;
  padding: 12px;
  background-color: #409eff;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 16px;
  transition: background 0.3s;
}

button:hover {
  background-color: #66b1ff;
}

button:disabled {
  background-color: #a0cfff;
  cursor: not-allowed;
}

.error-msg {
  color: #f56c6c;
  margin-bottom: 20px;
  font-size: 14px;
  text-align: center;
}
</style>