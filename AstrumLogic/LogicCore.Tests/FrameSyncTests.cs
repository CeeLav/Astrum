using Xunit;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Tests
{
    /// <summary>
    /// 帧同步相关的单元测试
    /// </summary>
    public class FrameSyncTests
    {
        [Fact]
        public void LSInput_Clone_ShouldCreateExactCopy()
        {
            // Arrange
            var original = new LSInput
            {
                PlayerId = 1,
                Frame = 10,
                MoveX = 0.5f,
                MoveY = -0.3f,
                Attack = true,
                Skill1 = false,
                Skill2 = true,
                Timestamp = 123456789
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(original.PlayerId, clone.PlayerId);
            Assert.Equal(original.Frame, clone.Frame);
            Assert.Equal(original.MoveX, clone.MoveX);
            Assert.Equal(original.MoveY, clone.MoveY);
            Assert.Equal(original.Attack, clone.Attack);
            Assert.Equal(original.Skill1, clone.Skill1);
            Assert.Equal(original.Skill2, clone.Skill2);
            Assert.Equal(original.Timestamp, clone.Timestamp);
        }

        [Fact]
        public void LSInput_IsEmpty_ShouldReturnTrueForEmptyInput()
        {
            // Arrange
            var emptyInput = new LSInput();

            // Act & Assert
            Assert.True(emptyInput.IsEmpty());
        }

        [Fact]
        public void LSInput_IsEmpty_ShouldReturnFalseForNonEmptyInput()
        {
            // Arrange
            var nonEmptyInput = new LSInput { MoveX = 0.5f };

            // Act & Assert
            Assert.False(nonEmptyInput.IsEmpty());
        }

        [Fact]
        public void LSInput_GetMoveInputMagnitude_ShouldCalculateCorrectly()
        {
            // Arrange
            var input = new LSInput { MoveX = 0.6f, MoveY = 0.8f };

            // Act
            var magnitude = input.GetMoveInputMagnitude();

            // Assert
            Assert.Equal(1.0f, magnitude, 5); // 0.6^2 + 0.8^2 = 1.0
        }

        [Fact]
        public void OneFrameInputs_AddInput_ShouldStoreInput()
        {
            // Arrange
            var frameInputs = new OneFrameInputs(10);
            var input = new LSInput { PlayerId = 1, MoveX = 0.5f };

            // Act
            frameInputs.AddInput(1, input);

            // Assert
            Assert.True(frameInputs.HasInputForPlayer(1));
            var retrievedInput = frameInputs.GetInput(1);
            Assert.NotNull(retrievedInput);
            Assert.Equal(1, retrievedInput.PlayerId);
            Assert.Equal(10, retrievedInput.Frame);
            Assert.Equal(0.5f, retrievedInput.MoveX);
        }

        [Fact]
        public void OneFrameInputs_HasAllInputs_ShouldReturnCorrectResult()
        {
            // Arrange
            var frameInputs = new OneFrameInputs(10);
            frameInputs.AddInput(1, new LSInput { PlayerId = 1 });
            frameInputs.AddInput(2, new LSInput { PlayerId = 2 });

            // Act & Assert
            Assert.True(frameInputs.HasAllInputs(2));
            Assert.False(frameInputs.HasAllInputs(3));
        }

        [Fact]
        public void OneFrameInputs_Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = new OneFrameInputs(10);
            original.AddInput(1, new LSInput { PlayerId = 1, MoveX = 0.5f });
            original.IsComplete = true;

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(original.Frame, clone.Frame);
            Assert.Equal(original.IsComplete, clone.IsComplete);
            Assert.Equal(original.GetInputCount(), clone.GetInputCount());
            
            var originalInput = original.GetInput(1);
            var clonedInput = clone.GetInput(1);
            Assert.NotNull(originalInput);
            Assert.NotNull(clonedInput);
            Assert.NotSame(originalInput, clonedInput);
            Assert.Equal(originalInput.MoveX, clonedInput.MoveX);
        }

        [Fact]
        public void FrameBuffer_AddFrame_ShouldStoreFrame()
        {
            // Arrange
            var frameBuffer = new FrameBuffer();
            var frameInputs = new OneFrameInputs(10);

            // Act
            frameBuffer.AddFrame(10, frameInputs);

            // Assert
            Assert.True(frameBuffer.HasFrame(10));
            var retrievedFrame = frameBuffer.GetFrame(10);
            Assert.NotNull(retrievedFrame);
            Assert.Equal(10, retrievedFrame.Frame);
        }

        [Fact]
        public void FrameBuffer_GetFrameRange_ShouldReturnCorrectFrames()
        {
            // Arrange
            var frameBuffer = new FrameBuffer();
            frameBuffer.AddFrame(10, new OneFrameInputs(10));
            frameBuffer.AddFrame(11, new OneFrameInputs(11));
            frameBuffer.AddFrame(12, new OneFrameInputs(12));

            // Act
            var frames = frameBuffer.GetFrameRange(10, 12);

            // Assert
            Assert.Equal(3, frames.Count);
            Assert.Equal(10, frames[0].Frame);
            Assert.Equal(11, frames[1].Frame);
            Assert.Equal(12, frames[2].Frame);
        }

        [Fact]
        public void FrameBuffer_RemoveOldFrames_ShouldRemoveFramesBelowThreshold()
        {
            // Arrange
            var frameBuffer = new FrameBuffer();
            frameBuffer.AddFrame(5, new OneFrameInputs(5));
            frameBuffer.AddFrame(10, new OneFrameInputs(10));
            frameBuffer.AddFrame(15, new OneFrameInputs(15));

            // Act
            frameBuffer.RemoveOldFrames(10);

            // Assert
            Assert.False(frameBuffer.HasFrame(5));
            Assert.True(frameBuffer.HasFrame(10));
            Assert.True(frameBuffer.HasFrame(15));
        }

        [Fact]
        public void LSInputComponent_SetInput_ShouldUpdateCurrentAndPreviousInput()
        {
            // Arrange
            var inputComponent = new LSInputComponent(1);
            var firstInput = new LSInput { PlayerId = 1, MoveX = 0.5f };
            var secondInput = new LSInput { PlayerId = 1, MoveX = 0.8f };

            // Act
            inputComponent.SetInput(firstInput);
            inputComponent.SetInput(secondInput);

            // Assert
            Assert.NotNull(inputComponent.CurrentInput);
            Assert.NotNull(inputComponent.PreviousInput);
            Assert.Equal(0.8f, inputComponent.CurrentInput.MoveX);
            Assert.Equal(0.5f, inputComponent.PreviousInput.MoveX);
        }

        [Fact]
        public void LSInputComponent_IsInputJustPressed_ShouldDetectKeyPress()
        {
            // Arrange
            var inputComponent = new LSInputComponent(1);
            var firstInput = new LSInput { PlayerId = 1, Attack = false };
            var secondInput = new LSInput { PlayerId = 1, Attack = true };

            // Act
            inputComponent.SetInput(firstInput);
            inputComponent.SetInput(secondInput);

            // Assert
            Assert.True(inputComponent.IsInputJustPressed(InputType.Attack));
        }

        [Fact]
        public void LSInputComponent_IsInputJustReleased_ShouldDetectKeyRelease()
        {
            // Arrange
            var inputComponent = new LSInputComponent(1);
            var firstInput = new LSInput { PlayerId = 1, Attack = true };
            var secondInput = new LSInput { PlayerId = 1, Attack = false };

            // Act
            inputComponent.SetInput(firstInput);
            inputComponent.SetInput(secondInput);

            // Assert
            Assert.True(inputComponent.IsInputJustReleased(InputType.Attack));
        }
    }
}
