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
    }
}