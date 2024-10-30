namespace PaymentGateway.Api.Models.Requests;

using System.Collections;
using System.Globalization;
using System.Text.Json.Serialization;

public class PostPaymentRequest
{
    public long CardNumber { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public required string Currency { get; set; }
    public int Amount { get; set; }
    public int Cvv { get; set; }
}
public class PostPaymentRequestJson
{
    [JsonPropertyName("card_number")]
    public long CardNumber { get; set; }
    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; }
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
    [JsonPropertyName("cvv")]
    public int Cvv { get; set; }

    public PostPaymentRequestJson(long cardNumber, string expiryDate, string currency, int amount, int cvv) {
        if (cardNumber.ToString().Length < 14 ||  cardNumber.ToString().Length > 19){
            throw new ArgumentException("Card Number must be between 14 and 19 characters and contain only numeric characters");
        }
        if (!IsFutureDate(expiryDate)){
            throw new ArgumentException("The card's expiry date must be in the future.");
        }
        if (!supportedCurrencies.Contains(currency) ){
            throw new ArgumentException("That currency is not supported, supported currencies include", string.Join(", ", supportedCurrencies));
        }
        if (cvv.ToString().Length < 3 || cvv.ToString().Length > 5 ){
            throw new ArgumentException("Cvv must be between 3 and 4 characters and contain only numeric characters");
        }
        CardNumber = cardNumber;
        ExpiryDate = expiryDate;
        Currency = currency;
        Amount = amount;
        Cvv = cvv;
    }

    public static List<string> supportedCurrencies = ["GBP", "EUR", "USD"];
    
    public static bool IsFutureDate(string date)
        {
        DateTime parsedDate;
        bool isValid = DateTime.TryParseExact(
            date,
            "MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsedDate);

        if (!isValid)
        {
            throw new ArgumentException("Invalid date format. Expected MM/yyyy.");
        }
        return parsedDate > DateTime.Now;
    
        
    }
    public static PostPaymentRequestJson ConvertRequest(PostPaymentRequest request) {
        string expiryMonthString = request.ExpiryMonth.ToString();
        if (expiryMonthString.Length < 2){
            expiryMonthString = "0" + expiryMonthString;
        }
        string expiryDate = expiryMonthString + "/" + request.ExpiryYear.ToString();

        return new PostPaymentRequestJson(request.CardNumber, expiryDate, request.Currency, request.Amount, request.Cvv);

    }
}

public class PostPaymentResponseServer
{
    public bool? authorized { get; set; }

    public string? authorization_code { get; set; }
    public string? errorMessage { get; set;}
    public PaymentStatus GetPaymentStatus() =>
        authorized switch
        {
            true => PaymentStatus.Authorized,
            false => PaymentStatus.Declined,
            _ => PaymentStatus.Rejected,
        };

}
