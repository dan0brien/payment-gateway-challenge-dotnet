## Assumptions
Only needs to work for a single merchant at this stage.
Transactions that make it to the bank simulator, but are rejected, receieve a rejected response rather than declined.
Rejected responses are not stored for future lookup.


## Design Considerations
Code should be readable.
Mocking of components for UTs should be straightforward.
The business logic is straightforward, no need to break into numerous files.


## Testing
Unit tests are found in `test\PaymentGateway.Api.Tests\PaymentsControllerTests.cs` and can be run individually or all together from the VSCode Test explorer.
Alternatively tests can be run from the command line using `dotnet test .\test\PaymentGateway.Api.Tests\PaymentGateway.Api.Tests.csproj` from the top level of the directory.
NB For the following testing, the banking simulator must be available - this can be achieved by running 'docker-compose up' in the top level directory.
Manual Testing can be performed via swagger API, which can be accessed by building and running the project.
Example JSON for POST commands that will trigger an authorized/denied response from the bank simulator are found at the top level of this directory.

# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

