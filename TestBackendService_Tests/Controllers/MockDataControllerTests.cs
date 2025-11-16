using Microsoft.AspNetCore.Mvc;
using Moq;
using TestBackendService.Models;
using TestBackendService.Services;

namespace TestBackendService.Controllers.UnitTests;

public class MockDataControllerTests
{
    [Test]
    [TestCase(0, null)]
    [TestCase(1, 0)]
    [TestCase(10, null)]
    [TestCase(-1, -1)]
    [TestCase(int.MinValue, int.MinValue)]
    [TestCase(int.MaxValue, int.MaxValue)]
    public void GetUsers_VariousInputs_ReturnsOkAndPassesThroughList(int count, int? seed)
    {
        // Arrange
        var serviceMock = new Mock<IMockDataService>(MockBehavior.Strict);
        var expectedUsers = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), FirstName = "A" },
                new UserDto { Id = Guid.NewGuid(), FirstName = "B" },
            };
        serviceMock.Setup(s => s.GetUsers(count, seed)).Returns(expectedUsers);

        var controller = new MockDataController(serviceMock.Object);

        // Act
        ActionResult<IEnumerable<UserDto>> result = controller.GetUsers(count, seed);

        // Assert
        serviceMock.Verify(s => s.GetUsers(count, seed), Times.Once);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null, "Result should be OkObjectResult");
        Assert.That(okResult?.Value, Is.SameAs(expectedUsers), "Controller should return the exact list instance provided by the service");
    }

    [Test]
    public void GetUsers_ServiceReturnsEmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        const int count = 0;
        int? seed = null;
        var serviceMock = new Mock<IMockDataService>(MockBehavior.Strict);
        var expectedUsers = new List<UserDto>();
        serviceMock.Setup(s => s.GetUsers(count, seed)).Returns(expectedUsers);

        var controller = new MockDataController(serviceMock.Object);

        // Act
        ActionResult<IEnumerable<UserDto>> result = controller.GetUsers(count, seed);

        // Assert
        serviceMock.Verify(s => s.GetUsers(count, seed), Times.Once);

        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null, "Result should be OkObjectResult");
        Assert.That(okResult?.Value, Is.SameAs(expectedUsers), "Controller should return the exact empty list instance provided by the service");
        Assert.That(((IReadOnlyList<UserDto>)okResult!.Value!).Count, Is.EqualTo(0), "List should be empty");
    }

    private static IEnumerable<TestCaseData> GetUsers_ServiceThrows_TestCases()
    {
        yield return new TestCaseData(0, null, new InvalidOperationException("boom"))
            .SetName("GetUsers_ServiceThrowsInvalidOperation_ExceptionBubblesUp");
        yield return new TestCaseData(-1, -5, new ArgumentException("bad"))
            .SetName("GetUsers_ServiceThrowsArgument_ExceptionBubblesUp");
        yield return new TestCaseData(int.MaxValue, int.MinValue, new Exception("generic"))
            .SetName("GetUsers_ServiceThrowsGeneric_ExceptionBubblesUp");
    }

    [TestCase(0, null, 0)]
    [TestCase(1, 0, 1)]
    [TestCase(-1, -1, 2)]
    [TestCase(int.MaxValue, int.MaxValue, 3)]
    [TestCase(int.MinValue, int.MinValue, 1)]
    public void GetProducts_VariousInputs_ReturnsOkWithServiceResult(int count, int? seed, int listSize)
    {
        // Arrange
        var products = CreateProducts(listSize);
        var serviceMock = new Mock<IMockDataService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.GetProducts(count, seed))
            .Returns(products);

        var controller = new MockDataController(serviceMock.Object);

        // Act
        ActionResult<IEnumerable<ProductDto>> result = controller.GetProducts(count, seed);

        // Assert
        serviceMock.Verify(s => s.GetProducts(count, seed), Times.Once);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>(), "Expected OkObjectResult.");
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo(200), "Expected HTTP 200.");
        Assert.That(ok.Value, Is.SameAs(products), "Controller should return the same instance from the service.");
    }

    [Test]
    public void GetProducts_ServiceThrows_ExceptionIsPropagated()
    {
        // Arrange
        const int count = 5;
        int? seed = 123;
        var expected = new InvalidOperationException("Service failure");
        var serviceMock = new Mock<IMockDataService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.GetProducts(count, seed))
            .Throws(expected);

        var controller = new MockDataController(serviceMock.Object);

        // Act + Assert
        var ex = Assert.Throws<InvalidOperationException>(() => controller.GetProducts(count, seed));
        Assert.That(ex, Is.SameAs(expected), "The exact exception instance should be propagated.");
        serviceMock.Verify(s => s.GetProducts(count, seed), Times.Once);
    }

    private static IReadOnlyList<ProductDto> CreateProducts(int count)
    {
        var list = new List<ProductDto>(Math.Max(0, count));
        for (int i = 0; i < count; i++)
        {
            list.Add(new ProductDto
            {
                Id = Guid.NewGuid(),
                Sku = $"SKU-{i}",
                Name = $"Name {i}",
                Category = $"Category {i}",
                Price = i + 0.99m,
                Description = $"Desc {i}",
                Color = $"Color {i}"
            });
        }
        return list;
    }

    [Test]
    [TestCase(null)]
    [TestCase(int.MinValue)]
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(int.MaxValue)]
    public void GetCompany_SeedValues_ReturnsOkAndPassesSeed(int? seed)
    {
        // Arrange
        var expectedAddress = new AddressDto("123 Test St", "Testville", "TS", "00000", "Testland");
        var expectedCompany = new CompanyDto("TestCorp", "We test things", "testing-bs", "555-0000", expectedAddress);

        var serviceMock = new Mock<IMockDataService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.GetCompany(It.Is<int?>(v => v == seed)))
            .Returns(expectedCompany);

        var controller = new MockDataController(serviceMock.Object);

        // Act
        ActionResult<CompanyDto> result = controller.GetCompany(seed);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.StatusCode, Is.EqualTo(200));
        Assert.That(ok.Value, Is.SameAs(expectedCompany));
        Assert.That(result.Value, Is.Null);

        serviceMock.Verify(s => s.GetCompany(It.Is<int?>(v => v == seed)), Times.Once);
        serviceMock.VerifyNoOtherCalls();
    }

    [Test]
    [TestCase(null)]
    [TestCase(123)]
    public void GetCompany_ServiceThrows_ExceptionIsPropagated(int? seed)
    {
        // Arrange
        var serviceMock = new Mock<IMockDataService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.GetCompany(It.Is<int?>(v => v == seed)))
            .Throws(new InvalidOperationException("Service failure"));

        var controller = new MockDataController(serviceMock.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => controller.GetCompany(seed));
        serviceMock.Verify(s => s.GetCompany(It.Is<int?>(v => v == seed)), Times.Once);
        serviceMock.VerifyNoOtherCalls();
    }

    [Test]
    public void Constructor_ValidService_DoesNotThrow()
    {
        // Arrange
        var serviceMock = new Mock<IMockDataService>(MockBehavior.Strict);

        // Act
        MockDataController? controller = null;
        Assert.DoesNotThrow(() => controller = new MockDataController(serviceMock.Object));

        // Assert
        Assert.That(controller, Is.Not.Null);
        Assert.That(controller, Is.InstanceOf<MockDataController>());
    }
}