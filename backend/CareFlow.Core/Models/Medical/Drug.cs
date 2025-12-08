using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareFlow.Core.Models;

namespace CareFlow.Core.Models.Medical
{
    /// <summary>
    /// 药品表 - 医院药品字典
    /// </summary>
    [Table("Drugs")]
    public class Drug : SoftDeleteEntity<string>
    {
        /// <summary>
        /// 药品编码（主键），例如：DRUG001
        /// </summary>
        [Key]
        [StringLength(50)]
        public new string Id { get; set; } = null!;

        /// <summary>
        /// 药品通用名称
        /// </summary>
        [Required]
        [StringLength(200)]
        public string GenericName { get; set; } = null!;

        /// <summary>
        /// 药品商品名称
        /// </summary>
        [StringLength(200)]
        public string? TradeName { get; set; }

        /// <summary>
        /// 生产厂家
        /// </summary>
        [StringLength(200)]
        public string? Manufacturer { get; set; }

        /// <summary>
        /// 药品规格 (如：100mg/片, 250ml/瓶)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Specification { get; set; } = null!;

        /// <summary>
        /// 药品剂型 (如：片剂、胶囊、注射液)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string DosageForm { get; set; } = null!;

        /// <summary>
        /// 包装规格 (如：10片/盒, 100ml/支)
        /// </summary>
        [StringLength(100)]
        public string? PackageSpec { get; set; }

        /// <summary>
        /// ATC编码 (解剖学治疗学及化学分类系统)
        /// </summary>
        [StringLength(20)]
        public string? AtcCode { get; set; }

        /// <summary>
        /// 药品分类名称
        /// </summary>
        [StringLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// 给药途径 (如：口服、静脉注射、外用)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AdministrationRoute { get; set; } = null!;

        /// <summary>
        /// 药品单价 (元)
        /// </summary>
        [Column(TypeName = "decimal(10,4)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 计价单位 (如：片、支、瓶、盒)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string PriceUnit { get; set; } = null!;

        /// <summary>
        /// 是否为处方药 (true=处方药, false=非处方药)
        /// </summary>
        public bool IsPrescriptionOnly { get; set; }

        /// <summary>
        /// 是否为麻醉药品
        /// </summary>
        public bool IsNarcotic { get; set; }

        /// <summary>
        /// 是否为精神药品
        /// </summary>
        public bool IsPsychotropic { get; set; }

        /// <summary>
        /// 是否为抗菌药物
        /// </summary>
        public bool IsAntibiotic { get; set; }

        /// <summary>
        /// 药品状态 (Active=可用, Inactive=停用, Discontinued=停产)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// 适应症
        /// </summary>
        [StringLength(1000)]
        public string? Indications { get; set; }

        /// <summary>
        /// 禁忌症
        /// </summary>
        [StringLength(1000)]
        public string? Contraindications { get; set; }

        /// <summary>
        /// 用法用量
        /// </summary>
        [StringLength(500)]
        public string? DosageInstructions { get; set; }

        /// <summary>
        /// 不良反应
        /// </summary>
        [StringLength(1000)]
        public string? SideEffects { get; set; }

        /// <summary>
        /// 储存条件
        /// </summary>
        [StringLength(200)]
        public string? StorageConditions { get; set; }

        /// <summary>
        /// 有效期（月）
        /// </summary>
        public int? ShelfLifeMonths { get; set; }

        /// <summary>
        /// 批准文号
        /// </summary>
        [StringLength(50)]
        public string? ApprovalNumber { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }

        // 导航属性
        /// <summary>
        /// 该药品的所有药品医嘱
        /// </summary>
        public ICollection<MedicationOrder> MedicationOrders { get; set; } = new List<MedicationOrder>();
    }
}