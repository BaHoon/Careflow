import axios from 'axios';
import router from '../router'; // 导入路由以便在 401 时跳转

// 1. 创建 axios 实例
const api = axios.create({
  // 使用相对路径，由 Nginx 代理转发到后端
  baseURL: '/api', 
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
});

// 2. 请求拦截器：在每个请求头中注入 JWT Token
api.interceptors.request.use(
  (config) => {
    // 从 localStorage 获取 token
    const token = localStorage.getItem('token'); 
    if (token) {
      // 按照 AuthService.cs 的要求，使用 Bearer 方案
      config.headers.Authorization = `Bearer ${token}`; 
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// 3. 响应拦截器：统一处理错误
api.interceptors.response.use(
  (response) => {
    // 如果后端返回的是标准数据包，可以在这里直接解包
    return response.data;
  },
  (error) => {
    if (error.response) {
      switch (error.response.status) {
        case 401:
          // Token 过期或无效，清除本地存储并跳转到登录页
          alert('登录已过期，请重新登录');
          localStorage.removeItem('token');
          localStorage.removeItem('userInfo');
          router.push('/login');
          break;
        case 403:
          alert('权限不足，拒绝访问');
          break;
        case 500:
          alert('服务器内部错误，请联系管理员');
          break;
        default:
          alert(`发生错误: ${error.response.data.message || '未知错误'}`);
      }
    } else {
      alert('网络连接超时，请检查后端服务是否启动');
    }
    return Promise.reject(error);
  }
);

export default api;