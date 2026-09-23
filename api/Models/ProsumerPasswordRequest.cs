// File: ProsumerPasswordRequest.cs
// Purpose: what a prosumer sends to change their own password
// Author: H.M.T.S.M.Dissanayake

namespace SolarMicrogrid.Api.Models;

public class ProsumerPasswordRequest
{
    public string CurrentPassword { get; set; } = "";

    public string NewPassword { get; set; } = "";
}
