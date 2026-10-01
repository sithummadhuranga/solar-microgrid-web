// File: UserRequest.cs
// Purpose: the fields sent when creating or updating a backoffice or grid operator user
// Author: H.M.T.S.M.Dissanayake

namespace SolarMicrogrid.Api.Models;

public class UserRequest
{
    public string Email { get; set; } = "";

    // left blank on an update to keep the current password
    public string? Password { get; set; }

    public string FullName { get; set; } = "";

    public string Role { get; set; } = "";
}
