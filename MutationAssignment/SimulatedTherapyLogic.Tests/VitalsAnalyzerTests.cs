using Xunit;
using SimulatedTherapyLogic;

namespace SimulatedTherapyLogic.Tests
{
    public class VitalsAnalyzerTests
    {
        private readonly VitalsAnalyzer _analyzer = new VitalsAnalyzer();

        [Fact]
        public void AnalyzeHeartRate_ReturnsInvalidData()
        {
            var result = _analyzer.AnalyzeHeartRate(-1, 80, 10);
            Assert.Equal("Invalid Data", result);
        }

        [Fact]
        public void AnalyzeHeartRate_ReturnsInsufficientData()
        {
            var result = _analyzer.AnalyzeHeartRate(70, 110, 4);
            Assert.Equal("Insufficient Data", result);
        }

        [Fact]
        public void AnalyzeHeartRate_ReturnsSevereAnxiety()
        {
            // Weak Test: diff = 40 (>= 30), exposure = 20 (> 15). Misses boundary.
            var result = _analyzer.AnalyzeHeartRate(70, 110, 20);
            Assert.Equal("Severe Anxiety", result);
        }

        [Fact]
        public void AnalyzeHeartRate_ReturnsModerateAnxiety()
        {
            // Weak Test: diff = 20 (>= 15). Misses boundary.
            var result = _analyzer.AnalyzeHeartRate(70, 90, 20);
            Assert.Equal("Moderate Anxiety", result);
        }

        [Fact]
        public void AnalyzeHeartRate_ReturnsMildAnxiety()
        {
            // Weak Test: diff = 10 (> 5). Misses boundary.
            var result = _analyzer.AnalyzeHeartRate(70, 80, 20);
            Assert.Equal("Mild Anxiety", result);
        }

        [Fact]
        public void AnalyzeHeartRate_ReturnsCalm()
        {
            var result = _analyzer.AnalyzeHeartRate(70, 70, 20);
            Assert.Equal("Calm", result);
        }

        [Fact]
        public void IsSessionValid_ReturnsTrue()
        {
            // Weak Test: 120 >= 60, 80 > 50
            Assert.True(_analyzer.IsSessionValid(120, 80));
        }

        [Fact]
        public void IsSessionValid_ReturnsFalse()
        {
            Assert.False(_analyzer.IsSessionValid(30, 80));
            Assert.False(_analyzer.IsSessionValid(120, 40));
        }

        [Fact]
        public void CalculateAnxietyScore_ReturnsZero()
        {
            Assert.Equal(0.0, _analyzer.CalculateAnxietyScore(70, 80));
        }

        [Fact]
        public void CalculateAnxietyScore_ReturnsScore()
        {
            Assert.Equal(15.0, _analyzer.CalculateAnxietyScore(90, 80));
        }

        [Fact]
        public void NeedsIntervention_ReturnsTrue()
        {
            Assert.True(_analyzer.NeedsIntervention(160, 150));
        }

        [Fact]
        public void NeedsIntervention_ReturnsFalse()
        {
            Assert.False(_analyzer.NeedsIntervention(140, 150));
        }
    }
}
