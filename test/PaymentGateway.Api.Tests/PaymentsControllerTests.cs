using System.Net;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Microsoft.AspNetCore.Http;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IHttpClientFactory _factory;
    
    private readonly Mock<ILogger<PaymentsController>> mockLogger;
    
    private readonly Random _random = new();
    
    public PaymentsControllerTests()
    {
        _factory = A.Fake<IHttpClientFactory>();
        _paymentsRepository = A.Fake<IPaymentsRepository>();
        mockLogger = new Mock<ILogger<PaymentsController>>();
    }

    [Fact]
    public async Task GetPayment_ValidId_Success()
    {
        // Arrange
        var paymentRequest = new PostPaymentRequest {
            CardNumber =  123456789101112,
            ExpiryYear = _random.Next(2025, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Currency = "GBP",
            Amount = 50,
            Cvv = 112
        };

        var dummyPayment = new PostPaymentResponse(Models.PaymentStatus.Authorized.ToString(), paymentRequest);

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(dummyPayment);
        ILogger<PaymentsController> logger = mockLogger.Object;

        var controller = new PaymentsController(paymentsRepository, _factory, logger);

        // Act 
        var result = await controller.GetPaymentAsync(dummyPayment.Id);

        // Assert
        if (result.Result is OkObjectResult okResult)
        {
            PostPaymentResponse? response = okResult.Value as PostPaymentResponse;
            if (response != null)
            {
                Assert.Equal(1112, response.CardNumberLastFour);
            }
        }
        else 
        {
            Assert.Equal(1,2);
        }
    }

    [Fact]
    public async Task GetPayment_UnknownId_Fail()
    {
        // Arrange
        ILogger<PaymentsController> logger = mockLogger.Object;
        var controller = new PaymentsController(_paymentsRepository, _factory, logger);
        
        // Act
        var result = await controller.GetPaymentAsync(Guid.NewGuid());
        
        // Assert
        if (result.Result is NotFoundResult notFound)
        {
            Assert.Equal(notFound.StatusCode, StatusCodes.Status404NotFound);
        }
        else 
        {
            Assert.Equal(1,2);
        }
    }

    [Fact]
    public async Task PostPayment_ValidRequest_Success()
    {
        // Arrange
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.SetupRequest(
        HttpMethod.Post,
        "http://localhost:8080/payments")
        .ReturnsJsonResponse(new {authorized = true, authorization_code = "0bb07405-6d44-4b50-a14f-7ae0bef56477" });

        var httpClientfactory = handlerMock.CreateClientFactory();
        ILogger<PaymentsController> logger = mockLogger.Object;
        var controller = new PaymentsController(_paymentsRepository, httpClientfactory, logger);
        var request = new PostPaymentRequest {
            CardNumber =  123456789101112,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 50,
            Cvv = 112
        };        
        // Act
        var result = await controller.PostPaymentAsync(request);
        if (result.Result is OkObjectResult okResult)
        {
            PostPaymentResponse? response = okResult.Value as PostPaymentResponse;
            if (response != null)
            {
                Assert.Equal(1112, response.CardNumberLastFour);
            }
        }
        else 
        {
            Assert.Equal(1,2);
        }
    }

    [Fact]
    public async Task PostPayment_ValidRequest_Declined()
    {
        // Arrange
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.SetupRequest(
        HttpMethod.Post,
        "http://localhost:8080/payments")
        .ReturnsJsonResponse(new {authorized = false});

        var httpClientfactory = handlerMock.CreateClientFactory();
        ILogger<PaymentsController> logger = mockLogger.Object;
        var controller = new PaymentsController(_paymentsRepository, httpClientfactory, logger);
        var request = new PostPaymentRequest {
            CardNumber =  123456789101112,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 50,
            Cvv = 112
        };        
        // Act
        var result = await controller.PostPaymentAsync(request);
        
        // Assert
        if (result.Result is OkObjectResult badResult)
        {
            PostPaymentResponse? response = badResult.Value as PostPaymentResponse;
            if (response != null)
            {
                Assert.Equal(Models.PaymentStatus.Declined.ToString(), response.Status);
            }
        }
        else 
        {
            Assert.Equal(1,2);
        }
    }

    [Fact]
    public async Task PostPayment_InvalidRequest_Rejected()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.SetupRequest(
        HttpMethod.Post,
        "http://localhost:8080/payments")
        .ReturnsJsonResponse(new {errorMessage = "The request supplied is not supported"});

        var httpClientfactory = handlerMock.CreateClientFactory();
        ILogger<PaymentsController> logger = mockLogger.Object;
        var controller = new PaymentsController(_paymentsRepository, httpClientfactory, logger);
        var request = new PostPaymentRequest {
            CardNumber =  123456789101112,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 50,
            Cvv = 112
        };        
        // Act
        var result = await controller.PostPaymentAsync(request);

        // Assert
        if (result.Result is BadRequestObjectResult badResult)
        {
            PostPaymentResponse? response = badResult.Value as PostPaymentResponse;
            if (response != null)
            {
                Assert.Equal(Models.PaymentStatus.Rejected.ToString(), response.Status);
            }
        }
        else 
        {
            Assert.Equal(1,2);
        }
    }

    [Fact]

    public async Task PostPayment_BankingServiceUnavailable_Rejected()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.SetupRequest(
        HttpMethod.Post,
        "http://localhost:8080/payments")
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
           {
               Content = new StringContent("{\"message\":\"Internal Server Error\"}", Encoding.UTF8, "application/json")
           });


        var httpClientfactory = handlerMock.CreateClientFactory();
        ILogger<PaymentsController> logger = mockLogger.Object;
        var controller = new PaymentsController(_paymentsRepository, httpClientfactory, logger);
        var request = new PostPaymentRequest {
            CardNumber =  123456789101112,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 50,
            Cvv = 112
        };
   
        // Act
        var result = await controller.PostPaymentAsync(request);

        // Assert
        if (result.Result is BadRequestObjectResult badResult)
        {
            PostPaymentResponse? response = badResult.Value as PostPaymentResponse;
            if (response != null)
            {
                Assert.Equal(Models.PaymentStatus.Rejected.ToString(), response.Status);
            }
        }
        else 
        {
            Assert.Equal(1,2);
        }
    }
}