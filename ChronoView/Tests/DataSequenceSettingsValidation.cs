using System;
using System.Collections.Generic;
using ChronoView.Models;

namespace ChronoView.Tests
{
    /// <summary>
    /// Simple validation tests for DataSequenceSettings (manual verification)
    /// Run this in Program.cs Main() or create unit tests later
    /// </summary>
    public static class DataSequenceSettingsValidation
    {
        public static void RunAllTests()
        {
            Console.WriteLine("===== DataSequenceSettings Validation Tests =====\n");

            TestGetByType();
            TestGetMinDelay();
            TestGetMaxDelay();
            TestGetOrderedTypes();
            TestValidation_EmptySequence();
            TestValidation_AllDisabled();
            TestValidation_DuplicateOrder();
            TestValidation_DuplicateType();
            TestValidation_NegativeDelay();
            TestValidation_ZeroTolerance();
            TestValidation_ValidConfiguration();
            TestPresets();

            Console.WriteLine("\n===== All Tests Completed =====");
        }

        private static void TestGetByType()
        {
            Console.WriteLine("Test: GetByType()");
            var settings = DataSequencePresets.NormalFirst();
            
            var nirItem = settings.GetByType(DataType.NIR);
            Assert(nirItem != null, "GetByType(NIR) should return item");
            Assert(nirItem!.Type == DataType.NIR, "Item type should be NIR");
            
            var missing = settings.GetByType((DataType)999);
            Assert(missing == null, "GetByType(invalid) should return null");
            
            Console.WriteLine("  ✓ PASS\n");
        }

        private static void TestGetMinDelay()
        {
            Console.WriteLine("Test: GetMinDelay()");
            var settings = DataSequencePresets.NormalFirst();
            
            double delay = settings.GetMinDelay(DataType.NIR);
            Assert(delay == 1, $"NIR delay should be 1, got {delay}");
            
            double defaultDelay = settings.GetMinDelay((DataType)999);
            Assert(defaultDelay == 0, $"Missing type should return 0, got {defaultDelay}");
            
            Console.WriteLine("  ✓ PASS\n");
        }

        private static void TestGetMaxDelay()
        {
            Console.WriteLine("Test: GetMaxDelay()");
            var settings = DataSequencePresets.NormalFirst();
            
            double tolerance = settings.GetMaxDelay(DataType.NIR);
            Assert(tolerance == 10, $"NIR tolerance should be 10, got {tolerance}");
            
            double defaultTolerance = settings.GetMaxDelay((DataType)999);
            Assert(defaultTolerance == 10, $"Missing type should return 10, got {defaultTolerance}");
            
            Console.WriteLine("  ✓ PASS\n");
        }

        private static void TestGetOrderedTypes()
        {
            Console.WriteLine("Test: GetOrderedTypes()");
            var settings = DataSequencePresets.NormalFirst();
            
            var ordered = settings.GetOrderedTypes();
            Assert(ordered.Count == 5, $"Should have 5 enabled types, got {ordered.Count}"); // Normal, NIR, Cam1-3
            Assert(ordered[0] == DataType.Normal, "First should be Normal");
            Assert(ordered[1] == DataType.NIR, "Second should be NIR");
            
            Console.WriteLine("  ✓ PASS\n");
        }

        private static void TestValidation_EmptySequence()
        {
            Console.WriteLine("Test: Validation - Empty Sequence");
            var settings = new DataSequenceSettings { Sequence = new List<DataSequenceItem>() };
            
            bool valid = settings.Validate(out var errors);
            Assert(!valid, "Empty sequence should fail validation");
            Assert(errors.Count > 0, "Should have error messages");
            
            Console.WriteLine($"  ✓ PASS - Errors: {string.Join(", ", errors)}\n");
        }

        private static void TestValidation_AllDisabled()
        {
            Console.WriteLine("Test: Validation - All Disabled (Timestamp-Only Mode)");
            var settings = new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem { Type = DataType.Normal, Order = 1, MinDelaySeconds = 0, MaxDelaySeconds = 5, Enabled = false },
                    new DataSequenceItem { Type = DataType.NIR, Order = 2, MinDelaySeconds = 1, MaxDelaySeconds = 10, Enabled = false }
                }
            };

            bool valid = settings.Validate(out var errors);
            Assert(valid, "All disabled should be valid (timestamp-only matching mode)");
            Assert(errors.Count == 0, $"Should have no errors, got {errors.Count}");

            Console.WriteLine("  ✓ PASS - All disabled is valid (timestamp-only mode)\n");
        }

        private static void TestValidation_DuplicateOrder()
        {
            Console.WriteLine("Test: Validation - Duplicate Order");
            var settings = new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem { Type = DataType.Normal, Order = 1, MinDelaySeconds = 0, MaxDelaySeconds = 5, Enabled = true },
                    new DataSequenceItem { Type = DataType.NIR, Order = 1, MinDelaySeconds = 1, MaxDelaySeconds = 10, Enabled = true } // Duplicate Order=1
                }
            };
            
            bool valid = settings.Validate(out var errors);
            Assert(!valid, "Duplicate order should fail validation");
            Assert(errors.Any(e => e.Contains("Duplicate Order")), "Should have 'Duplicate Order' error");
            
            Console.WriteLine($"  ✓ PASS - Errors: {string.Join(", ", errors)}\n");
        }

        private static void TestValidation_DuplicateType()
        {
            Console.WriteLine("Test: Validation - Duplicate Type");
            var settings = new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem { Type = DataType.Normal, Order = 1, MinDelaySeconds = 0, MaxDelaySeconds = 5, Enabled = true },
                    new DataSequenceItem { Type = DataType.Normal, Order = 2, MinDelaySeconds = 1, MaxDelaySeconds = 10, Enabled = true } // Duplicate Type=Normal
                }
            };
            
            bool valid = settings.Validate(out var errors);
            Assert(!valid, "Duplicate type should fail validation");
            Assert(errors.Any(e => e.Contains("Duplicate DataType")), "Should have 'Duplicate DataType' error");
            
            Console.WriteLine($"  ✓ PASS - Errors: {string.Join(", ", errors)}\n");
        }

        private static void TestValidation_NegativeDelay()
        {
            Console.WriteLine("Test: Validation - Negative Delay");
            var settings = new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem { Type = DataType.Normal, Order = 1, MinDelaySeconds = -1, MaxDelaySeconds = 5, Enabled = true }
                }
            };
            
            bool valid = settings.Validate(out var errors);
            Assert(!valid, "Negative delay should fail validation");
            Assert(errors.Any(e => e.Contains("Delay must be >= 0")), "Should have delay error");
            
            Console.WriteLine($"  ✓ PASS - Errors: {string.Join(", ", errors)}\n");
        }

        private static void TestValidation_ZeroTolerance()
        {
            Console.WriteLine("Test: Validation - Zero Tolerance");
            var settings = new DataSequenceSettings
            {
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem { Type = DataType.Normal, Order = 1, MinDelaySeconds = 0, MaxDelaySeconds = 0, Enabled = true }
                }
            };
            
            bool valid = settings.Validate(out var errors);
            Assert(!valid, "Zero tolerance should fail validation");
            Assert(errors.Any(e => e.Contains("MaxDelay must be >= 0")), "Should have tolerance error");
            
            Console.WriteLine($"  ✓ PASS - Errors: {string.Join(", ", errors)}\n");
        }

        private static void TestValidation_ValidConfiguration()
        {
            Console.WriteLine("Test: Validation - Valid Configuration");
            var settings = DataSequencePresets.NormalFirst();
            
            bool valid = settings.Validate(out var errors);
            Assert(valid, "Valid configuration should pass");
            Assert(errors.Count == 0, $"Should have no errors, got {errors.Count}");
            
            Console.WriteLine("  ✓ PASS\n");
        }

        private static void TestPresets()
        {
            Console.WriteLine("Test: Presets");
            
            var normalFirst = DataSequencePresets.NormalFirst();
            Assert(normalFirst.Validate(out _), "NormalFirst preset should be valid");
            Assert(normalFirst.Sequence.Count == 8, $"Should have 8 items, got {normalFirst.Sequence.Count}");
            Assert(normalFirst.GetOrderedTypes()[0] == DataType.Normal, "NormalFirst should start with Normal");
            
            var nirFirst = DataSequencePresets.NirFirst();
            Assert(nirFirst.Validate(out _), "NirFirst preset should be valid");
            Assert(nirFirst.GetOrderedTypes()[0] == DataType.NIR, "NirFirst should start with NIR");
            
            var camerasFirst = DataSequencePresets.CamerasFirst();
            Assert(camerasFirst.Validate(out _), "CamerasFirst preset should be valid");
            Assert(camerasFirst.GetOrderedTypes()[0] == DataType.Cam1, "CamerasFirst should start with Cam1");
            
            Console.WriteLine("  ✓ PASS - All presets valid\n");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"ASSERTION FAILED: {message}");
            }
        }
    }
}


