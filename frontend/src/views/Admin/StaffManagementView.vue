<template>
  <div class="staff-management-view">
    <!-- 固定顶部导航栏 -->
    <header class="layout-header">
      <div class="header-logo">
        <el-icon :size="24" color="#f56c6c"><Setting /></el-icon>
        <span class="logo-text">CareFlow | 管理员工作台</span>
      </div>
      
      <!-- 导航菜单 -->
      <nav class="header-nav">
        <router-link 
          to="/admin/order-history" 
          class="nav-item"
        >
          <el-icon><DocumentCopy /></el-icon>
          <span>医嘱流转记录</span>
        </router-link>
        <router-link 
          to="/staff-management" 
          class="nav-item"
          active-class="active"
        >
          <el-icon><User /></el-icon>
          <span>人员管理</span>
        </router-link>
        <router-link 
          to="/admin/department" 
          class="nav-item"
        >
          <el-icon><OfficeBuilding /></el-icon>
          <span>科室管理</span>
        </router-link>
        <router-link 
          to="/admin/system-log" 
          class="nav-item"
        >
          <el-icon><List /></el-icon>
          <span>系统日志</span>
        </router-link>
      </nav>
      
      <!-- 用户信息 -->
      <div class="header-user">
        <el-dropdown trigger="click">
          <span class="user-info">
            <el-avatar :size="32" style="background-color: #f56c6c;">
              {{ userName }}
            </el-avatar>
            <span class="user-name">{{ fullName }}</span>
            <span class="user-role">(管理员)</span>
            <el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="handleLogout">
                <el-icon><SwitchButton /></el-icon>
                <span>退出登录</span>
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </header>

    <!-- 内容区域 -->
    <div class="page-content">
     
      <!-- 操作栏 -->
      <el-card class="action-card" shadow="never">
        <div class="action-row">
          <!-- 搜索 -->
          <el-input 
            v-model="searchKeyword" 
            placeholder="搜索姓名或员工ID" 
            clearable
            size="default"
            style="width: 300px;"
            @input="handleSearch"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>

          <!-- 角色筛选 -->
          <el-select 
            v-model="roleFilter" 
            placeholder="角色筛选" 
            clearable
            size="default"
            style="width: 150px;"
            @change="handleSearch"
          >
            <el-option label="医生" value="Doctor" />
            <el-option label="护士" value="Nurse" />
          </el-select>

          <!-- 科室筛选 -->
          <el-select 
            v-model="deptFilter" 
            placeholder="科室筛选" 
            clearable
            size="default"
            style="width: 150px;"
            @change="handleSearch"
          >
            <el-option label="IM" value="IM" />
            <el-option label="SUR" value="SUR" />
            <el-option label="PED" value="PED" />
            <el-option label="ADM" value="ADM" />
            <el-option label="CHK" value="CHK" />
          </el-select>

          <div style="flex: 1;"></div>

          <!-- 添加人员按钮 -->
          <el-button type="primary" @click="showAddDialog" size="default">
            <el-icon><Plus /></el-icon>
            <span>添加人员</span>
          </el-button>
        </div>
      </el-card>

      <!-- 统计信息 -->
      <div class="stats-bar" v-if="totalDoctors + totalNurses > 0">
        <span>共 <strong>{{ totalDoctors + totalNurses }}</strong> 名人员</span>
        <span class="stat-item">医生: <strong>{{ totalDoctors }}</strong></span>
        <span class="stat-item">护士: <strong>{{ totalNurses }}</strong></span>
      </div>

      <!-- 人员列表 -->
      <el-card class="table-card" shadow="never">
        <el-table 
          :data="staffList" 
          v-loading="loading"
          stripe
          border
          height="calc(100vh - 350px)"
        >
          <el-table-column prop="staffId" label="员工ID" width="150" />
          <el-table-column prop="fullName" label="姓名" width="120" />
          <el-table-column prop="role" label="角色" width="100">
            <template #default="{ row }">
              <el-tag :type="getRoleColor(row.role)" size="small">
                {{ getRoleName(row.role) }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="deptCode" label="科室" width="150" />
          <el-table-column prop="createdAt" label="创建时间" width="180">
            <template #default="{ row }">
              {{ formatDateTime(row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column label="状态" width="80">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'info'" size="small">
                {{ row.isActive ? '启用' : '禁用' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" min-width="200" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click="showEditDialog(row)">
                编辑
              </el-button>
              <el-button type="warning" link size="small" @click="handleResetPassword(row)">
                重置密码
              </el-button>
              <el-button type="danger" link size="small" @click="handleDelete(row)">
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <!-- 分页 -->
        <div class="pagination-container">
          <el-pagination
            v-model:current-page="currentPage"
            v-model:page-size="pageSize"
            :page-sizes="[20, 50, 100]"
            :total="totalCount"
            layout="total, sizes, prev, pager, next, jumper"
            @size-change="loadStaffList"
            @current-change="loadStaffList"
          />
        </div>
      </el-card>
    </div>

    <!-- 添加/编辑人员弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'add' ? '添加人员' : '编辑人员'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form :model="formData" :rules="formRules" ref="formRef" label-width="100px">
        <el-form-item label="员工ID" prop="staffId">
          <el-input 
            v-model="formData.staffId" 
            placeholder="请输入员工ID"
            :disabled="dialogMode === 'edit'"
          />
        </el-form-item>
        <el-form-item label="姓名" prop="fullName">
          <el-input v-model="formData.fullName" placeholder="请输入姓名" :disabled="dialogMode === 'edit'" />
        </el-form-item>
        <el-form-item label="角色" prop="role">
          <el-select v-model="formData.role" placeholder="请选择角色" style="width: 100%;" :disabled="dialogMode === 'edit'">
            <el-option label="医生" value="Doctor" />
            <el-option label="护士" value="Nurse" />
          </el-select>
        </el-form-item>
        <el-form-item label="科室" prop="deptCode">
          <el-select v-model="formData.deptCode" placeholder="请选择科室" style="width: 100%;">
            <el-option label="IM" value="IM" />
            <el-option label="SUR" value="SUR" />
            <el-option label="PED" value="PED" />
            <el-option label="ADM" value="ADM" />
            <el-option label="CHK" value="CHK" />
          </el-select>
        </el-form-item>
        <el-form-item label="身份证号" prop="idCard" v-if="dialogMode === 'add'">
          <el-input v-model="formData.idCard" placeholder="请输入身份证号" maxlength="18" />
        </el-form-item>
        <el-form-item label="电话号码" prop="phone" v-if="dialogMode === 'add'">
          <el-input v-model="formData.phone" placeholder="请输入电话号码" maxlength="11" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { 
  Setting, 
  DocumentCopy, 
  User, 
  ArrowDown, 
  SwitchButton,
  Search,
  Plus,
  OfficeBuilding,
  List
} from '@element-plus/icons-vue';
import { useRouter } from 'vue-router';
import { queryStaffList, addStaff, resetPassword, updateStaff, deleteStaff } from '@/api/admin';
import { logLogout } from '@/api/systemLog';

const router = useRouter();

const userName = computed(() => {
  const stored = localStorage.getItem('userInfo') || localStorage.getItem('user')
  if (!stored) return '管理'
  try {
    const user = JSON.parse(stored)
    const name = user.fullName || user.name || '管理员'
    return name.substring(0, 2)
  } catch {
    return '管理'
  }
});

const fullName = computed(() => {
  const stored = localStorage.getItem('userInfo') || localStorage.getItem('user')
  if (!stored) return '管理员'
  try {
    const user = JSON.parse(stored)
    return user.fullName || user.name || '管理员'
  } catch {
    return '管理员'
  }
});

// ==================== 数据状态 ====================
const loading = ref(false);
const staffList = ref([]);
const totalCount = ref(0);
const totalDoctors = ref(0);
const totalNurses = ref(0);
const currentPage = ref(1);
const pageSize = ref(20);
const searchKeyword = ref('');
const roleFilter = ref('');
const deptFilter = ref('');

// ==================== 弹窗状态 ====================
const dialogVisible = ref(false);
const dialogMode = ref('add'); // 'add' | 'edit'
const submitting = ref(false);
const formRef = ref(null);

const formData = reactive({
  staffId: '',
  fullName: '',
  role: '',
  deptCode: '',
  idCard: '',
  phone: ''
});

const formRules = {
  staffId: [{ required: true, message: '请输入员工ID', trigger: 'blur' }],
  fullName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }],
  deptCode: [{ required: true, message: '请选择科室', trigger: 'change' }],
  idCard: [
    { required: true, message: '请输入身份证号', trigger: 'blur' },
    { pattern: /^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$/, message: '请输入有效的身份证号', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '请输入电话号码', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入有效的手机号码', trigger: 'blur' }
  ]
};

// ==================== 生命周期 ====================
onMounted(() => {
  loadStaffList();
});

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
    );
    
    // 记录登出日志
    try {
      const user = JSON.parse(localStorage.getItem('userInfo') || '{}')
      await logLogout({
        operatorId: user.id || null,
        operatorName: user.fullName || user.name || '未知用户'
      })
    } catch (logError) {
      console.error('记录登出日志失败:', logError)
    }
    
    localStorage.removeItem('token');
    localStorage.removeItem('userInfo');
    ElMessage.success('已退出登录');
    router.push('/login');
  } catch (error) {
    // 用户取消
  }
};

// ==================== 数据加载 ====================
const loadTotalStats = async () => {
  try {
    // 获取医生总数
    const doctorResponse = await queryStaffList({
      role: 'Doctor',
      pageNumber: 1,
      pageSize: 1
    });
    totalDoctors.value = doctorResponse.totalCount;
    
    // 获取护士总数
    const nurseResponse = await queryStaffList({
      role: 'Nurse',
      pageNumber: 1,
      pageSize: 1
    });
    totalNurses.value = nurseResponse.totalCount;
  } catch (error) {
    console.error('加载统计数据失败:', error);
  }
};

const loadStaffList = async () => {
  loading.value = true;
  try {
    const response = await queryStaffList({
      searchKeyword: searchKeyword.value,
      role: roleFilter.value,
      deptCode: deptFilter.value,
      pageNumber: currentPage.value,
      pageSize: pageSize.value
    });
    
    // 后端返回的字段映射
    staffList.value = response.staffList.map(s => ({
      staffId: s.staffId,
      fullName: s.fullName,
      role: s.role,
      deptCode: s.deptCode,
      wardId: s.wardId || '',
      createdAt: s.createdAt,
      isActive: s.isActive
    }));
    totalCount.value = response.totalCount;
    
    // 获取医生和护士的总数统计（不带筛选条件）
    await loadTotalStats();
    
  } catch (error) {
    console.error('加载人员列表失败:', error);
    ElMessage.error('加载人员列表失败');
  } finally {
    loading.value = false;
  }
};

const handleSearch = () => {
  currentPage.value = 1;
  loadStaffList();
};

// ==================== 弹窗操作 ====================
const showAddDialog = () => {
  dialogMode.value = 'add';
  Object.assign(formData, {
    staffId: '',
    fullName: '',
    role: '',
    deptCode: '',
    idCard: '',
    phone: ''
  });
  dialogVisible.value = true;
};

const showEditDialog = (row) => {
  dialogMode.value = 'edit';
  Object.assign(formData, {
    staffId: row.staffId,
    fullName: row.fullName,
    role: row.role,
    deptCode: row.deptCode,
    wardId: row.wardId || '',
    password: ''
  });
  dialogVisible.value = true;
};

const handleSubmit = async () => {
  if (!formRef.value) return;
  
  await formRef.value.validate(async (valid) => {
    if (!valid) return;
    
    submitting.value = true;
    try {
      if (dialogMode.value === 'add') {
        // 添加新人员
        const response = await addStaff({
          staffId: formData.staffId,
          fullName: formData.fullName,
          role: formData.role,
          deptCode: formData.deptCode,
          idCard: formData.idCard,
          phone: formData.phone
        });
        
        if (response.success) {
          ElMessage.success(response.message || '添加成功，默认密码为 123456');
          dialogVisible.value = false;
          loadStaffList();
        } else {
          ElMessage.error(response.message || '添加失败');
        }
      } else {
        // 编辑人员
        const response = await updateStaff({
          staffId: formData.staffId,
          deptCode: formData.deptCode
        });
        
        if (response.success) {
          ElMessage.success(response.message || '编辑成功');
          dialogVisible.value = false;
          loadStaffList();
        } else {
          ElMessage.error(response.message || '编辑失败');
        }
      }
    } catch (error) {
      console.error('提交失败:', error);
      ElMessage.error(error.response?.data?.message || '操作失败');
    } finally {
      submitting.value = false;
    }
  });
};

// ==================== 其他操作 ====================
const handleResetPassword = async (row) => {
  try {
    const { value: newPassword } = await ElMessageBox.prompt(
      `请输入 ${row.fullName} 的新密码`,
      '重置密码',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        inputType: 'password',
        inputPlaceholder: '请输入新密码',
        inputValidator: (value) => {
          if (!value || value.length < 6) {
            return '密码长度不能少于6位';
          }
          return true;
        }
      }
    );
    
    if (newPassword) {
      const response = await resetPassword({
        staffId: row.staffId,
        newPassword: newPassword
      });
      
      if (response.success) {
        ElMessage.success('密码重置成功');
      } else {
        ElMessage.error(response.message || '密码重置失败');
      }
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('重置密码失败:', error);
      ElMessage.error(error.response?.data?.message || '操作失败');
    }
  }
};

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除 ${row.fullName} 吗？此操作不可恢复！`,
      '删除人员',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'error'
      }
    );
    
    const response = await deleteStaff(row.staffId);
    
    if (response.success) {
      ElMessage.success('删除成功');
      loadStaffList();
    } else {
      ElMessage.error(response.message || '删除失败');
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error);
      ElMessage.error(error.response?.data?.message || '操作失败');
    }
  }
};

// ==================== 格式化方法 ====================
const formatDateTime = (dateString) => {
  if (!dateString) return '-';
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', { 
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'Asia/Shanghai'
  });
};

const getRoleName = (role) => {
  const map = {
    'Doctor': '医生',
    'Nurse': '护士',
    'Admin': '管理员'
  };
  return map[role] || role;
};

const getRoleColor = (role) => {
  const map = {
    'Doctor': 'primary',
    'Nurse': 'success',
    'Admin': 'danger'
  };
  return map[role] || 'info';
};

const getDeptName = (deptCode) => {
  const map = {
    'IM': '内科',
    'SUR': '外科',
    'PED': '儿科',
    'OB': '妇产科',
    'ICU': '重症医学科',
    'ER': '急诊科'
  };
  return map[deptCode] || deptCode;
};
</script>

<style scoped>
.staff-management-view {
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
  color: #f56c6c;
}

.nav-item.active {
  background: #fef0f0;
  color: #f56c6c;
  font-weight: 600;
}

/* ==================== 用户信息 ==================== */
.header-user {
  margin-left: auto;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
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
  font-weight: 500;
  color: #303133;
}

.user-role {
  font-size: 12px;
  color: #909399;
}

/* ==================== 页面内容区域 ==================== */
.page-content {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.page-header {
  margin-bottom: 20px;
}

.page-header h2 {
  margin: 0 0 8px 0;
  color: #303133;
  font-size: 24px;
}

.subtitle {
  margin: 0;
  color: #909399;
  font-size: 14px;
}

.action-card {
  margin-bottom: 20px;
}

.action-row {
  display: flex;
  gap: 15px;
  align-items: center;
}

.stats-bar {
  padding: 12px 20px;
  background: white;
  border-radius: 4px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  display: flex;
  gap: 30px;
}

.stats-bar strong {
  color: #409eff;
  font-size: 18px;
}

.stat-item {
  color: #606266;
}

.stat-item strong {
  font-size: 16px;
  margin-left: 5px;
}

.table-card {
  margin-bottom: 20px;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 0 10px 0;
}
</style>
