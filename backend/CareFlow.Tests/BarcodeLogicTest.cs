using CareFlow.Core.Models;
using Xunit;

namespace CareFlow.Tests
{
    public class BarcodeLogicTest
    {
        [Fact]
        public void TestBarcodeGenerationAndParsingMatch()
        {
            // Arrange
            var originalIndex = new BarcodeIndex
            {
                TableName = "MedicalOrders",
                RecordId = "1001"
            };

            // Act: 生成条形码文本
            string barcodeText = originalIndex.ToString();
            
            // Act: 解析条形码文本
            var parsedIndex = BarcodeIndex.Parse(barcodeText);

            // Assert: 验证往返转换是否一致
            Assert.Equal(originalIndex.TableName, parsedIndex.TableName);
            Assert.Equal(originalIndex.RecordId, parsedIndex.RecordId);
            Assert.Equal("MedicalOrders-1001", barcodeText);
        }

        [Theory]
        [InlineData("Doctors", "D001", "Doctors-D001")]
        [InlineData("MedicationOrders", "4", "MedicationOrders-4")]
        [InlineData("Patients", "P001", "Patients-P001")]
        [InlineData("Wards", "W001", "Wards-W001")]
        public void TestBarcode2_0Formats(string tableName, string recordId, string expectedFormat)
        {
            // Arrange
            var index = new BarcodeIndex
            {
                TableName = tableName,
                RecordId = recordId
            };

            // Act
            string generated = index.ToString();
            var parsed = BarcodeIndex.Parse(generated);

            // Assert: 验证条形码2.0格式
            Assert.Equal(expectedFormat, generated);
            Assert.Equal(tableName, parsed.TableName);
            Assert.Equal(recordId, parsed.RecordId);
        }

        [Fact]
        public void TestParseBarcode1_0Format_StillWorks()
        {
            // Arrange: 条形码1.0格式测试
            string barcode1_0 = "Doctor-D001";

            // Act
            var parsed = BarcodeIndex.Parse(barcode1_0);

            // Assert: 仍然能解析旧格式
            Assert.Equal("Doctor", parsed.TableName);
            Assert.Equal("D001", parsed.RecordId);
        }

        [Fact]
        public void TestParseBarcode2_0Format_WithComplexIds()
        {
            // Arrange: 测试包含多个连字符的记录ID
            string barcode2_0 = "MedicationOrders-MO-2024-001";

            // Act
            var parsed = BarcodeIndex.Parse(barcode2_0);

            // Assert: 只用第一个连字符分割
            Assert.Equal("MedicationOrders", parsed.TableName);
            Assert.Equal("MO-2024-001", parsed.RecordId);
        }

        [Fact]
        public void TestParseInvalidFormat_ThrowsDetailedException()
        {
            // Arrange
            string invalidFormat = "InvalidFormatWithoutDash";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => BarcodeIndex.Parse(invalidFormat));
            Assert.Contains("无效的条形码格式", ex.Message);
            Assert.Contains("TableName-RecordId", ex.Message);
        }

        [Fact]
        public void TestParseEmptyString_ThrowsException()
        {
            // Arrange
            string emptyFormat = "";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => BarcodeIndex.Parse(emptyFormat));
            Assert.Contains("条形码文本不能为空", ex.Message);
        }

        [Fact] 
        public void TestParseOnlyDash_ThrowsException()
        {
            // Arrange
            string onlyDash = "-";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => BarcodeIndex.Parse(onlyDash));
            Assert.Contains("无效的条形码格式", ex.Message);
        }

        [Fact]
        public void TestParseWhitespaceHandling()
        {
            // Arrange
            string withSpaces = " MedicationOrders - 4 ";

            // Act
            var parsed = BarcodeIndex.Parse(withSpaces);

            // Assert: 应该正确处理空格
            Assert.Equal("MedicationOrders", parsed.TableName);
            Assert.Equal("4", parsed.RecordId);
        }

        [Fact]
        public void TestTableNameMappingConversion()
        {
            // Arrange: 测试大写表名到正确表名的转换
            string uppercaseBarcode = "EXECUTIONTASKS-123";

            // Act
            var parsed = BarcodeIndex.Parse(uppercaseBarcode);

            // Assert: 应该转换为正确的表名
            Assert.Equal("ExecutionTasks", parsed.TableName);
            Assert.Equal("123", parsed.RecordId);
        }

        [Theory]
        [InlineData("DEPARTMENTS", "Departments")]
        [InlineData("STAFFS", "Staffs")]
        [InlineData("DOCTORS", "Doctors")]
        [InlineData("NURSES", "Nurses")]
        [InlineData("WARDS", "Wards")]
        [InlineData("BEDS", "Beds")]
        [InlineData("PATIENTS", "Patients")]
        [InlineData("MEDICALORDERS", "MedicalOrders")]
        [InlineData("MEDICATIONORDERS", "MedicationOrders")]
        [InlineData("INSPECTIONORDERS", "InspectionOrders")]
        [InlineData("SURGICALORDERS", "SurgicalOrders")]
        [InlineData("OPERATIONORDERS", "OperationOrders")]
        [InlineData("EXECUTIONTASKS", "ExecutionTasks")]
        [InlineData("HOSPITALTIMESLOTS", "HospitalTimeSlots")]
        [InlineData("VITALSIGNSRECORDS", "VitalSignsRecords")]
        [InlineData("NURSINGCARENOTES", "NursingCareNotes")]
        public void TestTableNameMapping_AllKnownTables(string uppercaseTableName, string expectedTableName)
        {
            // Arrange
            string barcode = $"{uppercaseTableName}-TEST001";

            // Act
            var parsed = BarcodeIndex.Parse(barcode);

            // Assert
            Assert.Equal(expectedTableName, parsed.TableName);
            Assert.Equal("TEST001", parsed.RecordId);
        }

        [Fact]
        public void TestTableNameMapping_UnknownTable()
        {
            // Arrange: 测试未知表名的处理
            string unknownBarcode = "UNKNOWNTABLE-456";

            // Act
            var parsed = BarcodeIndex.Parse(unknownBarcode);

            // Assert: 未知表名应该保持原样
            Assert.Equal("UNKNOWNTABLE", parsed.TableName);
            Assert.Equal("456", parsed.RecordId);
        }

        [Fact]
        public void TestGenerationAndParsingWithTableNameMapping()
        {
            // Arrange: 测试完整的生成和解析流程
            var originalIndex = new BarcodeIndex
            {
                TableName = "ExecutionTasks",
                RecordId = "ET001"
            };

            // Act: 生成条形码（应该转为大写）
            string barcode = originalIndex.ToString();
            
            // Act: 解析条形码（应该转换回正确的表名）
            var parsed = BarcodeIndex.Parse(barcode);

            // Assert
            Assert.Equal("EXECUTIONTASKS-ET001", barcode);
            Assert.Equal("ExecutionTasks", parsed.TableName);
            Assert.Equal("ET001", parsed.RecordId);
        }
    }
}