using Application.Common;
using Xunit;

namespace ApplicationTests
{
    public class PinValidatorTest
    {
        [Theory()]
        [InlineData("1111", 4)]
        [InlineData(null, 1)]
        [InlineData("", 2)]
        [InlineData("    ", 3)]
        [InlineData("abcd", 3)]
        [InlineData("0000", 4)]
        [InlineData("7777", 4)]
        [InlineData("3333", 4)]
        [InlineData("1234", 5)]
        [InlineData("3456", 5)]

        [InlineData("1911", 0)]
        [InlineData("3666", 0)]
        [InlineData("1837", 0)]

        [InlineData("5437", 0)]
        [InlineData("7893", 0)]
        [InlineData("7895", 0)]
        [InlineData("7890", 0)]
        [InlineData("1112", 0)]

        [InlineData("1231", 0)]
        [InlineData("9993", 0)]


        public void ValidatePinShouldReturnAppropriateStatus(string pin, int pinStatus)
        {
            var res = Utils.ValidatePin(pin);
            Assert.Equal(pinStatus, res);
        }

        [Theory]
        [InlineData("ABCD ", "Abcd")]
        [InlineData("ABCD ABC", "Abcd Abc")]
        [InlineData("A", "A")]
        [InlineData(" A    ", "A")]
        [InlineData(" A.    ", "A.")]
        [InlineData("BB", "Bb")]
        [InlineData("AbCf", "Abcf")]
        [InlineData(" Abcd", "Abcd")]
        [InlineData("Sara R", "Sara R")]
        [InlineData(" Ei-Jen F", "Ei-jen F")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("A,", "A,")]
        [InlineData("  Gules  Myer   ", "Gules Myer")]
        public void ShouldUpperCaseName(string name, string expected)
        {
            var res = Utils.UppercaseFirst(name);
            Assert.Equal(expected, res);
        }

        [InlineData(1, 0, false)]
        [InlineData(110, 0, false)]
        [InlineData(100000005, 0, false)]
        [InlineData(98, 0, false)]
        [InlineData(99, 0, false)]

        [InlineData(98, 99, true)]
        [InlineData(99, 99, true)]
        [InlineData(100, 99, false)]
        [InlineData(101, 99, true)]

        [InlineData(100, 0, false)]
        [InlineData(101, 0, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 2, true)]
        [InlineData(3, 2, false)]
        [InlineData(1, 3, true)]
        [InlineData(2, 3, true)]
        [InlineData(3, 3, true)]

        [Theory]
        public void InPercentShouldReturnCorrectBoolean(int count, int percent, bool expected)
        {
            var result = Utils.InPercentRange(count, percent);
            Assert.Equal(expected, result);

            for (int i = 1; i < 202; i++)
            {
                result = Utils.InPercentRange(i, 1);
                if (i == 1 || i == 101 || i == 201)
                {
                    Assert.True(result);
                }
                else
                {
                    Assert.False(result);
                }
            }

            for (int i = 1; i < 100000; i++)
            {
                result = Utils.InPercentRange(i, 100);
                Assert.True(result);
            }

            for (int i = 1; i < 100000; i++)
            {
                result = Utils.InPercentRange(i, 0);
                Assert.False(result);
            }

        }
    }
}


