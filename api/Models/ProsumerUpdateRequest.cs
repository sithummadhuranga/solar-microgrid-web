// File: ProsumerUpdateRequest.cs
// Purpose: the fields backoffice can change on any prosumer, including the nic
// Author: H.M.T.S.M.Dissanayake

namespace SolarMicrogrid.Api.Models;

public class ProsumerUpdateRequest
{
    public string Nic { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";
}
