using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Models.Interfaces;

public interface IPaymentsRepository
{
    void Add(PostPaymentResponse paymentResponse);
    PostPaymentResponse? Get(Guid id);
}