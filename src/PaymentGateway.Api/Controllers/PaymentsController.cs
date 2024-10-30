using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models.Requests;
using System.Text;
using System.Text.Json;
using PaymentGateway.Api.Models.Interfaces;
namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger _logger;

    public PaymentsController(IPaymentsRepository paymentsRepository, IHttpClientFactory factory, ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _factory = factory;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        _logger.LogInformation("Looking for Guid {Id} in payment repository", id);
        var payment = _paymentsRepository.Get(id);
        return payment is null ? NotFound() : new OkObjectResult(payment);
    }
    
    [HttpPost()]
    public async Task<ActionResult<PostPaymentResponse?>> PostPaymentAsync(PostPaymentRequest paymentRequest) // [FromServices] IHttpClientFactory factory
    {
        // Attempt to parse object received to form required by banking simulator
        _logger.LogInformation("Entry point for PostPaymentAsync.");
        PostPaymentRequestJson paymentRequestJson;
        try
        {
            _logger.LogInformation("Attempt to convert paymentRequest to required JSON format.");
            paymentRequestJson = PostPaymentRequestJson.ConvertRequest(paymentRequest);
        }
        catch (Exception)
        {
            string statusRejected = Models.PaymentStatus.Rejected.ToString();
            var failedPaymentResponse = new PostPaymentResponse(statusRejected, paymentRequest);
            return new BadRequestObjectResult(failedPaymentResponse);
        }

        string myJson = JsonSerializer.Serialize(paymentRequestJson);
        var s_httpClient = _factory.CreateClient();
        s_httpClient.BaseAddress = new Uri("http://localhost:8080");
        string responseString;
        try 
        {
             _logger.LogInformation("Send POST to banking simulator.");
            var response = await s_httpClient.PostAsync($"/payments", new StringContent(myJson, Encoding.UTF8, "application/json"));
            responseString = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Banking service unreachable" );
        }

        // If we are unable to parse what was returned from the simulator, or it has not been accepted or declined by the
        // simulator then return failure code to merchant.
        PostPaymentResponseServer? parsed = JsonSerializer.Deserialize<PostPaymentResponseServer>(responseString);

        if (parsed is null || parsed.authorized is null){
             _logger.LogWarning("Failed to receive authorization response from Banking Service");
            string statusRejected = Models.PaymentStatus.Rejected.ToString();
            PostPaymentResponse failedPaymentResponse = new PostPaymentResponse(statusRejected, paymentRequest);
            return new BadRequestObjectResult(failedPaymentResponse);
        }
        // Else return 200 OK to merchant, with the status detailing whether the bank accepted or declined the transaction
        string status = parsed.GetPaymentStatus().ToString(); // throw an error here?
        PostPaymentResponse postPaymentResponse = new PostPaymentResponse(status, paymentRequest);

         _paymentsRepository.Add(postPaymentResponse);

        return new OkObjectResult(postPaymentResponse);
    }
}