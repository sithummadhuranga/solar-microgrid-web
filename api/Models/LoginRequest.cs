// File: LoginRequest.cs
// Purpose: what the login endpoint reads from the request body
// Author: H.M.T.S.M.Dissanayake

namespace SolarMicrogrid.Api.Models;

public class LoginRequest
{
    public string Identifier { get; set; } = "";

    public string Password { get; set; } = "";

    public string Platform { get; set; } = "";
}
