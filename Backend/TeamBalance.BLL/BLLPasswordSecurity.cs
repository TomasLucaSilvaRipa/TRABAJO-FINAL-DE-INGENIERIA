using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLPasswordSecurity
{
    private readonly PasswordSecurityWebService _passwordSecurityWebService;

    public BLLPasswordSecurity(PasswordSecurityWebService passwordSecurityWebService)
    {
        _passwordSecurityWebService = passwordSecurityWebService;
    }

    public async Task<PasswordEvaluationResponse> Evaluar(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) { throw new ArgumentException("Ingresá una contraseña."); }

        return await _passwordSecurityWebService.EvaluarPassword(password);
    }
}