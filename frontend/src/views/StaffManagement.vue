<template>
  <div class="staff-container">
    <header class="page-header">
      <h2>🧑‍⚕️ 人员账号管理</h2>
      <button class="back-btn" @click="$router.push('/home')">返回工作台</button>
    </header>

    <div v-if="loading" class="loading">正在获取全院人员名单...</div>
    
    <div v-if="errorMsg" class="error">{{ errorMsg }}</div>

    <div v-else class="accordion-list">
      <div 
        v-for="(staffList, deptName) in groupedStaff" 
        :key="deptName" 
        class="dept-group"
      >
        <div class="dept-header" @click="toggleDept(deptName)">
          <div class="title-left">
            <span class="arrow" :class="{ rotated: !collapsed[deptName] }">▶</span>
            <span class="dept-name">{{ deptName }}</span>
            <span class="count-badge">{{ staffList.length }}人</span>
          </div>
        </div>

        <div class="dept-body" v-show="!collapsed[deptName]">
          <table>
            <thead>
              <tr>
                <th>工号</th>
                <th>姓名</th>
                <th>身份</th>
                <th>职称/职级</th>
                <th>状态</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="staff in staffList" :key="staff.id">
                <td>{{ staff.employeeNumber }}</td>
                <td>{{ staff.fullName }}</td>
                <td>
                    <span :class="['role-tag', getRoleColor(staff.role)]">
                        {{ getRoleName(staff.role) }}
                    </span>
                </td>
                <td>{{ staff.titleOrRank }}</td>
                <td>
                  <span :class="['status-dot', staff.isActive ? 'active' : 'inactive']"></span>
                  {{ staff.isActive ? '在职' : '已禁用' }}
                </td>
                <td>
                  <button class="action-btn">编辑</button>
                  <button class="action-btn delete">删除</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import axios from 'axios';

// 状态定义
const staffData = ref([]); // 后端返回的原始扁平数组
const loading = ref(true);
const errorMsg = ref('');
const collapsed = ref({}); // 记录每个科室是否折叠 { '心内科': false, '急诊科': true }

// 1. 获取数据
const fetchStaff = async () => {
  try {
    // 确保端口是 5181
    const res = await axios.get('http://localhost:5181/api/AdminStaff');
    staffData.value = res.data;
    
    // 初始化折叠状态：默认全部展开 (false 表示不折叠)
    const depts = [...new Set(res.data.map(s => s.deptName))];
    depts.forEach(d => collapsed.value[d] = false);

  } catch (err) {
    console.error(err);
    errorMsg.value = "无法加载人员列表，请检查后端是否启动";
  } finally {
    loading.value = false;
  }
};

// 获取身份中文名
const getRoleName = (role) => {
  switch (role) {
    case 'Doctor': return '医生';
    case 'Nurse': return '护士';
    case 'Admin': return '管理员';
    default: return '未知';
  }
};

// 获取标签颜色样式
const getRoleColor = (role) => {
  switch (role) {
    case 'Doctor': return 'blue';  // 蓝色
    case 'Nurse': return 'green';  // 绿色
    case 'Admin': return 'purple'; // 紫色 (区分管理员)
    default: return 'gray';
  }
};

// 2. 核心逻辑：按科室分组
// 后端给的是 [ {张三, 心内科}, {李四, 急诊科}, {王五, 心内科} ]
// 我们要转成 { '心内科': [张三, 王五], '急诊科': [李四] }
const groupedStaff = computed(() => {
  return staffData.value.reduce((groups, item) => {
    const dept = item.deptName || '未分配科室';
    if (!groups[dept]) {
      groups[dept] = [];
    }
    groups[dept].push(item);
    return groups;
  }, {});
});

// 3. 切换折叠状态
const toggleDept = (deptName) => {
  collapsed.value[deptName] = !collapsed.value[deptName];
};

// 页面加载时执行
onMounted(() => {
  fetchStaff();
});
</script>

<style scoped>
/* 容器样式 */
.staff-container {
  padding: 30px;
  max-width: 1000px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.back-btn {
  padding: 8px 16px;
  background: white;
  border: 1px solid #ddd;
  border-radius: 4px;
  cursor: pointer;
}

/* 折叠组样式 */
.dept-group {
  border: 1px solid #e0e0e0;
  margin-bottom: 15px;
  border-radius: 8px;
  overflow: hidden;
  background: white;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
}

.dept-header {
  background-color: #f8f9fa;
  padding: 15px 20px;
  cursor: pointer;
  display: flex;
  justify-content: space-between;
  align-items: center;
  transition: background 0.2s;
}

.dept-header:hover {
  background-color: #f1f3f5;
}

.title-left {
  display: flex;
  align-items: center;
  gap: 10px;
  font-weight: bold;
  color: #333;
}

.arrow {
  font-size: 12px;
  transition: transform 0.3s;
  color: #999;
}
.arrow.rotated {
  transform: rotate(90deg); /* 展开时箭头向下 */
}

.count-badge {
  background-color: #e6f7ff;
  color: #1890ff;
  padding: 2px 8px;
  border-radius: 10px;
  font-size: 12px;
}

/* 表格样式 */
.dept-body {
  padding: 0;
  border-top: 1px solid #e0e0e0;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  padding: 12px 20px;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

th {
  background-color: #fff;
  color: #666;
  font-weight: 500;
  font-size: 14px;
}

/* 身份标签 */
.role-tag {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: bold;
}
.role-tag.blue { background: #e6f7ff; color: #1890ff; }
.role-tag.green { background: #f6ffed; color: #52c41a; }
.role-tag.purple { background: #f9f0ff; color: #722ed1; }
.role-tag.gray { background: #f5f5f5; color: #999; }

/* 状态点 */
.status-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  margin-right: 5px;
  margin-bottom: 2px;
}
.active { background-color: #52c41a; }
.inactive { background-color: #ccc; }

/* 操作按钮 */
.action-btn {
  margin-right: 8px;
  padding: 4px 8px;
  border: none;
  background: none;
  color: #1890ff;
  cursor: pointer;
}
.action-btn.delete { color: #ff4d4f; }
.action-btn:hover { text-decoration: underline; }
</style>