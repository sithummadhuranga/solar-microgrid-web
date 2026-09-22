// File: UserDetail.cs
// Purpose: one user account, a prosumer, a backoffice staff member or a grid operator
// Author: H.M.T.S.M.Dissanayake

using MongoDB.Bson.Serialization.Attributes;

namespace SolarMicrogrid.Api.Models;

public class UserDetail
{
    // a guid made before insert, same idea for every role
    [BsonId]
    public string Id { get; set; } = "";

    // only prosumers have a nic, left out of the document for anyone else
    [BsonIgnoreIfNull]
    public string? Nic { get; set; }

    // only backoffice and grid operator have an email, left out of the document for anyone else
    [BsonIgnoreIfNull]
    public string? Email { get; set; }

    public string Role { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = "pending";

    public bool DeactivationRequested { get; set; }

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";
}
