using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MySqlVowisQueryTests;

[TestClass]
public class VowisQueryTests : VowisQueryTestFramework
{
    [TestMethod]
    [TestCategory("Setup")]
    public async Task Test_000_DatabaseTablesExist()
    {
        // Act & Assert
        await VerifyTablesExistAsync();
    }

    [TestMethod]
    [TestCategory("Setup")]
    public async Task Test_001_SetupTestData()
    {
        // Act & Assert
        await SetupTestDataAsync();
    }

    [TestMethod]
    [TestCategory("Positive")]
    public async Task Test_100_VowisQuery_ReturnsExpectedPositiveResults()
    {
        // Arrange
        await SetupTestDataAsync();

        // Act
        var results = await ExecuteQueryAsync(VowisQuery);
        var resultList = results.ToList();

        // Assert
        Logger?.Information("Vowis query returned {Count} results", resultList.Count);
        
        // Should return at least 3 positive test cases:
        // 1. Phương Nguyễn (expires in 20 days)
        // 2. Minh Phương (already inactive)  
        // 3. Phương Trần (expires in 45 days)
        Assert.IsTrue(resultList.Count >= 3, $"Expected at least 3 results, got {resultList.Count}");
        
        // Validate each result
        foreach (var result in resultList)
        {
            ValidateQueryResultStructure(result);
            ValidateBusinessLogic(result);
        }

        Logger?.Information("✅ All {Count} results passed validation", resultList.Count);
    }

    [TestMethod]
    [TestCategory("Positive")]
    public async Task Test_101_PositiveCase_PhuongNguyen_ActiveExpiring()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1001"; // Phương Nguyễn

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(1, resultList.Count, "Should return exactly 1 result for Phương Nguyễn");
        
        var result = resultList.First();
        var resultDict = (IDictionary<string, object>)result;
        
        Assert.AreEqual("Phương", resultDict["first_name"], "First name should be Phương");
        Assert.AreEqual("Nguyễn", resultDict["last_name"], "Last name should be Nguyễn"); 
        Assert.AreEqual(1989, Convert.ToDateTime(resultDict["date_of_birth"]).Year, "Birth year should be 1989");
        Assert.IsTrue(resultDict["company_name"]?.ToString()?.Contains("VNEXT") == true, "Company should contain VNEXT");
        Assert.IsTrue(resultDict["role_code"]?.ToString()?.Contains("DD") == true, "Role should contain DD");
        Assert.AreEqual("Sắp hết hạn vai trò", resultDict["work_status"], "Should be expiring soon");

        Logger?.Information("✅ Phương Nguyễn test case passed");
    }

    [TestMethod]
    [TestCategory("Positive")]
    public async Task Test_102_PositiveCase_MinhPhuong_InactiveUser()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1002"; // Minh Phương

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(1, resultList.Count, "Should return exactly 1 result for Minh Phương");
        
        var result = resultList.First();
        var resultDict = (IDictionary<string, object>)result;
        
        Assert.AreEqual("Minh", resultDict["first_name"], "First name should be Minh");
        Assert.AreEqual("Phương", resultDict["last_name"], "Last name should be Phương");
        Assert.AreEqual(1989, Convert.ToDateTime(resultDict["date_of_birth"]).Year, "Birth year should be 1989");
        Assert.IsTrue(resultDict["company_name"]?.ToString()?.Contains("VNEXT") == true, "Company should contain VNEXT");
        Assert.IsTrue(resultDict["role_code"]?.ToString()?.Contains("DD") == true, "Role should contain DD");
        Assert.AreEqual("Đã nghỉ việc", resultDict["work_status"], "Should be already left");

        Logger?.Information("✅ Minh Phương test case passed");
    }

    [TestMethod]
    [TestCategory("Positive")]
    public async Task Test_103_PositiveCase_PhuongTran_ActiveExpiring()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1003"; // Phương Trần

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(1, resultList.Count, "Should return exactly 1 result for Phương Trần");
        
        var result = resultList.First();
        var resultDict = (IDictionary<string, object>)result;
        
        Assert.AreEqual("Phương", resultDict["first_name"], "First name should be Phương");
        Assert.AreEqual("Trần", resultDict["last_name"], "Last name should be Trần");
        Assert.AreEqual(1989, Convert.ToDateTime(resultDict["date_of_birth"]).Year, "Birth year should be 1989");
        Assert.IsTrue(resultDict["company_name"]?.ToString()?.Contains("VNEXT") == true, "Company should contain VNEXT");
        Assert.IsTrue(resultDict["role_code"]?.ToString()?.Contains("DD") == true, "Role should contain DD");
        Assert.AreEqual("Sắp hết hạn vai trò", resultDict["work_status"], "Should be expiring soon");

        Logger?.Information("✅ Phương Trần test case passed");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public async Task Test_200_NegativeCase_PhuongLe_NoExpiration()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1004"; // Phương Lê - no expiration

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(0, resultList.Count, "Should return 0 results for Phương Lê (no expiration)");
        
        Logger?.Information("✅ Phương Lê negative test case passed (no results as expected)");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public async Task Test_201_NegativeCase_DucNguyen_NotPhuongName()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1005"; // Đức Nguyễn - not Phương

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(0, resultList.Count, "Should return 0 results for Đức Nguyễn (name doesn't contain Phương)");
        
        Logger?.Information("✅ Đức Nguyễn negative test case passed (no results as expected)");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public async Task Test_202_NegativeCase_PhuongHoang_WrongBirthYear()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1006"; // Phương Hoàng - born 1990

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(0, resultList.Count, "Should return 0 results for Phương Hoàng (born 1990, not 1989)");
        
        Logger?.Information("✅ Phương Hoàng negative test case passed (no results as expected)");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public async Task Test_203_NegativeCase_PhuongPham_WrongCompany()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1007"; // Phương Phạm - FPT company

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(0, resultList.Count, "Should return 0 results for Phương Phạm (FPT company, not VNEXT)");
        
        Logger?.Information("✅ Phương Phạm negative test case passed (no results as expected)");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public async Task Test_204_NegativeCase_PhuongDo_WrongRole()
    {
        // Arrange
        await SetupTestDataAsync();
        
        var specificQuery = VowisQuery + " AND u.id = 1008"; // Phương Đỗ - MANAGER role

        // Act
        var results = await ExecuteQueryAsync(specificQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(0, resultList.Count, "Should return 0 results for Phương Đỗ (MANAGER role, not DD)");
        
        Logger?.Information("✅ Phương Đỗ negative test case passed (no results as expected)");
    }

    [TestMethod]
    [TestCategory("BusinessLogic")]
    public async Task Test_300_ValidateWorkStatusLogic()
    {
        // Arrange
        await SetupTestDataAsync();

        // Act - Execute custom query to test CASE WHEN logic
        var statusTestQuery = @"
            SELECT u.id, u.first_name, u.last_name, u.is_active, ur.expires_at,
                   CASE 
                       WHEN u.is_active = 0 THEN 'Đã nghỉ việc'
                       WHEN ur.expires_at IS NOT NULL AND ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY) THEN 'Sắp hết hạn vai trò'
                       ELSE 'Đang làm việc'
                   END AS work_status
            FROM users u
            JOIN user_roles ur ON u.id = ur.user_id
            WHERE u.id IN (1001, 1002, 1003, 1004)
            ORDER BY u.id";

        var results = await ExecuteQueryAsync(statusTestQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(4, resultList.Count, "Should return 4 test users");

        foreach (var result in resultList)
        {
            var resultDict = (IDictionary<string, object>)result;
            var userId = Convert.ToInt32(resultDict["id"]);
            var isActive = Convert.ToBoolean(resultDict["is_active"]);
            var workStatus = resultDict["work_status"]?.ToString();

            switch (userId)
            {
                case 1001: // Phương Nguyễn - active, expires soon
                    Assert.IsTrue(isActive, "User 1001 should be active");
                    Assert.AreEqual("Sắp hết hạn vai trò", workStatus, "User 1001 should be expiring soon");
                    break;
                    
                case 1002: // Minh Phương - inactive
                    Assert.IsFalse(isActive, "User 1002 should be inactive");
                    Assert.AreEqual("Đã nghỉ việc", workStatus, "User 1002 should have already left");
                    break;
                    
                case 1003: // Phương Trần - active, expires soon
                    Assert.IsTrue(isActive, "User 1003 should be active");
                    Assert.AreEqual("Sắp hết hạn vai trò", workStatus, "User 1003 should be expiring soon");
                    break;
                    
                case 1004: // Phương Lê - active, no expiration
                    Assert.IsTrue(isActive, "User 1004 should be active");
                    Assert.AreEqual("Đang làm việc", workStatus, "User 1004 should be working normally");
                    break;
            }
        }

        Logger?.Information("✅ Work status logic validation passed for all test cases");
    }

    [TestMethod]
    [TestCategory("BusinessLogic")]
    public async Task Test_301_ValidateDateCalculations()
    {
        // Arrange
        await SetupTestDataAsync();

        // Act - Test DATE_ADD and INTERVAL calculations
        var dateTestQuery = @"
            SELECT ur.user_id, ur.expires_at,
                   DATE_ADD(NOW(), INTERVAL 30 DAY) as thirty_days_future,
                   DATE_ADD(NOW(), INTERVAL 60 DAY) as sixty_days_future,
                   (ur.expires_at <= DATE_ADD(NOW(), INTERVAL 30 DAY)) as expires_within_30,
                   (ur.expires_at <= DATE_ADD(NOW(), INTERVAL 60 DAY)) as expires_within_60
            FROM user_roles ur
            WHERE ur.user_id IN (1001, 1002, 1003, 1004)
            ORDER BY ur.user_id";

        var results = await ExecuteQueryAsync(dateTestQuery);
        var resultList = results.ToList();

        // Assert
        Assert.AreEqual(4, resultList.Count, "Should return 4 user role records");

        foreach (var result in resultList)
        {
            var resultDict = (IDictionary<string, object>)result;
            var userId = Convert.ToInt32(resultDict["user_id"]);
            var expiresWithin30 = Convert.ToBoolean(resultDict["expires_within_30"]);
            var expiresWithin60 = Convert.ToBoolean(resultDict["expires_within_60"]);

            switch (userId)
            {
                case 1001: // Expires in 20 days
                    Assert.IsTrue(expiresWithin30, "User 1001 should expire within 30 days");
                    Assert.IsTrue(expiresWithin60, "User 1001 should expire within 60 days");
                    break;
                    
                case 1002: // Already expired 
                    Assert.IsTrue(expiresWithin30, "User 1002 expired (should be within 30 days)");
                    Assert.IsTrue(expiresWithin60, "User 1002 expired (should be within 60 days)");
                    break;
                    
                case 1003: // Expires in 45 days
                    Assert.IsFalse(expiresWithin30, "User 1003 should NOT expire within 30 days");
                    Assert.IsTrue(expiresWithin60, "User 1003 should expire within 60 days");
                    break;
                    
                case 1004: // No expiration (NULL)
                    Assert.IsFalse(expiresWithin30, "User 1004 has no expiration (NULL check)");
                    Assert.IsFalse(expiresWithin60, "User 1004 has no expiration (NULL check)");
                    break;
            }
        }

        Logger?.Information("✅ Date calculation logic validation passed");
    }

    [TestMethod]
    [TestCategory("Performance")]
    public async Task Test_400_QueryPerformance()
    {
        // Arrange
        await SetupTestDataAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await ExecuteQueryAsync(VowisQuery);
        stopwatch.Stop();

        var resultCount = results.Count();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, $"Query should complete within 5 seconds, took {stopwatch.ElapsedMilliseconds}ms");
        Assert.IsTrue(resultCount >= 0, "Query should return at least 0 results");

        Logger?.Information("✅ Query performance test passed - {ElapsedMs}ms, {ResultCount} results", 
            stopwatch.ElapsedMilliseconds, resultCount);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Test_500_FullIntegrationTest()
    {
        // Arrange
        Logger?.Information("Starting full integration test");

        // Step 1: Verify tables exist
        await VerifyTablesExistAsync();

        // Step 2: Setup test data
        await SetupTestDataAsync();

        // Step 3: Execute main query
        var results = await ExecuteQueryAsync(VowisQuery);
        var resultList = results.ToList();

        // Step 4: Validate all positive results
        Assert.IsTrue(resultList.Count >= 3, $"Expected at least 3 positive results, got {resultList.Count}");

        var positiveUserIds = new HashSet<int> { 1001, 1002, 1003 };
        var returnedUserIds = new HashSet<int>();

        foreach (var result in resultList)
        {
            ValidateQueryResultStructure(result);
            ValidateBusinessLogic(result);
            
            var resultDict = (IDictionary<string, object>)result;
            var userId = Convert.ToInt32(resultDict["id"]);
            returnedUserIds.Add(userId);
        }

        // Step 5: Verify expected users are returned
        foreach (var expectedUserId in positiveUserIds)
        {
            Assert.IsTrue(returnedUserIds.Contains(expectedUserId), 
                $"Expected user {expectedUserId} not found in results");
        }

        // Step 6: Verify negative test cases are not returned
        var negativeUserIds = new int[] { 1004, 1005, 1006, 1007, 1008 };
        foreach (var negativeUserId in negativeUserIds)
        {
            Assert.IsFalse(returnedUserIds.Contains(negativeUserId), 
                $"Negative test user {negativeUserId} should not be in results");
        }

        Logger?.Information("✅ Full integration test passed - all scenarios validated");
    }
} 