using System.Linq;
using CarWash.ClassLibrary.Models;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class KeyLockerTests
    {
        [Theory]
        [InlineData(0b00000000, new bool[] { false, false, false, false, false, false, false, false })]
        [InlineData(0b00000001, new bool[] { true, false, false, false, false, false, false, false })]
        [InlineData(0b11111111, new bool[] { true, true, true, true, true, true, true, true })]
        [InlineData(0b00001111, new bool[] { true, true, true, true, false, false, false, false })]
        [InlineData(0b0000000100000000, new bool[] { false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false })]
        [InlineData(0b10001000, new bool[] { false, false, false, true, false, false, false, true })]
        [InlineData(0b1000100000000000, new bool[] { false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, true })]
        public void GetBoxStates_ReturnsExpectedStates(int inputs, bool[] expectedStates)
        {
            // Arrange
            var message = new KeyLockerDeviceMessage
            {
                // Use reflection to set internal class property for testing
                Inputs = inputs
            };

            // Act
            var states = message.GetBoxStates();

            // Assert
            Assert.Equal(expectedStates.Count(), states.Count);
            Assert.Equal(expectedStates, states);
        }
    }
}
