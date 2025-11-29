namespace CareFlow.Core.Enums;

public enum Gender { Male = 1, Female = 2, Unknown = 0 }
public enum RoomGenderType{ MaleOnly = 1, FemaleOnly = 2, Mixed = 3}
public enum RoomType { ICU = 1, General = 2, VIP = 3, Isolation = 4 }
public enum BedStatus { Available = 1, Occupied = 2, ToBeCleaned = 3, Maintenance = 4 }
public enum CareLevel { Special = 0, Level1 = 1, Level2 = 2, Level3 = 3 }
public enum AdmissionStatus { Admitted = 1, Transferring = 2, Discharged = 3 }
public enum TransferStatus { Pending = 1, Transferring = 2, Completed = 3, Rejected = 4 }