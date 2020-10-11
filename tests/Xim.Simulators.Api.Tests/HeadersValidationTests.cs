using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class HeadersValidationTests
    {
        [TestCase("Location")]
        [TestCase("location")]
        [TestCase("0L")]
        [TestCase("te*x")]
        [TestCase("0123456789")]
        [TestCase("abcdefghijklmnopqrstuvwxyz")]
        [TestCase("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        [TestCase("!#$%&'*+-.^_`|~")]
        public void NameContainsInvalidChar_ReturnsFalse_WhenNameValid(string validName)
        {
            var result = HeadersValidation.NameContainsInvalidChar(validName, out (char Char, int Index) character);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeFalse(),
                () => character.ShouldBe(default)
            );
        }

        [TestCase("ce£n", '£', 2)]
        [TestCase("l0l<>", '<', 3)]
        [TestCase(">a", '>', 0)]
        public void NameContainsInvalidChar_ReturnsTrue_WhenNameInvalid(string invalidName, char c, int i)
        {
            var result = HeadersValidation.NameContainsInvalidChar(invalidName, out (char Char, int Index) character);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => character.ShouldBe((c, i))
            );
        }

        [TestCase("abc;def")]
        [TestCase("'quote'")]
        [TestCase("abc and \"def\"")]
        public void ValueContainsInvalidChar_ReturnsFalse_WhenValueValid(string validValue)
        {
            var result = HeadersValidation.ValueContainsInvalidChar(validValue, out (char Char, int Index) character);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeFalse(),
                () => character.ShouldBe(default)
            );
        }

        [TestCase("\ryes", '\r', 0, TestName = "ValueContainsInvalidChar_ReturnsTrue_WhenValueInvalid(ContainsNewline)")]
        [TestCase("tom \x7f 32", '\x7f', 4, TestName = "ValueContainsInvalidChar_ReturnsTrue_WhenValueInvalid(Contains\\x7f)")]
        public void ValueContainsInvalidChar_ReturnsTrue_WhenValueInvalid(string invalidValue, char c, int i)
        {
            var result = HeadersValidation.ValueContainsInvalidChar(invalidValue, out (char Char, int Index) character);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => character.ShouldBe((c, i))
            );
        }
    }
}
