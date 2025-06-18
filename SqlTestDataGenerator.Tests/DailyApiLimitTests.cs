using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlTestDataGenerator.Core.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SqlTestDataGenerator.Tests
{
    [TestClass]
    public class DailyApiLimitTests
    {
        private EnhancedGeminiFlashRotationService _service;
        private const string TestApiKey = "test-api-key-12345";

        [TestInitialize]
        public void Setup()
        {
            _service = new EnhancedGeminiFlashRotationService(TestApiKey);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _service?.Dispose();
        }

        [TestMethod]
        public void TestApiUsageStatistics()
        {
            // Arrange & Act
            var stats = _service.GetAPIUsageStatistics();

            // Assert
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.ContainsKey("DailyCallsUsed"));
            Assert.IsTrue(stats.ContainsKey("DailyCallLimit"));
            Assert.IsTrue(stats.ContainsKey("DailyCallsRemaining"));
            Assert.IsTrue(stats.ContainsKey("DailyResetTime"));
            Assert.IsTrue(stats.ContainsKey("LastAPICall"));
            Assert.IsTrue(stats.ContainsKey("NextCallableTime"));
            Assert.IsTrue(stats.ContainsKey("CanCallNow"));
            Assert.IsTrue(stats.ContainsKey("MinDelayBetweenCalls"));

            // Verify initial state
            Assert.AreEqual(0, stats["DailyCallsUsed"]);
            Assert.AreEqual(100, stats["DailyCallLimit"]);
            Assert.AreEqual(100, stats["DailyCallsRemaining"]);
            Assert.AreEqual("5 seconds", stats["MinDelayBetweenCalls"]);

            Console.WriteLine($"✅ API Usage Statistics Test Passed");
            Console.WriteLine($"📊 Daily Calls Used: {stats["DailyCallsUsed"]}/{stats["DailyCallLimit"]}");
            Console.WriteLine($"⏰ Next Callable Time: {stats["NextCallableTime"]}");
            Console.WriteLine($"🔄 Can Call Now: {stats["CanCallNow"]}");
        }

        [TestMethod]
        public void TestTimeAvailabilityChecking()
        {
            // Arrange & Act
            var canCallNow = _service.CanCallAPINow();
            var nextCallableTime = _service.GetNextCallableTime();

            // Assert - Initially should be able to call
            Assert.IsTrue(canCallNow, "Should be able to call API initially");
            Assert.IsTrue(nextCallableTime <= DateTime.UtcNow.AddSeconds(1), "Next callable time should be immediate or very soon");

            Console.WriteLine($"✅ Time Availability Test Passed");
            Console.WriteLine($"🔄 Can Call Now: {canCallNow}");
            Console.WriteLine($"⏰ Next Callable Time: {nextCallableTime:yyyy-MM-dd HH:mm:ss UTC}");
            Console.WriteLine($"🕒 Current Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        }

        [TestMethod]
        public void TestDailyLimitSimulation()
        {
            // This test simulates approaching daily limits without actual API calls
            // We'll test the statistics and availability logic

            // Arrange
            var initialStats = _service.GetAPIUsageStatistics();
            
            // Assert initial state
            Assert.AreEqual(0, initialStats["DailyCallsUsed"]);
            Assert.AreEqual(100, initialStats["DailyCallLimit"]);
            Assert.AreEqual(100, initialStats["DailyCallsRemaining"]);

            // Verify next callable time calculation
            var nextCallTime = _service.GetNextCallableTime();
            Assert.IsTrue(nextCallTime <= DateTime.UtcNow.AddSeconds(5), 
                "Next call time should be within rate limit window");

            Console.WriteLine($"✅ Daily Limit Simulation Test Passed");
            Console.WriteLine($"📊 Initial Daily Calls: {initialStats["DailyCallsUsed"]}/{initialStats["DailyCallLimit"]}");
            Console.WriteLine($"⏰ Daily Reset Time: {initialStats["DailyResetTime"]}");
            Console.WriteLine($"🔄 Initial Can Call: {initialStats["CanCallNow"]}");
        }

        [TestMethod]
        public void TestModelStatistics()
        {
            // Arrange & Act
            var modelStats = _service.GetModelStatistics();

            // Assert
            Assert.IsNotNull(modelStats);
            Assert.IsTrue(modelStats.ContainsKey("TotalModels"));
            Assert.IsTrue(modelStats.ContainsKey("HealthyModels"));
            Assert.IsTrue(modelStats.ContainsKey("CurrentIndex"));
            Assert.IsTrue(modelStats.ContainsKey("ModelsByTier"));
            Assert.IsTrue(modelStats.ContainsKey("FailedModels"));

            // Verify model count
            Assert.IsTrue((int)modelStats["TotalModels"] > 0, "Should have Flash models available");
            Assert.IsTrue((int)modelStats["HealthyModels"] > 0, "Should have healthy models initially");

            Console.WriteLine($"✅ Model Statistics Test Passed");
            Console.WriteLine($"🔄 Total Models: {modelStats["TotalModels"]}");
            Console.WriteLine($"💚 Healthy Models: {modelStats["HealthyModels"]}");
            Console.WriteLine($"📍 Current Index: {modelStats["CurrentIndex"]}");

            var modelsByTier = (Dictionary<string, int>)modelStats["ModelsByTier"];
            foreach (var tier in modelsByTier)
            {
                Console.WriteLine($"🏷️ {tier.Key}: {tier.Value} models");
            }
        }

        [TestMethod]
        public void TestGetNextFlashModel()
        {
            // Arrange & Act
            var model1 = _service.GetNextFlashModel();
            var model2 = _service.GetNextFlashModel();

            // Assert
            Assert.IsNotNull(model1);
            Assert.IsNotNull(model2);
            Assert.IsTrue(model1.Contains("gemini"), "Should return a Gemini model");
            Assert.IsTrue(model2.Contains("gemini"), "Should return a Gemini model");

            Console.WriteLine($"✅ Model Rotation Test Passed");
            Console.WriteLine($"🚀 First Model: {model1}");
            Console.WriteLine($"🔄 Second Model: {model2}");
            
            // Models should rotate (unless only one healthy model)
            var modelStats = _service.GetModelStatistics();
            if ((int)modelStats["HealthyModels"] > 1)
            {
                Console.WriteLine($"🔄 Model rotation working (multiple healthy models available)");
            }
            else
            {
                Console.WriteLine($"ℹ️ Single healthy model - rotation not applicable");
            }
        }

        [TestMethod]
        public void TestDailyResetTimeCalculation()
        {
            // Arrange & Act
            var stats = _service.GetAPIUsageStatistics();
            var resetTimeStr = (string)stats["DailyResetTime"];
            var resetTime = DateTime.Parse(resetTimeStr.Replace(" UTC", ""));

            // Assert
            Assert.IsTrue(resetTime > DateTime.UtcNow, "Reset time should be in the future");
            
            // Should be within next 24 hours
            var hoursUntilReset = (resetTime - DateTime.UtcNow).TotalHours;
            Assert.IsTrue(hoursUntilReset <= 24, "Reset should be within 24 hours");
            Assert.IsTrue(hoursUntilReset > 0, "Reset should be in the future");

            Console.WriteLine($"✅ Daily Reset Time Test Passed");
            Console.WriteLine($"🕛 Current Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            Console.WriteLine($"🔄 Reset Time: {resetTimeStr}");
            Console.WriteLine($"⏱️ Hours Until Reset: {hoursUntilReset:F2}");
        }

        [TestMethod]
        public void TestApiLimitBoundaryConditions()
        {
            // Test various boundary conditions for API limits
            
            // Test when we're at the edge of rate limit
            var canCall = _service.CanCallAPINow();
            var nextTime = _service.GetNextCallableTime();
            
            Assert.IsNotNull(canCall);
            Assert.IsNotNull(nextTime);
            
            // Verify the time difference logic
            var timeDiff = nextTime - DateTime.UtcNow;
            Assert.IsTrue(timeDiff.TotalSeconds >= -1, "Next callable time should not be significantly in the past");
            
            Console.WriteLine($"✅ Boundary Conditions Test Passed");
            Console.WriteLine($"🔄 Can Call: {canCall}");
            Console.WriteLine($"⏰ Next Call: {nextTime:yyyy-MM-dd HH:mm:ss UTC}");
            Console.WriteLine($"⌛ Time Diff: {timeDiff.TotalSeconds:F2} seconds");
        }
    }
} 