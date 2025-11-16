using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Collections;
using System.Net;
using System.Reflection;

namespace TestBackendService.Controllers.UnitTests;

[TestFixture]
public class UtilityControllerTests
{

    [Test]
    public void Health_Always_ReturnsOkWithHealthyStatusAndValidUtcTimestamp()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var controller = new UtilityController(loggerMock.Object);
        var beforeUtc = DateTime.UtcNow;

        // Act
        var actionResult = controller.Health();
        var afterUtc = DateTime.UtcNow;

        // Assert
        Assert.That(actionResult, Is.Not.Null);
        var okResult = actionResult.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null, "Expected OkObjectResult from Health().");
        Assert.That(okResult!.StatusCode, Is.EqualTo((int)HttpStatusCode.OK), "Expected HTTP 200 OK.");

        var payload = okResult.Value;
        Assert.That(payload, Is.Not.Null, "Expected non-null payload.");

        var statusProp = payload!.GetType().GetProperty("status", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(statusProp, Is.Not.Null, "Payload must contain 'status' property.");
        var statusValue = statusProp!.GetValue(payload) as string;
        Assert.That(statusValue, Is.EqualTo("Healthy"), "Expected status to be 'Healthy'.");

        var timeProp = payload.GetType().GetProperty("timeUtc", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(timeProp, Is.Not.Null, "Payload must contain 'timeUtc' property.");
        var timeValueObj = timeProp!.GetValue(payload);
        Assert.That(timeValueObj, Is.Not.Null, "Expected 'timeUtc' value to be non-null.");
        var timeValue = (DateTime)timeValueObj!;
        Assert.That(timeValue.Kind, Is.EqualTo(DateTimeKind.Utc), "Expected 'timeUtc' to be UTC.");
        Assert.That(timeValue, Is.InRange(beforeUtc, afterUtc), "Expected 'timeUtc' to be within the invocation window.");
    }

    [Test]
    public void Ping_Always_ReturnsOkWithPongMessageAndRecentUtcTimestamp()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var controller = new UtilityController(loggerMock.Object);
        var before = DateTime.UtcNow;

        // Act
        var actionResult = controller.Ping();
        var after = DateTime.UtcNow;

        // Assert
        Assert.That(actionResult, Is.Not.Null, "ActionResult should not be null.");

        var okResult = actionResult.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null, "Result should be OkObjectResult.");
        Assert.That(okResult!.StatusCode, Is.EqualTo((int)HttpStatusCode.OK), "Status code should be 200.");

        var value = okResult.Value;
        Assert.That(value, Is.Not.Null, "Response value should not be null.");

        var valueType = value!.GetType();
        var messageProp = valueType.GetProperty("message", BindingFlags.Instance | BindingFlags.Public);
        var timeUtcProp = valueType.GetProperty("timeUtc", BindingFlags.Instance | BindingFlags.Public);

        Assert.That(messageProp, Is.Not.Null, "Response should contain 'message' property.");
        Assert.That(timeUtcProp, Is.Not.Null, "Response should contain 'timeUtc' property.");

        var message = (string)messageProp!.GetValue(value)!;
        var timeUtc = (DateTime)timeUtcProp!.GetValue(value)!;

        Assert.That(message, Is.EqualTo("pong"), "Message should be 'pong'.");
        Assert.That(timeUtc.Kind, Is.EqualTo(DateTimeKind.Utc), "timeUtc should be in UTC.");
        Assert.That(timeUtc, Is.InRange(before, after), "timeUtc should be recent and within the call window.");
    }

    [TestCase(MockBehavior.Strict)]
    [TestCase(MockBehavior.Loose)]
    public void Constructor_WithValidLogger_CreatesInstance(MockBehavior behavior)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(behavior);

        // Act
        UtilityController controller = new UtilityController(loggerMock.Object);

        // Assert
        Assert.That(controller, Is.Not.Null);
    }

    [Test]
    public void Version_CurrentAssemblyMetadata_ReturnsOkWithExpectedFields()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>();
        var controller = new UtilityController(loggerMock.Object);

        var asm = Assembly.GetEntryAssembly() ?? typeof(global::Program).Assembly;
        var expectedVersion = asm.GetName().Version?.ToString() ?? "unknown";
        var expectedInformational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Act
        ActionResult<object> result = controller.Version();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        Assert.That(ok.Value, Is.Not.Null);

        var value = ok.Value!;
        var valueType = value.GetType();
        var versionProp = valueType.GetProperty("version");
        var informationalProp = valueType.GetProperty("informational");

        Assert.That(versionProp, Is.Not.Null, "Expected property 'version' not found on anonymous response object.");
        Assert.That(informationalProp, Is.Not.Null, "Expected property 'informational' not found on anonymous response object.");

        var actualVersion = (string?)versionProp!.GetValue(value);
        var actualInformational = (string?)informationalProp!.GetValue(value);

        Assert.That(actualVersion, Is.EqualTo(expectedVersion), "Version string mismatch.");
        Assert.That(actualInformational, Is.EqualTo(expectedInformational), "Informational version mismatch.");
    }

    [Test]
    public void Env_ReturnsOkWithCaseInsensitiveDictionary()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>();
        var controller = new UtilityController(loggerMock.Object);

        // Act
        ActionResult<IDictionary<string, string>> actionResult = controller.Env();

        // Assert
        Assert.That(actionResult.Result, Is.InstanceOf<OkObjectResult>(), "Expected OkObjectResult");
        var ok = (OkObjectResult)actionResult.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo((int)HttpStatusCode.OK), "Expected status code 200");
        Assert.That(ok.Value, Is.InstanceOf<Dictionary<string, string>>(), "Expected a Dictionary<string,string> payload");

        var dict = (Dictionary<string, string>)ok.Value!;
        Assert.That(dict.Comparer, Is.SameAs(StringComparer.OrdinalIgnoreCase), "Dictionary comparer should be OrdinalIgnoreCase");
    }

    [TestCaseSource(nameof(EnvValueCases))]
    public void Env_SetEnvironmentVariable_VariableIncludedWithExactValueAndCaseInsensitiveKey(string value)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>();
        var controller = new UtilityController(loggerMock.Object);
        string varNameUpper = "UTIL_ENV_TEST_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        string varNameLower = varNameUpper.ToLowerInvariant();

        string? previous = Environment.GetEnvironmentVariable(varNameUpper);
        Environment.SetEnvironmentVariable(varNameUpper, value);

        try
        {
            // Act
            ActionResult<IDictionary<string, string>> actionResult = controller.Env();

            // Assert
            Assert.That(actionResult.Result, Is.InstanceOf<OkObjectResult>(), "Expected OkObjectResult");
            var ok = (OkObjectResult)actionResult.Result!;
            Assert.That(ok.Value, Is.InstanceOf<Dictionary<string, string>>(), "Expected a Dictionary<string,string> payload");

            var dict = (Dictionary<string, string>)ok.Value!;
            Assert.That(dict.ContainsKey(varNameLower), Is.True, "Dictionary should contain the variable name irrespective of case");
            Assert.That(dict[varNameLower], Is.EqualTo(value), "Dictionary should preserve the exact environment variable value");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(varNameUpper, previous);
        }
    }

    private static IEnumerable<string> EnvValueCases()
    {
        yield return "value";
        yield return string.Empty;
        yield return " \t\r\n ";
        yield return "Üñïçødë🔥";
        yield return new string('a', 5000);
        yield return "line1\nline2\t\"quote\"\\backslash";
    }


    [Test]
    public void WhoAmI_ReturnsOkObjectResult_WithExpectedEnvironmentData()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var controller = new UtilityController(loggerMock.Object);

        var expectedHost = Dns.GetHostName();
        var expectedAddresses = Dns.GetHostAddresses(expectedHost).Select(a => a.ToString()).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        var expectedProcessId = Environment.ProcessId;

        // Act
        ActionResult<object> actionResult = controller.WhoAmI();

        // Assert
        Assert.That(actionResult, Is.Not.Null, "ActionResult should not be null.");
        var okResult = actionResult.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null, "Result should be OkObjectResult.");
        Assert.That(okResult!.StatusCode, Is.EqualTo(200), "HTTP status code should be 200.");

        var body = okResult.Value;
        Assert.That(body, Is.Not.Null, "Response body should not be null.");

        var bodyType = body!.GetType();
        var hostProp = bodyType.GetProperty("host", BindingFlags.Instance | BindingFlags.Public);
        var addressesProp = bodyType.GetProperty("addresses", BindingFlags.Instance | BindingFlags.Public);
        var processIdProp = bodyType.GetProperty("processId", BindingFlags.Instance | BindingFlags.Public);

        Assert.That(hostProp, Is.Not.Null, "Property 'host' should be present.");
        Assert.That(addressesProp, Is.Not.Null, "Property 'addresses' should be present.");
        Assert.That(processIdProp, Is.Not.Null, "Property 'processId' should be present.");

        var actualHost = (string)hostProp!.GetValue(body)!;
        var actualAddresses = (string[])addressesProp!.GetValue(body)!;
        var actualProcessId = (int)processIdProp!.GetValue(body)!;

        Assert.That(actualHost, Is.EqualTo(expectedHost), "Host name should match the system DNS host name.");

        var actualSorted = actualAddresses.OrderBy(s => s, StringComparer.Ordinal).ToArray();
        Assert.That(actualSorted, Is.EqualTo(expectedAddresses), "IP addresses should match the system's resolved addresses for the host.");

        Assert.That(actualProcessId, Is.EqualTo(expectedProcessId), "Process ID should match the current process ID.");
    }

    private static IEnumerable<object> EchoBodies()
    {
        // Strings
        yield return string.Empty;
        yield return " \t";
        yield return "special\u0000chars\u263A";

        // Integers
        yield return 0;
        yield return int.MinValue;
        yield return int.MaxValue;
        yield return -1;
        yield return 1;

        // Doubles
        yield return double.NaN;
        yield return double.PositiveInfinity;
        yield return double.NegativeInfinity;

        // Complex object (reference type)
        yield return new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
    }

    [TestCase(int.MinValue, 100)]
    [TestCase(99, 100)]
    [TestCase(100, 100)]
    [TestCase(404, 404)]
    [TestCase(599, 599)]
    [TestCase(600, 599)]
    [TestCase(int.MaxValue, 599)]
    public void Error_CodeClamping_ReturnsObjectResultWithExpectedStatusAndMessage(int input, int expectedClamped)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var controller = new UtilityController(loggerMock.Object);

        // Act
        ActionResult<object> actionResult = controller.Error(input);

        // Assert
        var objectResult = actionResult.Result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null, "Result should be an ObjectResult produced by StatusCode()");
        Assert.Multiple(() =>
        {
            Assert.That(objectResult!.StatusCode, Is.EqualTo(expectedClamped), "Status code should be clamped to [100, 599]");
            Assert.That(objectResult.Value, Is.Not.Null, "Response body should not be null");

            var value = objectResult.Value!;
            var errorProp = value.GetType().GetProperty("error", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            Assert.That(errorProp, Is.Not.Null, "Response body should contain an 'error' property");

            var errorMessage = errorProp!.GetValue(value)?.ToString();
            Assert.That(errorMessage, Is.EqualTo($"Generated error {expectedClamped}"), "Error message should reflect the clamped status code");
        });
    }

    private static UtilityController CreateControllerWithContext(out DefaultHttpContext httpContext)
    {
        var loggerMock = new Mock<ILogger<UtilityController>>();
        var controller = new UtilityController(loggerMock.Object);
        httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    [Test]
    public void Headers_NoHeaders_ReturnsEmptyDictionary()
    {
        // Arrange
        var controller = CreateControllerWithContext(out var httpContext);

        // Act
        var result = controller.Headers();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo(200));
        Assert.That(ok.Value, Is.InstanceOf<IDictionary<string, string>>());
        var headersDict = (IDictionary<string, string>)ok.Value!;
        Assert.That(headersDict, Is.Empty);
        Assert.That(httpContext.Request.Headers.Count, Is.EqualTo(0));
    }

    [TestCase("X-Custom-Header", "abc123")]
    [TestCase("X-Comma-Value", "value,with,commas")]
    public void Headers_SingleHeader_ReturnsExactProjection(string headerKey, string headerValue)
    {
        // Arrange
        var controller = CreateControllerWithContext(out var httpContext);
        httpContext.Request.Headers[headerKey] = new StringValues(headerValue);

        // Act
        var result = controller.Headers();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo(200));
        Assert.That(ok.Value, Is.InstanceOf<IDictionary<string, string>>());
        var headersDict = (IDictionary<string, string>)ok.Value!;
        Assert.That(headersDict.Count, Is.EqualTo(1), "Expected exactly one header in the projection.");
        Assert.That(headersDict.ContainsKey(headerKey), Is.True, "Header key casing should be preserved from insertion.");
        Assert.That(headersDict[headerKey], Is.EqualTo(headerValue));
    }

    [Test]
    public void Headers_HeaderWithMultipleValues_ValuesJoinedAsString()
    {
        // Arrange
        var controller = CreateControllerWithContext(out var httpContext);
        var headerKey = "Accept";
        var values = new[] { "application/json", "text/plain" };
        httpContext.Request.Headers[headerKey] = new StringValues(values);
        var expectedJoined = new StringValues(values).ToString();

        // Act
        var result = controller.Headers();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo(200));
        Assert.That(ok.Value, Is.InstanceOf<IDictionary<string, string>>());
        var headersDict = (IDictionary<string, string>)ok.Value!;
        Assert.That(headersDict.Count, Is.EqualTo(1));
        Assert.That(headersDict.ContainsKey(headerKey), Is.True);
        Assert.That(headersDict[headerKey], Is.EqualTo(expectedJoined), "Multi-value header should be joined consistently with StringValues.ToString().");
    }

    [Test]
    public void Headers_MultipleHeaders_AllProjected()
    {
        // Arrange
        var controller = CreateControllerWithContext(out var httpContext);
        httpContext.Request.Headers["X-TraceId"] = new StringValues("trace-001");
        httpContext.Request.Headers["X-Features"] = new StringValues(new[] { "A", "B" });
        var expectedFeatures = new StringValues(new[] { "A", "B" }).ToString();

        // Act
        var result = controller.Headers();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo(200));
        Assert.That(ok.Value, Is.InstanceOf<IDictionary<string, string>>());
        var headersDict = (IDictionary<string, string>)ok.Value!;
        Assert.That(headersDict.Count, Is.EqualTo(2));
        Assert.That(headersDict["X-TraceId"], Is.EqualTo("trace-001"));
        Assert.That(headersDict["X-Features"], Is.EqualTo(expectedFeatures));
    }

    [Test]
    public void Allocate_SequenceOfValues_ClampsLowerBoundAndAccumulatesCorrectly()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Loose);
        var controller = new UtilityController(loggerMock.Object);
        var inputs = new[] { int.MinValue, -1, 0, 1, 2 };

        int? previousChunks = null;
        int? previousTotalMb = null;

        foreach (var input in inputs)
        {
            // Act
            var actionResult = controller.Allocate(input);

            // Assert
            Assert.That(actionResult, Is.Not.Null, "ActionResult should not be null.");
            var ok = actionResult.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null, "Result should be OkObjectResult.");
            Assert.That(ok!.StatusCode, Is.EqualTo(StatusCodes.Status200OK), "Status code should be 200 OK.");

            var payload = ok.Value!;
            int allocatedMb = GetIntProperty(payload, "allocatedMb");
            int chunks = GetIntProperty(payload, "chunks");
            int totalMb = GetIntProperty(payload, "totalMb");

            int expectedAllocated = Math.Max(1, input);
            Assert.That(allocatedMb, Is.EqualTo(expectedAllocated), "allocatedMb should reflect clamping for lower bound.");

            if (previousChunks.HasValue && previousTotalMb.HasValue)
            {
                Assert.That(chunks, Is.EqualTo(previousChunks.Value + 1), "chunks should increase by exactly 1 per allocation.");
                Assert.That(totalMb, Is.EqualTo(previousTotalMb.Value + expectedAllocated), "totalMb should increase by the allocated amount.");
            }

            previousChunks = chunks;
            previousTotalMb = totalMb;
        }
    }

    private static int GetIntProperty(object obj, string propertyName)
    {
        var type = obj.GetType();
        var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(prop, Is.Not.Null, $"Response is missing expected property '{propertyName}'.");
        var value = prop!.GetValue(obj);
        Assert.That(value, Is.Not.Null, $"Property '{propertyName}' should not be null.");
        return (int)value!;
    }

    [Test]
    [TestCase(new int[] { }, 0)]
    [TestCase(new int[] { 1 }, 1)]
    [TestCase(new int[] { 1, 2 }, 3)]
    public void ClearAllocations_WithPreExistingAllocations_ReturnsCorrectFreedMbAndClears(int[] allocationsMb, int expectedFreedMb)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Loose);
        var controller = new UtilityController(loggerMock.Object);

        // Ensure a clean slate to avoid cross-test interference.
        _ = controller.ClearAllocations();

        // Allocate requested chunks (Arrange state via Allocate; not asserting its behavior).
        foreach (var mb in allocationsMb)
        {
            _ = controller.Allocate(mb);
        }

        // Act
        var result = controller.ClearAllocations();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>(), "Expected OkObjectResult.");
        var ok = (OkObjectResult)result.Result!;
        dynamic payload = ok.Value!;
        Assert.That((bool)payload.cleared, Is.True, "Expected 'cleared' to be true.");
        Assert.That((int)payload.freedMb, Is.EqualTo(expectedFreedMb), "Freed MB should equal the sum of allocated MB.");

        // Arrange-Act-Assert (post-condition):
        // Calling ClearAllocations again should now free 0 MB, proving state was cleared.
        var resultAfterClear = controller.ClearAllocations();
        Assert.That(resultAfterClear.Result, Is.InstanceOf<OkObjectResult>(), "Expected OkObjectResult on second clear.");
        var ok2 = (OkObjectResult)resultAfterClear.Result!;
        dynamic payload2 = ok2.Value!;
        Assert.That((bool)payload2.cleared, Is.True, "Expected 'cleared' to be true on second clear.");
        Assert.That((int)payload2.freedMb, Is.EqualTo(0), "Freed MB should be 0 after allocations were cleared.");
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void Files_NullOrEmptyPath_ReturnsDirectoryListing(string? inputPath)
    {
        // Arrange
        string tempRoot = CreateTempDir();
        try
        {
            // Create directory and files
            string subDir = Path.Combine(tempRoot, "sub");
            Directory.CreateDirectory(subDir);

            string specialDir = Path.Combine(tempRoot, "子");
            Directory.CreateDirectory(specialDir);

            string file1 = Path.Combine(tempRoot, "file.txt");
            File.WriteAllText(file1, "hello");

            string file2 = Path.Combine(tempRoot, "spécial@file.txt");
            File.WriteAllText(file2, "abc");

            using var _ = SetDataRoot(tempRoot);
            var controller = CreateController();

            // Act
            var actionResult = controller.Files(inputPath);

            // Assert
            Assert.That(actionResult.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult.Result!;
            Assert.That(ok.Value, Is.Not.Null);

            var value = ok.Value!;
            var fullRoot = (string)GetProperty(value, "root")!;
            var target = (string)GetProperty(value, "target")!;
            Assert.That(fullRoot, Is.EqualTo(Path.GetFullPath(tempRoot)));
            Assert.That(target, Is.EqualTo(tempRoot)); // target equals root when path is null or empty

            var entries = (IEnumerable)GetProperty(value, "entries")!;
            // Validate presence and attributes of directory and file entries (order-independent)
            var subDirEntry = FindEntry(entries, "sub");
            Assert.That(subDirEntry, Is.Not.Null);
            Assert.That((string?)GetProperty(subDirEntry!, "type"), Is.EqualTo("dir"));
            Assert.That(GetProperty(subDirEntry!, "size"), Is.Null);
            Assert.That((string?)GetProperty(subDirEntry!, "path"), Is.EqualTo("sub"));

            var specialDirEntry = FindEntry(entries, "子");
            Assert.That(specialDirEntry, Is.Not.Null);
            Assert.That((string?)GetProperty(specialDirEntry!, "type"), Is.EqualTo("dir"));
            Assert.That(GetProperty(specialDirEntry!, "size"), Is.Null);
            Assert.That((string?)GetProperty(specialDirEntry!, "path"), Is.EqualTo("子"));

            var file1Entry = FindEntry(entries, "file.txt");
            Assert.That(file1Entry, Is.Not.Null);
            Assert.That((string?)GetProperty(file1Entry!, "type"), Is.EqualTo("file"));
            Assert.That((long?)GetProperty(file1Entry!, "size"), Is.EqualTo(new FileInfo(file1).Length));
            Assert.That((string?)GetProperty(file1Entry!, "path"), Is.EqualTo("file.txt"));

            var file2Entry = FindEntry(entries, "spécial@file.txt");
            Assert.That(file2Entry, Is.Not.Null);
            Assert.That((string?)GetProperty(file2Entry!, "type"), Is.EqualTo("file"));
            Assert.That((long?)GetProperty(file2Entry!, "size"), Is.EqualTo(new FileInfo(file2).Length));
            Assert.That((string?)GetProperty(file2Entry!, "path"), Is.EqualTo("spécial@file.txt"));
        }
        finally
        {
            SafeDeleteDir(tempRoot);
        }
    }

    [Test]
    public void Files_PathTraversalOutsideRoot_ReturnsBadRequest()
    {
        // Arrange
        string tempRoot = CreateTempDir();
        try
        {
            using var _ = SetDataRoot(tempRoot);
            var controller = CreateController();

            // Act
            var actionResult = controller.Files("..");

            // Assert
            Assert.That(actionResult.Result, Is.InstanceOf<BadRequestObjectResult>());
            var bad = (BadRequestObjectResult)actionResult.Result!;
            Assert.That(bad.Value, Is.Not.Null);
            var value = bad.Value!;
            Assert.That((string?)GetProperty(value, "error"), Is.EqualTo("Path is outside of allowed root"));
        }
        finally
        {
            SafeDeleteDir(tempRoot);
        }
    }

    [Test]
    public void Files_FileTarget_ReturnsSizeAndContent()
    {
        // Arrange
        string tempRoot = CreateTempDir();
        try
        {
            string fileName = "content.txt";
            string filePath = Path.Combine(tempRoot, fileName);
            string content = "abc123";
            File.WriteAllText(filePath, content);

            using var _ = SetDataRoot(tempRoot);
            var controller = CreateController();

            // Act
            var actionResult = controller.Files(fileName);

            // Assert
            Assert.That(actionResult.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult.Result!;
            var value = ok.Value!;
            var fullRoot = (string)GetProperty(value, "root")!;
            var target = (string)GetProperty(value, "target")!;
            var size = (long)GetProperty(value, "size")!;
            var returnedContent = (string)GetProperty(value, "content")!;

            Assert.That(fullRoot, Is.EqualTo(Path.GetFullPath(tempRoot)));
            Assert.That(target, Is.EqualTo(Path.GetFullPath(filePath)));
            Assert.That(size, Is.EqualTo(new FileInfo(filePath).Length));
            Assert.That(returnedContent, Is.EqualTo(content));
        }
        finally
        {
            SafeDeleteDir(tempRoot);
        }
    }

    [Test]
    public void Files_FileTarget_TooLargeContent_TruncatedTo16000PlusEllipsis()
    {
        // Arrange
        string tempRoot = CreateTempDir();
        try
        {
            string fileName = "large.txt";
            string filePath = Path.Combine(tempRoot, fileName);
            string original = new string('a', 16050);
            File.WriteAllText(filePath, original);

            using var _ = SetDataRoot(tempRoot);
            var controller = CreateController();

            // Act
            var actionResult = controller.Files(fileName);

            // Assert
            Assert.That(actionResult.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult.Result!;
            var value = ok.Value!;
            var size = (long)GetProperty(value, "size")!;
            var content = (string)GetProperty(value, "content")!;

            Assert.That(size, Is.EqualTo(new FileInfo(filePath).Length));
            Assert.That(content.Length, Is.EqualTo(16003));
            Assert.That(content.EndsWith("...", StringComparison.Ordinal), Is.True);
            Assert.That(content.StartsWith(original[..16000], StringComparison.Ordinal), Is.True);
        }
        finally
        {
            SafeDeleteDir(tempRoot);
        }
    }

    [Test]
    public void Files_PathWithInvalidChar_ThrowsArgumentException()
    {
        // Arrange
        string tempRoot = CreateTempDir();
        try
        {
            using var _ = SetDataRoot(tempRoot);
            var controller = CreateController();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => controller.Files("bad\0path"));
        }
        finally
        {
            SafeDeleteDir(tempRoot);
        }
    }

    // ----------------- Helpers -----------------

    private static UtilityController CreateController()
    {
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Loose);
        return new UtilityController(loggerMock.Object);
    }

    private static string CreateTempDir()
    {
        string path = Path.Combine(Path.GetTempPath(), "UtilityControllerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void SafeDeleteDir(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; ignore failures to keep tests stable.
        }
    }

    private static object? GetProperty(object obj, string name)
    {
        var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetValue(obj);
    }

    private static object? FindEntry(IEnumerable entries, string name)
    {
        foreach (var e in entries)
        {
            var entryName = (string?)GetProperty(e!, "name");
            if (string.Equals(entryName, name, StringComparison.Ordinal))
            {
                return e;
            }
        }
        return null;
    }

    private static EnvVarScope SetDataRoot(string value) => new EnvVarScope("DATA_ROOT", value);

    private sealed class EnvVarScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;

        public EnvVarScope(string name, string? value)
        {
            _name = name;
            _previous = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _previous);
        }
    }

    [TestCase(0)]
    [TestCase(10)]
    public async Task Delay_ValidMs_ReturnsOkWithElapsedAtLeastRequested(int ms)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var sut = new UtilityController(loggerMock.Object);

        // Act
        ActionResult<object> actionResult = await sut.Delay(ms, CancellationToken.None);

        // Assert
        Assert.That(actionResult, Is.Not.Null, "ActionResult should not be null");
        Assert.That(actionResult.Result, Is.TypeOf<OkObjectResult>(), "Result should be OkObjectResult");
        var ok = (OkObjectResult)actionResult.Result!;
        Assert.That(ok.Value, Is.Not.Null, "OkObjectResult.Value should not be null");

        var valueType = ok.Value!.GetType();
        var requestedMsProp = valueType.GetProperty("requestedMs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var elapsedMsProp = valueType.GetProperty("elapsedMs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(requestedMsProp, Is.Not.Null, "Payload should contain 'requestedMs' property");
        Assert.That(elapsedMsProp, Is.Not.Null, "Payload should contain 'elapsedMs' property");

        var requestedMs = (int)(requestedMsProp!.GetValue(ok.Value, null) ?? 0);
        var elapsedMs = (long)(elapsedMsProp!.GetValue(ok.Value, null) ?? 0L);

        Assert.That(requestedMs, Is.EqualTo(ms), "requestedMs should echo the input milliseconds");
        Assert.That(elapsedMs, Is.GreaterThanOrEqualTo(ms), "elapsedMs should be at least the requested delay");
    }

    [TestCase(int.MinValue)]
    [TestCase(-2)]
    public void Delay_InvalidNegativeMs_ThrowsArgumentOutOfRangeException(int ms)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var sut = new UtilityController(loggerMock.Object);

        // Act + Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await sut.Delay(ms, CancellationToken.None));
    }

    [Test]
    public void Delay_InfiniteMsWithPreCanceledToken_ThrowsTaskCanceledException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var sut = new UtilityController(loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act + Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () => await sut.Delay(-1, cts.Token));
    }

    [Test]
    public void Delay_LargeMsCanceled_ThrowsTaskCanceledException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var sut = new UtilityController(loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);

        // Act + Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () => await sut.Delay(int.MaxValue, cts.Token));
    }

    [Test]
    public void Ready_Always_ReturnsOkWithStatusReady()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UtilityController>>(MockBehavior.Strict);
        var controller = new UtilityController(loggerMock.Object);

        // Act
        var actionResult = controller.Ready();

        // Assert
        Assert.That(actionResult, Is.Not.Null);
        var okResult = actionResult.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null, "Expected OkObjectResult.");
        Assert.That(okResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var payload = okResult.Value;
        Assert.That(payload, Is.Not.Null, "Expected a non-null payload.");
        var statusProp = payload!.GetType().GetProperty("status", BindingFlags.Public | BindingFlags.Instance);
        Assert.That(statusProp, Is.Not.Null, "Payload should contain a 'status' property.");
        var statusValue = statusProp!.GetValue(payload) as string;
        Assert.That(statusValue, Is.EqualTo("Ready"));
    }
}