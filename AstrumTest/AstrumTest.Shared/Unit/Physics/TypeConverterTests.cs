using System;
using Xunit;
using TrueSync;
using FixMath.NET;
using Astrum.LogicCore.Physics;

namespace AstrumTest.PhysicsTests
{
    /// <summary>
    /// 类型转换器测试
    /// 验证 TrueSync ↔ BEPU 类型转换的正确性和确定性
    /// </summary>
    [Trait("TestLevel", "Unit")]        // 纯单元测试：无依赖，纯函数
    [Trait("Category", "Unit")]
    [Trait("Module", "Physics")]
    [Trait("Priority", "High")]
    public class TypeConverterTests
    {
        #region 标量转换测试 (FP ↔ Fix64)

        [Fact]
        public void Test_FP_To_Fix64_Basic()
        {
            // Arrange
            var fp = FP.One;

            // Act
            var fix64 = fp.ToFix64();

            // Assert
            Assert.Equal(fp._serializedValue, fix64.RawValue);
        }

        [Fact]
        public void Test_Fix64_To_FP_Basic()
        {
            // Arrange
            var fix64 = Fix64.One;

            // Act
            var fp = fix64.ToFP();

            // Assert
            Assert.Equal(fix64.RawValue, fp._serializedValue);
        }

        [Fact]
        public void Test_FP_Fix64_RoundTrip()
        {
            // Arrange
            var original = (FP)3.14159265m;

            // Act
            var fix64 = original.ToFix64();
            var result = fix64.ToFP();

            // Assert
            Assert.Equal(original._serializedValue, result._serializedValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(3.14159265)]
        [InlineData(-2.71828182)]
        [InlineData(100.5)]
        [InlineData(-999.999)]
        public void Test_FP_Fix64_Values(double value)
        {
            // Arrange
            var fp = (FP)(decimal)value;

            // Act
            var fix64 = fp.ToFix64();
            var back = fix64.ToFP();

            // Assert
            Assert.Equal(fp._serializedValue, back._serializedValue);
        }

        #endregion

        #region 向量转换测试 (TSVector ↔ BEPUVector3)

        [Fact]
        public void Test_TSVector_To_BepuVector_Basic()
        {
            // Arrange
            var ts = new TSVector(FP.One, (FP)2, (FP)3);

            // Act
            var bepu = ts.ToBepuVector();

            // Assert
            Assert.Equal(ts.x._serializedValue, bepu.X.RawValue);
            Assert.Equal(ts.y._serializedValue, bepu.Y.RawValue);
            Assert.Equal(ts.z._serializedValue, bepu.Z.RawValue);
        }

        [Fact]
        public void Test_BepuVector_To_TSVector_Basic()
        {
            // Arrange
            var bepu = new BEPUutilities.Vector3((Fix64)1, (Fix64)2, (Fix64)3);

            // Act
            var ts = bepu.ToTSVector();

            // Assert
            Assert.Equal(bepu.X.RawValue, ts.x._serializedValue);
            Assert.Equal(bepu.Y.RawValue, ts.y._serializedValue);
            Assert.Equal(bepu.Z.RawValue, ts.z._serializedValue);
        }

        [Fact]
        public void Test_TSVector_BepuVector_RoundTrip()
        {
            // Arrange
            var original = new TSVector((FP)1.5m, (FP)2.5m, (FP)3.5m);

            // Act
            var bepu = original.ToBepuVector();
            var result = bepu.ToTSVector();

            // Assert
            Assert.Equal(original.x._serializedValue, result.x._serializedValue);
            Assert.Equal(original.y._serializedValue, result.y._serializedValue);
            Assert.Equal(original.z._serializedValue, result.z._serializedValue);
        }

        [Fact]
        public void Test_TSVector_Zero()
        {
            // Arrange
            var ts = TSVector.zero;

            // Act
            var bepu = ts.ToBepuVector();
            var back = bepu.ToTSVector();

            // Assert
            Assert.Equal(FP.Zero._serializedValue, back.x._serializedValue);
            Assert.Equal(FP.Zero._serializedValue, back.y._serializedValue);
            Assert.Equal(FP.Zero._serializedValue, back.z._serializedValue);
        }

        #endregion

        #region 四元数转换测试 (TSQuaternion ↔ BEPUQuaternion)

        [Fact]
        public void Test_TSQuaternion_To_BepuQuaternion_Identity()
        {
            // Arrange
            var ts = TSQuaternion.identity;

            // Act
            var bepu = ts.ToBepuQuaternion();

            // Assert
            Assert.Equal(ts.x._serializedValue, bepu.X.RawValue);
            Assert.Equal(ts.y._serializedValue, bepu.Y.RawValue);
            Assert.Equal(ts.z._serializedValue, bepu.Z.RawValue);
            Assert.Equal(ts.w._serializedValue, bepu.W.RawValue);
        }

        [Fact]
        public void Test_TSQuaternion_BepuQuaternion_RoundTrip()
        {
            // Arrange
            var original = new TSQuaternion((FP)0.1m, (FP)0.2m, (FP)0.3m, (FP)0.9m);

            // Act
            var bepu = original.ToBepuQuaternion();
            var result = bepu.ToTSQuaternion();

            // Assert
            Assert.Equal(original.x._serializedValue, result.x._serializedValue);
            Assert.Equal(original.y._serializedValue, result.y._serializedValue);
            Assert.Equal(original.z._serializedValue, result.z._serializedValue);
            Assert.Equal(original.w._serializedValue, result.w._serializedValue);
        }

        #endregion

        #region 矩阵转换测试 (TSMatrix ↔ BEPUMatrix)

        [Fact]
        public void Test_TSMatrix_BepuMatrix_RoundTrip()
        {
            // Arrange - TSMatrix 是 3x3 旋转矩阵
            var original = TSMatrix.Identity;

            // Act
            var bepu = original.ToBepuMatrix3x3();
            var result = bepu.ToTSMatrix3x3();

            // Assert - 只比较 3x3 部分
            Assert.Equal(original.M11._serializedValue, result.M11._serializedValue);
            Assert.Equal(original.M22._serializedValue, result.M22._serializedValue);
            Assert.Equal(original.M33._serializedValue, result.M33._serializedValue);
            
            // 验证是单位矩阵
            Assert.Equal(FP.One._serializedValue, result.M11._serializedValue);
            Assert.Equal(FP.One._serializedValue, result.M22._serializedValue);
            Assert.Equal(FP.One._serializedValue, result.M33._serializedValue);
        }

        #endregion

        #region 确定性测试

        [Fact]
        public void Test_Determinism_Multiple_Conversions()
        {
            // Arrange
            var original = new TSVector((FP)1.23456789m, (FP)9.87654321m, (FP)5.55555555m);

            // Act - 多次转换
            var bepu1 = original.ToBepuVector();
            var back1 = bepu1.ToTSVector();
            var bepu2 = back1.ToBepuVector();
            var back2 = bepu2.ToTSVector();

            // Assert - 所有结果应该完全相同
            Assert.Equal(original.x._serializedValue, back1.x._serializedValue);
            Assert.Equal(original.y._serializedValue, back1.y._serializedValue);
            Assert.Equal(original.z._serializedValue, back1.z._serializedValue);

            Assert.Equal(back1.x._serializedValue, back2.x._serializedValue);
            Assert.Equal(back1.y._serializedValue, back2.y._serializedValue);
            Assert.Equal(back1.z._serializedValue, back2.z._serializedValue);
        }

        [Fact]
        public void Test_Determinism_Same_Value_Different_Objects()
        {
            // Arrange
            var fp1 = (FP)1.5m;
            var fp2 = (FP)1.5m;

            // Act
            var fix1 = fp1.ToFix64();
            var fix2 = fp2.ToFix64();

            // Assert - 相同的值应该产生相同的结果
            Assert.Equal(fix1.RawValue, fix2.RawValue);
        }

        #endregion

        #region 批量转换测试

        [Fact]
        public void Test_Array_Conversion_ToBepuArray()
        {
            // Arrange
            var tsArray = new[]
            {
                new TSVector(FP.One, FP.Zero, FP.Zero),
                new TSVector(FP.Zero, FP.One, FP.Zero),
                new TSVector(FP.Zero, FP.Zero, FP.One)
            };

            // Act
            var bepuArray = tsArray.ToBepuVectorArray();

            // Assert
            Assert.Equal(tsArray.Length, bepuArray.Length);
            for (int i = 0; i < tsArray.Length; i++)
            {
                Assert.Equal(tsArray[i].x._serializedValue, bepuArray[i].X.RawValue);
                Assert.Equal(tsArray[i].y._serializedValue, bepuArray[i].Y.RawValue);
                Assert.Equal(tsArray[i].z._serializedValue, bepuArray[i].Z.RawValue);
            }
        }

        #endregion
    }
}

