namespace CareFlow.Core.Enums;

public enum DrugCategory { Oral = 1, Injection = 2, External = 3, Material = 4, Exam = 5 }
public enum OrderType { LongTerm = 1, Temporary = 2 } // 长期/临时
public enum OrderStatus { Draft = 0, PendingReview = 1, Accepted = 2, Stopped = 3, Cancelled = 4 }
public enum TaskType { Dispensing = 1, Administration = 2, Patrol = 3 } // 配药/给药/巡视
public enum ExecutionTaskStatus { Pending = 1, Dispensed = 2, InProgress = 3, Completed = 4, Cancelled = 5 }