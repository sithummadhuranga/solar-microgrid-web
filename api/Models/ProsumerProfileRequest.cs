// File: ProsumerProfileRequest.cs
// Purpose: the fields a prosumer can change on their own profile, the nic is not one of them
// Author: H.M.T.S.M.Dissanayake

namespace SolarMicrogrid.Api.Models;

public class ProsumerProfileRequest
{
    public string FullName { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";
}
