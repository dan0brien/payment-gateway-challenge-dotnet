namespace PaymentGateway.Api.Models.Responses;

public class PostPaymentResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public int CardNumberLastFour { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; }
    public int Amount { get; set; }

    public PostPaymentResponse(string status, Requests.PostPaymentRequest request) {
        Id = Guid.NewGuid();
        Status = status;
        CardNumberLastFour = GetCardNumberLastFour(request.CardNumber);
        ExpiryMonth = request.ExpiryMonth;
        ExpiryYear = request.ExpiryYear;
        Currency = request.Currency;
        Amount = request.Amount;
    }

    private int GetCardNumberLastFour(long cardNumber){
        string cardString = cardNumber.ToString();
        int lastFourDigits = int.Parse(cardString.Substring(cardString.Length - 4));
        return lastFourDigits;
    }

}