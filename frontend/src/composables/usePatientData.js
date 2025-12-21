/**
 * ==============================
 * 【患者数据管理 Composable】
 * 提供患者列表加载、选择、多选模式管理等功能
 * 可在多个页面中复用
 * ==============================
 */

import { ref, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { 
  getPatientsWithPendingCount,
  getCurrentWard 
} from '../api/patient';

export function usePatientData() {
  // ==================== 状态管理 ====================
  
  // 患者列表
  const patientList = ref([]);
  
  // 单选模式：当前选中的患者
  const selectedPatient = ref(null);
  
  // 多选模式：选中的患者列表
  const selectedPatients = ref([]);
  
  // 是否启用多选模式
  const enableMultiSelect = ref(false);
  
  // 护士当前排班的病区ID
  const currentScheduledWardId = ref(null);
  
  // 加载状态
  const loading = ref(false);

  // ==================== 当前用户信息 ====================
  
  /**
   * 获取当前登录的护士信息
   */
  const getCurrentNurse = () => {
    try {
      const userInfoStr = localStorage.getItem('userInfo');
      if (userInfoStr) {
        return JSON.parse(userInfoStr);
      }
    } catch (error) {
      console.error('解析用户信息失败:', error);
    }
    return { staffId: 'NUR001', fullName: '未登录', wardId: 'IM-W01' };
  };

  const currentNurse = ref(getCurrentNurse());

  // ==================== 数据加载 ====================
  
  /**
   * 获取护士当前排班病区
   */
  const fetchCurrentScheduledWard = async () => {
    try {
      const nurseId = currentNurse.value.staffId;
      const result = await getCurrentWard(nurseId);
      currentScheduledWardId.value = result.wardId;
      
      if (result.wardId) {
        console.log(`✅ 护士当前排班病区: ${result.wardId}`);
      } else {
        console.log('ℹ️ 护士今日无排班记录，使用默认病区');
        currentScheduledWardId.value = currentNurse.value.wardId;
      }
      
      return result.wardId;
    } catch (error) {
      console.error('获取当前排班病区失败:', error);
      // 失败时使用护士基本信息中的病区
      currentScheduledWardId.value = currentNurse.value.wardId;
      return currentNurse.value.wardId;
    }
  };

  /**
   * 加载患者列表（带未签收统计）
   * @param {string} deptCode - 科室代码，不传则使用当前护士的科室
   */
  const loadPatientList = async (deptCode = null) => {
    try {
      loading.value = true;
      
      const dept = deptCode || currentNurse.value.deptCode;
      if (!dept) {
        ElMessage.error('未找到护士所属科室信息');
        return false;
      }

      const summary = await getPatientsWithPendingCount(dept);
      patientList.value = summary;
      
      ElMessage.success(`加载了 ${summary.length} 个患者`);
      return true;
    } catch (error) {
      console.error('加载患者列表失败:', error);
      ElMessage.error(error.message || '加载患者列表失败');
      return false;
    } finally {
      loading.value = false;
    }
  };

  /**
   * 初始化患者数据（获取排班病区 + 加载患者列表）
   */
  const initializePatientData = async (deptCode = null) => {
    await fetchCurrentScheduledWard();
    await loadPatientList(deptCode);
  };

  // ==================== 患者选择逻辑 ====================
  
  /**
   * 判断患者是否被选中
   */
  const isPatientSelected = (patient) => {
    if (!enableMultiSelect.value) {
      return selectedPatient.value?.patientId === patient.patientId;
    }
    return selectedPatients.value.some(p => p.patientId === patient.patientId);
  };

  /**
   * 单选模式：选择一个患者
   */
  const selectSinglePatient = (patient) => {
    if (selectedPatient.value?.patientId === patient.patientId) {
      return; // 已经选中，不重复选择
    }
    selectedPatient.value = patient;
    selectedPatients.value = [patient];
  };

  /**
   * 多选模式：切换患者选中状态
   */
  const togglePatientSelection = (patient) => {
    const index = selectedPatients.value.findIndex(
      p => p.patientId === patient.patientId
    );
    
    if (index > -1) {
      // 取消选中
      selectedPatients.value.splice(index, 1);
    } else {
      // 选中
      selectedPatients.value.push(patient);
    }
    
    // 更新单选引用
    selectedPatient.value = selectedPatients.value[0] || null;
  };

  /**
   * 通用选择患者方法（自动处理单选/多选）
   */
  const selectPatient = (patient, isMultiSelect = false) => {
    if (isMultiSelect) {
      togglePatientSelection(patient);
    } else {
      selectSinglePatient(patient);
    }
  };

  /**
   * 清空选择
   */
  const clearSelection = () => {
    selectedPatient.value = null;
    selectedPatients.value = [];
  };

  /**
   * 切换多选模式
   */
  const toggleMultiSelectMode = (enabled) => {
    enableMultiSelect.value = enabled;
    
    if (!enabled) {
      // 关闭多选模式，保留第一个选中的患者
      if (selectedPatients.value.length > 0) {
        selectedPatient.value = selectedPatients.value[0];
        selectedPatients.value = [selectedPatients.value[0]];
      }
    } else {
      // 开启多选模式
      if (selectedPatient.value) {
        selectedPatients.value = [selectedPatient.value];
      }
    }
  };

  // ==================== 计算属性 ====================
  
  /**
   * 是否有选中的患者
   */
  const hasSelectedPatient = computed(() => {
    return selectedPatients.value.length > 0;
  });

  /**
   * 选中的患者数量
   */
  const selectedPatientCount = computed(() => {
    return selectedPatients.value.length;
  });

  // ==================== 返回接口 ====================
  
  return {
    // 状态
    patientList,
    selectedPatient,
    selectedPatients,
    enableMultiSelect,
    currentScheduledWardId,
    currentNurse,
    loading,
    
    // 计算属性
    hasSelectedPatient,
    selectedPatientCount,
    
    // 方法
    getCurrentNurse,
    fetchCurrentScheduledWard,
    loadPatientList,
    initializePatientData,
    isPatientSelected,
    selectSinglePatient,
    togglePatientSelection,
    selectPatient,
    clearSelection,
    toggleMultiSelectMode
  };
}
