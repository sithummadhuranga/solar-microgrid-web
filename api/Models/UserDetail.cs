// File: UserDetail.cs
// Purpose: one user account, a prosumer, a backoffice staff member or a grid operator
// Author: H.M.T.S.M.Dissanayake

using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

public class UserDetail
{
    // the nic for a prosumer, the username for a web user, this is the mongo _id
    [BsonId]
    public string Id { get; set; } = "";

    public string Role { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = "pending";

    public bool DeactivationRequested { get; set; }

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";
}
