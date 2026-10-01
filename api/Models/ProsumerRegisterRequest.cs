// File: ProsumerRegisterRequest.cs
// Purpose: the fields sent when registering a new prosumer
// Author: H.M.T.S.M.Dissanayake

namespace SolarMicrogrid.Api.Models;

public class ProsumerRegisterRequest
{
    public string Nic { get; set; } = "";

    public string Password { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";
}
